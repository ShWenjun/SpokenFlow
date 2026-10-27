using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpokenFlow {
    public sealed class SelectionController : IDisposable {
        private readonly Form owner;
        private readonly Bubble main;
        private readonly DeepSeekClient api;
        private readonly Func<Settings> configuration;
        private readonly bool demo;
        public readonly Bubble Popup=new Bubble(true);
        private SelectionMouse mouse;
        private CancellationTokenSource pending;
        private readonly System.Windows.Forms.Timer delay=new System.Windows.Forms.Timer { Interval=230 };
        private readonly Dictionary<string,Translation> cache=new Dictionary<string,Translation>();
        private int generation;
        private bool reading,disposed;
        private Point point,start;
        private IntPtr foreground;
        private string lastKey;
        public bool Enabled { get { return mouse!=null; } }
        public bool AppearanceOpen;
        private bool suspended;
        public bool Suspended { get { return suspended; } set { if(suspended==value) return; suspended=value; if(value) Cancel(); } }
        public event Action<bool> Changed;
        public SelectionController(Form owner,Bubble main,DeepSeekClient api,Func<Settings> configuration,bool demo) {
            this.owner=owner; this.main=main; this.api=api; this.configuration=configuration; this.demo=demo;
            main.TextChosen+=OwnSelection; Popup.TextChosen+=OwnSelection;
            Popup.Dismissed+=Cancel;
            delay.Tick+=async delegate { delay.Stop(); await ReadSelection(); };
        }
        public void SetEnabled(bool value) {
            if(value==Enabled) return;
            if(value) {
                try { mouse=new SelectionMouse(); mouse.Released+=Released; mouse.Started+=Started; mouse.KeyPressed+=KeyPressed; }
                catch(Exception ex) { MessageBox.Show(owner,ex.Message,L.T("划词互译")); return; }
            } else { mouse.Dispose(); mouse=null; Cancel(); }
            Appearance.Current.SelectionEnabled=Enabled; Appearance.Save(); main.SetSelectionEnabled(Enabled);
            if(Changed!=null) Changed(Enabled);
        }
        private bool InsidePopup(Point p) { return main.Visible && main.Bounds.Contains(p) || Popup.Visible && Popup.Bounds.Contains(p); }
        private void Started(Point p) { if(!InsidePopup(p)) Cancel(); }
        private void KeyPressed() {
            // Keep text selection/copy in our own popup usable.
            if(Native.GetForegroundWindow()!=owner.Handle) Cancel();
        }
        private void Released(Point from,Point p,bool selection) {
            // Decide popup ownership before its MouseUp handler can move the result
            // window. Also reject queued releases invalidated by a later input event.
            if(disposed || suspended || !owner.IsHandleCreated || InsidePopup(p)) return;
            int queuedGeneration=generation; IntPtr sourceForeground=Native.GetForegroundWindow();
            try { owner.BeginInvoke((Action)delegate {
                if(disposed || queuedGeneration!=generation || sourceForeground!=Native.GetForegroundWindow()) return;
                if(!Enabled || suspended || AppearanceOpen || main.Bounds.Contains(p)&&main.Visible || Popup.Bounds.Contains(p)&&Popup.Visible) return;
                Cancel();
                if(!selection) return;
                start=from; point=p; foreground=sourceForeground; delay.Start();
            }); } catch(InvalidOperationException) { }
        }
        private async Task ReadSelection() {
            if(!Enabled || suspended || AppearanceOpen) return;
            if(reading) { delay.Start(); return; }
            int id=generation; var p=point; var from=start; var fg=foreground; reading=true;
            Func<bool> valid=delegate { return !disposed && !suspended && Enabled && !AppearanceOpen && id==generation && fg==Native.GetForegroundWindow(); };
            try {
                string text=await SelectionRetry.Read(()=>CaptureThread.Run(delegate { return SelectionCapture.Read(from,p,fg); }),valid,ms=>Task.Delay(ms));
                if(text==null && valid() && Appearance.Current.SelectionClipboardFallback) {
                    bool allowed=await CaptureThread.Run(delegate { return SelectionCapture.CanCopy(from,p,fg); });
                    if(!valid()) return;
                    if(allowed) {
                        text=await SelectionCopy.Read(new WindowsSelectionClipboard(fg),valid,ms=>Task.Delay(ms));
                        if(valid() && text==null) {
                            Popup.ResetTransientPosition();
                            Popup.Present(new Point(p.X,p.Y+18),L.T("未取得选中文字"),L.T("请重新拖选文字。当前应用可能未提供选区，或剪贴板暂时无法读取。"),L.T("未发送翻译请求 · 点击其他位置关闭"));
                        }
                    }
                }
                if(valid() && text!=null) Translate(text,p,fg);
            } finally { reading=false; }
        }
        private void OwnSelection(string text,Point p) { if(Enabled && !suspended) Translate(text,p,Native.GetForegroundWindow()); }
        public void ObserveForeground() { if(Popup.Visible && !AppearanceOpen && foreground!=Native.GetForegroundWindow()) Cancel(); }
        private void Cancel() {
            generation++; delay.Stop(); if(pending!=null) { pending.Cancel(); pending=null; }
            lastKey=null; Popup.Hide();
        }
        public void ConfigurationChanged() { Cancel(); cache.Clear(); }
        private async void Translate(string text,Point p,IntPtr fg) {
            if(String.IsNullOrWhiteSpace(text) || text.Length>TextCapture.Limit) return;
            if(!Rules.HasLanguageText(text)) return;
            var config=configuration(); string model=config.Model; var pair=config.Pair;
            string key=config.BaseUrl+"\n"+pair.CacheKey+"\n"+model+"\n"+text;
            if(lastKey==key && Popup.Visible && foreground==fg) return;
            Cancel(); lastKey=key; foreground=fg; int id=generation;
            Point anchor=new Point(p.X,p.Y+18); Popup.ResetTransientPosition();
            string title=L.T("划词互译 · ")+pair.Label;
            Translation hit;
            if(cache.TryGetValue(key,out hit)) { Popup.Present(anchor,title,hit.Text,L.T("已完成 · 缓存命中 · 点击复制")); return; }
            if(demo) { Popup.Present(anchor,L.T("划词演示 · 不联网"),L.T("已选中：")+text,L.T("真实模式按所选语言互译 · 点击复制")); return; }
            string apiKey=config.Key;
            if(ApiEndpoint.NeedsKey(config.BaseUrl) && String.IsNullOrWhiteSpace(apiKey)) { Popup.Present(anchor,title,L.T("请先在主窗口填写 API Key 并保存开启。"),L.T("划词翻译需要可用的模型连接")); return; }
            Popup.Present(anchor,title,L.T("正在翻译选中文字…"),L.T("生成中 · 点击其他位置关闭"));
            var cancel=new CancellationTokenSource(); pending=cancel;
            try {
                var result=await api.TranslatePair(config.BaseUrl,apiKey,model,text,pair,delegate(string part,long first) {
                    try { if(!disposed && owner.IsHandleCreated) owner.BeginInvoke((Action)delegate {
                        if(disposed || !Enabled || generation!=id || fg!=Native.GetForegroundWindow()) return;
                        Popup.Present(anchor,title,part,L.T("正在生成 · 点击其他位置关闭"));
                    }); } catch(InvalidOperationException) { }
                },cancel.Token);
                if(disposed || !Enabled || generation!=id || fg!=Native.GetForegroundWindow()) return;
                if(cache.Count>=40) cache.Clear(); cache[key]=result;
                Popup.Present(anchor,title,result.Text,L.T("已完成 · 点击复制 · 点击其他位置关闭"));
            } catch(Exception ex) {
                if(!disposed && Enabled && generation==id && fg==Native.GetForegroundWindow()) Popup.Present(anchor,L.T("划词翻译未完成"),ex is InvalidOperationException?ex.Message:L.T("请求超时或网络中断，请重新划词。"),L.T("原文未修改"));
            } finally { if(pending==cancel) pending=null; cancel.Dispose(); }
        }
        public void Dispose() {
            disposed=true; Cancel(); if(mouse!=null) mouse.Dispose(); mouse=null; delay.Dispose();
            main.TextChosen-=OwnSelection; Popup.Dispose();
        }
    }
}
