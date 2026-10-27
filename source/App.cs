using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpokenFlow {
    public sealed class MainForm : Form {
        private readonly DeepSeekClient api=new DeepSeekClient();
        private Settings settings;
        private TextBox keyBox,modelBox,resultBox,endpointBox;
        private ComboBox uiLanguage;
        private string keyEndpoint;
        private ComboBox languageA,languageB;
        private bool changingLanguages;
        private NumericUpDown pauseBox;
        private Label status,metrics;
        private Button toggle;
        private readonly Bubble bubble=new Bubble();
        private SelectionController selection;
        private bool appearanceOpen,composing;
        private long missingSince=-1;
        private NotifyIcon tray;
        private System.Windows.Forms.Timer timer;
        private bool enabled,inspectOnly,reading,exiting;
        private int version,requested=-1;
        private Snapshot current,readySource,undoSource;
        private Translation ready;
        private string undoText;
        private CancellationTokenSource pending;
        private long changedAt;
        private readonly Stopwatch time=Stopwatch.StartNew();
        private readonly Dictionary<string,Translation> cache=new Dictionary<string,Translation>();
        private DateTime lastError=DateTime.MinValue;
        private bool hotkeysReady;
        private bool cachedResult;
        private CancellationTokenSource connectionTest;
        private readonly bool demo;
        private int monitorEpoch;
        private const string DemoSource="我们晚点再聊。我先把手头的事情做完。";
        private const string DemoEnglish="Let's talk later. I'll finish what I'm working on first.";

        public MainForm(bool demonstration,bool inspectAtStartup=false) {
            demo=demonstration;
            try { settings=Settings.Load(); string probe=settings.Key; }
            catch { settings=new Settings(); MessageBox.Show(L.T("之前的设置无法读取，请重新填写。原配置文件未被修改。"),"SpokenFlow"); }
            L.SetLanguage(settings.UiLanguage);
            Text=demo?L.T("SpokenFlow · 离线演示（固定译文）"):L.T("SpokenFlow · 随打随译"); StartPosition=FormStartPosition.CenterScreen;
            ClientSize=new Size(820,820); MinimumSize=new Size(836,720); AutoScroll=true;
            BackColor=Color.FromArgb(246,248,251); Font=new Font("Segoe UI",10); Icon=AppIcon.Load();
            var layout=new TableLayoutPanel { Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(24),ColumnCount=1 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); Controls.Add(layout);
            var header=new FlowLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,WrapContents=true };
            header.Controls.Add(new Label { Text="SpokenFlow",Font=new Font("Segoe UI",24),AutoSize=true,Margin=new Padding(0,0,35,12) });
            header.Controls.Add(new Label { Text=L.T("界面语言"),AutoSize=true,Margin=new Padding(0,14,8,0) });
            uiLanguage=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList,Width=160,Margin=new Padding(0,10,0,0),AccessibleName=L.T("界面语言") };
            uiLanguage.Items.AddRange(LanguagePair.Labels); uiLanguage.SelectedIndex=Array.IndexOf(LanguagePair.Codes,settings.UiLanguage); header.Controls.Add(uiLanguage); layout.Controls.Add(header);
            layout.Controls.Add(new Label { Text=L.T("保留现有输入法，在所选两种语言之间自然互译"),AutoSize=true,MaximumSize=new Size(750,0),Margin=new Padding(0,0,0,16) });
            var toolbar=new FlowLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,WrapContents=true };
            var selectionButton=UiButton(L.T("划词：关"),delegate { selection.SetEnabled(!selection.Enabled); }); toolbar.Controls.Add(selectionButton);
            toolbar.Controls.Add(UiButton(L.T("浮窗外观"),delegate { ShowAppearance(); }));
            toolbar.Controls.Add(UiButton(L.T("预览浮窗"),delegate { Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--bubble-preview") { UseShellExecute=false }); })); layout.Controls.Add(toolbar);
            var fields=new TableLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,ColumnCount=2,Margin=new Padding(0,12,0,12) };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            endpointBox=new TextBox { Text=settings.BaseUrl,Dock=DockStyle.Fill,AccessibleName=L.T("服务地址") }; keyEndpoint=ApiEndpoint.Normalize(settings.BaseUrl);
            keyBox=new TextBox { AccessibleName="API Key",UseSystemPasswordChar=true,Text=demo?"":settings.Key,Enabled=!demo,Dock=DockStyle.Fill };
            modelBox=new TextBox { AccessibleName=L.T("模型名称"),Text=settings.Model,Dock=DockStyle.Fill };
            pauseBox=new NumericUpDown { Minimum=150,Maximum=3000,Increment=50,Value=settings.PauseMs,Width=130 };
            languageA=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList,Width=210,AccessibleName=L.T("互译语言一") };
            languageB=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList,Width=210,AccessibleName=L.T("互译语言二") };
            languageA.Items.AddRange(LanguagePair.Labels); languageB.Items.AddRange(LanguagePair.Labels);
            languageA.SelectedIndex=Array.IndexOf(LanguagePair.Codes,settings.LanguageA); languageB.SelectedIndex=Array.IndexOf(LanguagePair.Codes,settings.LanguageB);
            var pairRow=new FlowLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,WrapContents=true,Margin=Padding.Empty };
            pairRow.Controls.Add(languageA); pairRow.Controls.Add(new Label { Text="↔",AutoSize=true,Margin=new Padding(8) }); pairRow.Controls.Add(languageB);
            Field(fields,L.T("服务地址"),endpointBox); Field(fields,"API Key",keyBox); Field(fields,L.T("模型"),modelBox); Field(fields,L.T("停笔等待（ms）"),pauseBox); Field(fields,L.T("互译语言"),pairRow); layout.Controls.Add(fields);
            layout.Controls.Add(new Label { Text=L.T("密钥在本机加密保存。翻译文字会发送到所填服务地址。更换地址后请重新填写对应密钥。"),AutoSize=true,MaximumSize=new Size(750,0),Margin=new Padding(0,0,0,12) });
            var actions=new FlowLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,WrapContents=true };
            toggle=UiButton(demo?L.T("开启离线演示"):L.T("保存并开启翻译"),delegate { if(enabled) Stop(); else Start(false); }); toggle.BackColor=Color.FromArgb(29,111,100); toggle.ForeColor=Color.White;
            actions.Controls.Add(toggle); actions.Controls.Add(UiButton(L.T("仅检测兼容性"),delegate { Start(true); }));
            actions.Controls.Add(UiButton(L.T("打开练习输入框"),delegate { Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--playground") { UseShellExecute=false }); }));
            actions.Controls.Add(UiButton(L.T("测试 API"),async delegate { await TestConnection(); })); layout.Controls.Add(actions);
            status=new Label { AutoSize=true,MaximumSize=new Size(750,0),Margin=new Padding(0,16,0,8),ForeColor=Color.FromArgb(39,89,85),Text=demo?L.T("离线演示只处理固定测试例句，不调用 API。"):L.T("尚未开启，请填写 API Key，或先检测兼容性。") }; layout.Controls.Add(status);
            metrics=new Label { AutoSize=true,MaximumSize=new Size(750,0),Margin=new Padding(0,0,0,8),Text=L.T("首段译文与整段完成耗时将在这里显示") }; layout.Controls.Add(metrics);
            resultBox=new TextBox { Dock=DockStyle.Fill,Height=130,Multiline=true,ScrollBars=ScrollBars.Vertical,ReadOnly=true,BackColor=Color.White,Font=new Font("Segoe UI",12) }; layout.Controls.Add(resultBox);
            layout.Controls.Add(new Label { Text=L.T("Ctrl + Alt + E   替换当前区域       Ctrl + Alt + Z   恢复原文"),AutoSize=true,MaximumSize=new Size(750,0),Margin=new Padding(0,12,0,6) });
            layout.Controls.Add(new Label { Text=L.T("Ctrl + Alt + Space   开启 / 暂停       Ctrl + Alt + R   重新翻译"),AutoSize=true,MaximumSize=new Size(750,0) });
            layout.Controls.Add(new Label { Text=L.T("关闭窗口后留在托盘；右键托盘图标可退出。首次使用建议先在练习输入框中验证。"),AutoSize=true,MaximumSize=new Size(750,0),Margin=new Padding(0,12,0,0) });
            var menu=new ContextMenuStrip();
            menu.Items.Add(L.T("打开设置"),null,delegate { Show(); WindowState=FormWindowState.Normal; Activate(); });
            menu.Items.Add(L.T("开启 / 暂停"),null,delegate { if(enabled) Stop(); else Start(false); });
            var selectionItem=menu.Items.Add(L.T("划词互译：关"));
            selectionItem.Click+=delegate { selection.SetEnabled(!selection.Enabled); };
            menu.Items.Add(L.T("浮窗外观"),null,delegate { ShowAppearance(); });
            menu.Items.Add(L.T("退出"),null,delegate { exiting=true; Close(); });
            tray=new NotifyIcon { Icon=Icon, Text=L.T("SpokenFlow · 已暂停"), Visible=true, ContextMenuStrip=menu };
            selection=new SelectionController(this,bubble,api,delegate { return settings; },demo);
            uiLanguage.SelectedIndexChanged+=delegate {
                if(uiLanguage.SelectedIndex<0) return;
                string old=settings.UiLanguage; settings.UiLanguage=LanguagePair.Codes[uiLanguage.SelectedIndex];
                try { settings.Save(); Stop(); L.SetLanguage(settings.UiLanguage); L.RefreshMenu(menu); status.Text=L.T("界面语言已更新。点击开启继续翻译。"); }
                catch { settings.UiLanguage=old; status.Text=L.T("语言设置未能保存，请重试。"); }
            };
            endpointBox.TextChanged+=delegate {
                string next; try { next=ApiEndpoint.Normalize(endpointBox.Text); } catch { next=""; }
                if(next==keyEndpoint) return;
                Stop(); keyBox.Clear(); keyEndpoint=next;
                status.Text=L.T("服务地址已更改，请填写该服务的密钥并保存。原密钥不会自动发送到新地址。");
            };
            keyBox.TextChanged+=delegate { try { keyEndpoint=ApiEndpoint.Normalize(endpointBox.Text); } catch { keyEndpoint=""; } };
            L.Register(this);
            languageA.SelectedIndexChanged+=delegate { ChangeLanguages(true); };
            languageB.SelectedIndexChanged+=delegate { ChangeLanguages(false); };
            bubble.CollapsedChanged+=delegate(bool minimized) {
                monitorEpoch++; InvalidateTranslation(); requested=-1; composing=false; resultBox.Clear();
                selection.Suspended=minimized;
                changedAt=time.ElapsedMilliseconds-settings.PauseMs;
                status.Text=minimized?L.T("浮窗已最小化 · 翻译请求已暂停 · 点击小图标恢复"):L.T("已展开 · 正在读取最新输入内容");
                if(!minimized) { current=null; bubble.Present(Cursor.Position,L.T("正在读取最新内容"),L.T("等待读取最新输入内容…"),L.T("读取完成后翻译 · 原有译文已失效")); }
            };
            bubble.SelectionToggled+=delegate { selection.SetEnabled(!selection.Enabled); };
            bubble.AppearanceRequested+=ShowAppearance; selection.Popup.AppearanceRequested+=ShowAppearance;
            selection.Changed+=delegate(bool value) { selectionItem.Text=value?L.T("划词互译：开"):L.T("划词互译：关"); selectionButton.Text=value?L.T("划词：开"):L.T("划词：关"); };
            Shown+=delegate { selection.SetEnabled(Appearance.Current.SelectionEnabled); if(inspectAtStartup) Start(true); };
            tray.DoubleClick+=delegate { Show(); WindowState=FormWindowState.Normal; Activate(); };
            timer=new System.Windows.Forms.Timer { Interval=120 }; timer.Tick+=async delegate { await Poll(); }; timer.Start();
            FormClosing+=delegate(object sender,FormClosingEventArgs e) {
                if(!exiting && e.CloseReason==CloseReason.UserClosing) { e.Cancel=true; Hide(); return; }
                timer.Stop(); Stop(); selection.Dispose(); tray.Visible=false; tray.Dispose(); bubble.Dispose(); api.Dispose();
                for(int i=1;i<=4;i++) Native.UnregisterHotKey(Handle,i);
            };
        }
        private static Button UiButton(string text,EventHandler action) {
            var button=new Button { Text=text,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(12,7,12,7),FlatStyle=FlatStyle.Flat,BackColor=Color.White,Margin=new Padding(0,0,8,8) };
            button.Click+=action; return button;
        }
        private static void Field(TableLayoutPanel table,string label,Control control) {
            int row=table.RowCount++; table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(new Label { Text=label,AutoSize=true,Margin=new Padding(0,7,20,12) },0,row);
            control.Margin=new Padding(0,3,0,8); table.Controls.Add(control,1,row);
        }
        private Label AddLabel(string text,int x,int y,int w,int h,float size,Color color) {
            var l=new Label { Text=text, Location=new Point(x,y), Size=new Size(w,h), Font=new Font("Microsoft YaHei UI",size), ForeColor=color };
            Controls.Add(l); return l;
        }
        private void ChangeLanguages(bool firstChanged) {
            if(changingLanguages || languageA.SelectedIndex<0 || languageB.SelectedIndex<0) return;
            changingLanguages=true;
            string oldA=settings.LanguageA,oldB=settings.LanguageB;
            try {
                if(languageA.SelectedIndex==languageB.SelectedIndex) {
                    if(firstChanged) languageB.SelectedIndex=Array.IndexOf(LanguagePair.Codes,oldA);
                    else languageA.SelectedIndex=Array.IndexOf(LanguagePair.Codes,oldB);
                }
                settings.LanguageA=LanguagePair.Codes[languageA.SelectedIndex]; settings.LanguageB=LanguagePair.Codes[languageB.SelectedIndex];
                settings.Save();
                if(connectionTest!=null) { connectionTest.Cancel(); connectionTest=null; }
                monitorEpoch++; InvalidateTranslation(); requested=-1; current=null; cache.Clear(); resultBox.Clear();
                selection.ConfigurationChanged();
                status.Text=L.T("当前互译语言为 ")+settings.Pair.Label+L.T("，输入和划词同时生效。");
                if(!bubble.IsCollapsed) bubble.Hide();
            } catch {
                settings.LanguageA=oldA; settings.LanguageB=oldB;
                languageA.SelectedIndex=Array.IndexOf(LanguagePair.Codes,oldA); languageB.SelectedIndex=Array.IndexOf(LanguagePair.Codes,oldB);
                status.Text=L.T("语言设置未能保存，请重试。");
            } finally { changingLanguages=false; }
        }
        private void ShowAppearance() {
            if(appearanceOpen) return;
            appearanceOpen=true; selection.AppearanceOpen=true;
            try { using(var dialog=new AppearanceDialog()) dialog.ShowDialog(); }
            finally { appearanceOpen=false; selection.AppearanceOpen=false; }
        }
        private Button AddButton(string text,int x,int y,int w,int h,EventHandler action) {
            var b=new Button { Text=text, Location=new Point(x,y), Size=new Size(w,h), FlatStyle=FlatStyle.Flat, BackColor=Color.White, Cursor=Cursors.Hand };
            b.FlatAppearance.BorderColor=Color.FromArgb(208,218,229); b.Click+=action; Controls.Add(b); return b;
        }
        protected override void OnShown(EventArgs e) {
            base.OnShown(e); bool ok=true;
            ok &= Native.RegisterHotKey(Handle,1,0x4003,0x45);
            ok &= Native.RegisterHotKey(Handle,2,0x4003,0x20);
            ok &= Native.RegisterHotKey(Handle,3,0x4003,0x5A);
            ok &= Native.RegisterHotKey(Handle,4,0x4003,0x52);
            hotkeysReady=ok;
            if(!ok) status.Text=L.T("部分快捷键被占用。请关闭占用它们的软件后重新打开 SpokenFlow。");
        }
        protected override void WndProc(ref Message m) {
            if(m.Msg==0x0312) {
                int id=m.WParam.ToInt32();
                if(id==2) { if(enabled) Stop(); else Start(false); }
                else if(id==4) { InvalidateTranslation(); requested=-1; changedAt=time.ElapsedMilliseconds-settings.PauseMs; }
                else if(id==1 || id==3) {
                    var wait=new System.Windows.Forms.Timer { Interval=40 }; int ticks=0;
                    wait.Tick+=delegate { if(Native.ModifiersDown() && ++ticks<40) return; wait.Stop(); wait.Dispose(); if(!Native.ModifiersDown()) Replace(id==3); };
                    wait.Start();
                }
            }
            base.WndProc(ref m);
        }
        private void Start(bool onlyInspect) {
            Stop(false);
            selection.ConfigurationChanged();
            if(onlyInspect) selection.SetEnabled(false);
            if(!onlyInspect && !demo) {
                try { if(ApiEndpoint.NeedsKey(endpointBox.Text) && String.IsNullOrWhiteSpace(keyBox.Text)) { status.Text=L.T("请先填写 API Key。"); return; } }
                catch(InvalidOperationException ex) { status.Text=ex.Message; return; }
                if(String.IsNullOrWhiteSpace(modelBox.Text)) { status.Text=L.T("请填写模型名称。"); return; }
                try { settings.BaseUrl=ApiEndpoint.Normalize(endpointBox.Text); settings.Key=keyBox.Text.Trim(); keyEndpoint=settings.BaseUrl; settings.Model=modelBox.Text.Trim(); settings.PauseMs=(int)pauseBox.Value; settings.Save(); }
                catch { status.Text=L.T("设置保存失败，请检查本机配置目录权限。"); return; }
            }
            enabled=true; inspectOnly=onlyInspect; toggle.Text=L.T("暂停翻译");
            tray.Text=onlyInspect?L.T("SpokenFlow · 兼容性检测"):L.T("SpokenFlow · 翻译已开启");
            status.Text=onlyInspect?L.T("兼容性检测已开启，不发送网络请求。"):L.T("已开启 ")+settings.Pair.Label+L.T("，请切换到输入框。");
            if(!hotkeysReady) status.Text+=L.T(" 部分快捷键未注册。");
        }
        private void Stop(bool all=true) {
            if(all && selection!=null) selection.SetEnabled(false);
            if(connectionTest!=null) { connectionTest.Cancel(); connectionTest=null; }
            monitorEpoch++; enabled=false; InvalidateTranslation(true); current=null; requested=-1; cache.Clear(); composing=false;
            if(tray!=null) tray.Text=L.T("SpokenFlow · 已暂停");
            if(toggle!=null) toggle.Text=demo?L.T("开启离线演示"):L.T("保存并开启翻译");
            if(status!=null) status.Text=L.T("已暂停");
            if(resultBox!=null) resultBox.Clear();
        }
        private void InvalidateTranslation(bool hide=false) {
            version++; ready=null; readySource=null; cachedResult=false;
            if(pending!=null) { pending.Cancel(); pending=null; }
            if(hide) bubble.Hide();
        }
        private async Task Poll() {
            selection.ObserveForeground();
            if(!enabled || reading || IsDisposed) return;
            var foreground=Native.GetForegroundWindow();
            if(!appearanceOpen && current!=null && foreground!=current.Foreground) {
                InvalidateTranslation(true); current=null; requested=-1; composing=false;
            }
            if(appearanceOpen || bubble.Interacting || selection.Popup.Interacting) return;
            if(current!=null && foreground==current.Foreground && Native.Composing(foreground)) {
                if(!composing) { InvalidateTranslation(); requested=-1; composing=true; }
                bubble.State(current.Anchor,L.T("正在输入 · 等待文字上屏"),L.T("旧译文仅供参考 · 暂不可替换")); return;
            }
            if(composing) { composing=false; changedAt=time.ElapsedMilliseconds; }
            reading=true;
            int readEpoch=monitorEpoch;
            try {
                string description="";
                Snapshot sample=await CaptureThread.Run(delegate { try { return TextCapture.Read(out description); } catch { description=L.T("无法读取此输入框，可能未开放接口或权限不同。"); return null; } });
                if(!enabled || IsDisposed || readEpoch!=monitorEpoch) return;
                if(sample!=null && sample.Foreground!=Native.GetForegroundWindow()) return;
                if(sample==null) {
                    if(current!=null && current.Foreground==Native.GetForegroundWindow()) {
                        if(missingSince<0) missingSince=time.ElapsedMilliseconds;
                        if(time.ElapsedMilliseconds-missingSince<300) return;
                    }
                    if(current!=null) { InvalidateTranslation(true); current=null; resultBox.Clear(); }
                    if((DateTime.UtcNow-lastError).TotalSeconds>4) status.Text=description;
                    return;
                }
                missingSince=-1;
                if(!TextCapture.Same(sample,current)) {
                    bool sameField=TextCapture.SameField(sample,current);
                    InvalidateTranslation(); current=sample; changedAt=time.ElapsedMilliseconds; requested=-1;
                    resultBox.Clear();
                    if(!sameField) bubble.Present(sample.Anchor,settings.Pair.Label,L.T("请输入待翻译的文字。"),L.T("停笔后自动翻译"));
                    else bubble.State(sample.Anchor,L.T("输入已变化 · 正在更新"),L.T("旧译文仅供参考 · 暂不可替换"));
                } else current=sample;
                if(bubble.IsCollapsed) { status.Text=L.T("浮窗已最小化 · 翻译请求已暂停 · 点击小图标恢复"); return; }
                if(demo && !inspectOnly && sample.Text!=DemoSource) {
                    status.Text=L.T("离线演示仅处理固定例句：")+DemoSource;
                    return;
                }
                if(inspectOnly) {
                    status.Text=L.T("可读取：")+sample.App+" · "+sample.Scope+" "+sample.Text.Length+L.T(" 字 · 未发送网络请求");
                    metrics.Text=L.T("兼容性检测只验证读取和浮窗定位；真实翻译需 API Key。");
                    bubble.Present(sample.Anchor,L.T("兼容性检测 · ")+sample.Scope,sample.Text.Length==0?L.T("输入框已识别，请输入文字。"):L.T("已读取")+sample.Scope+" "+sample.Text.Length+L.T(" 字。\n开启翻译后，这里会显示译文。"),L.T("不联网 · 读取确认上屏后的文字"));
                    return;
                }
                if(!Rules.HasLanguageText(sample.Text)) {
                    status.Text=L.T("输入框已识别，等待文字内容");
                    bubble.Present(sample.Anchor,settings.Pair.Label,sample.Text.Length==0?L.T("输入框已清空，继续输入即可。"):L.T("当前内容没有可翻译的文字。"),L.T("浮窗保持显示 · 暂不可替换")); return;
                }
                if(ready!=null) { ShowReady(); return; }
                if(requested==version) return;
                if(time.ElapsedMilliseconds-changedAt<settings.PauseMs) { status.Text=L.T("输入中 · 等待短暂停顿"); bubble.State(sample.Anchor,L.T("输入中 · 等待停笔"),L.T("旧译文仅供参考 · 暂不可替换")); return; }
                requested=version;
                var requestSource=current;
                Translation hit;
                if(cache.TryGetValue(settings.BaseUrl+"\n"+settings.Pair.CacheKey+"\n"+settings.Model+"\n"+requestSource.Text,out hit)) {
                    ready=hit; readySource=requestSource; cachedResult=true; ShowReady(); return;
                }
                var cancel=new CancellationTokenSource(); pending=cancel; int requestVersion=version;
                status.Text=L.T("正在翻译 · ")+sample.App+" · "+sample.Text.Length+L.T(" 字");
                bubble.State(sample.Anchor,L.T("正在翻译 · 更新中"),L.T("旧译文仅供参考 · 暂不可替换"));
                TranslateInBackground(requestSource,requestVersion,cancel);
            } finally { reading=false; }
        }
        private async void TranslateInBackground(Snapshot source,int requestVersion,CancellationTokenSource cancel) {
            long lastPaint=-100;
            var pair=settings.Pair; string model=settings.Model,address=settings.BaseUrl;
            try {
                Action<string,long> onPartial=delegate(string partial,long first) {
                    // Bound UI refreshes independently of the model's token frequency.
                    long now=time.ElapsedMilliseconds;
                    if(now-lastPaint<50) return; lastPaint=now;
                    Post(delegate {
                        if(!enabled || ready!=null || version!=requestVersion || Native.GetForegroundWindow()!=source.Foreground) return;
                        resultBox.Text=partial;
                        metrics.Text=L.T("请求后首段译文 ")+first+L.T(" ms · 正在生成，完成后才能替换");
                        bubble.Present(current.Anchor,pair.Label+L.T(" · 正在生成"),partial,L.T("生成中 · 暂不可替换"));
                    });
                };
                Translation value;
                if(demo) {
                    string partial=""; var clock=Stopwatch.StartNew();
                    foreach(char ch in DemoEnglish) { cancel.Token.ThrowIfCancellationRequested(); partial+=ch; onPartial(partial,0); await Task.Delay(12,cancel.Token); }
                    value=new Translation { Text=DemoEnglish,FirstMs=0,TotalMs=clock.ElapsedMilliseconds };
                } else value=await api.TranslatePair(address,settings.Key,model,source.Text,pair,onPartial,cancel.Token);
                if(!enabled || version!=requestVersion || !TextCapture.Same(current,source)) return;
                ready=value; readySource=source;
                if(cache.Count>=24) cache.Clear(); cache[address+"\n"+pair.CacheKey+"\n"+model+"\n"+source.Text]=value;
                ShowReady();
            } catch(OperationCanceledException) {
                if(enabled && version==requestVersion) Error(L.T("请求超时；Ctrl+Alt+R 重试。"));
            } catch(Exception ex) {
                if(enabled && version==requestVersion) Error(ex is InvalidOperationException?ex.Message:L.T("网络连接失败或响应中断；请检查网络，Ctrl+Alt+R 重试。"));
            } finally { if(pending==cancel) pending=null; cancel.Dispose(); }
        }
        private void Post(Action action) { try { if(!IsDisposed && IsHandleCreated) BeginInvoke(action); } catch(InvalidOperationException) { } }
        private void ShowReady() {
            if(ready==null || current==null || Native.GetForegroundWindow()!=current.Foreground) return;
            resultBox.Text=ready.Text;
            status.Text=L.T("译文已完成 · ")+current.App+" · "+current.Scope+L.T(" · Ctrl+Alt+E 替换");
            metrics.Text=L.T("请求后首段译文 ")+ready.FirstMs+L.T(" ms · 全部完成 ")+ready.TotalMs+L.T(" ms · 另加停笔等待 ")+settings.PauseMs+L.T(" ms 和读取耗时");
            if(cachedResult) metrics.Text=L.T("命中本次运行的译文缓存 · 无需重新请求");
            if(demo) metrics.Text=L.T("离线固定译文演示；以上生成过程不代表 DeepSeek 的速度或质量。");
            bubble.Present(current.Anchor,demo?L.T("离线演示 · 固定译文"):settings.Pair.Label+" · "+current.Scope,ready.Text,L.T("Ctrl+Alt+E 替换")+current.Scope+L.T(" · 长文可滚动"));
        }
        private void Error(string message) {
            lastError=DateTime.UtcNow; status.Text=message; ready=null; readySource=null;
            if(current!=null) bubble.Present(current.Anchor,L.T("暂时无法完成翻译"),message,L.T("Ctrl+Alt+R 重试 · 原文保持原样"));
        }
        private void Replace(bool undo) {
            try {
                if(undo) {
                    if(undoSource==null) { Error(L.T("没有可恢复的替换记录。")); return; }
                    string message=TextCapture.Replace(undoSource,undoText); undoSource=null; undoText=null; InvalidateTranslation(); current=null; status.Text=L.T("已恢复原文");
                } else {
                    if(!enabled || ready==null || readySource==null) { Error(L.T("请等待当前整段译文完成后再按替换快捷键。")); return; }
                    var source=readySource; var value=ready.Text;
                    string message=TextCapture.Replace(source,value);
                    undoText=source.Text;
                    undoSource=source.WithText(value);
                    InvalidateTranslation(); current=null; status.Text=message; lastError=DateTime.UtcNow;
                }
            } catch(Exception ex) { Error(ex is InvalidOperationException?ex.Message:L.T("替换未完成，请检查当前输入框。可以在主窗口复制译文。")); }
        }
        private async Task TestConnection() {
            Stop();
            if(demo) { status.Text=L.T("离线演示不调用 API。请正常启动程序进行真实 API 测试。"); return; }
            try { if(ApiEndpoint.NeedsKey(endpointBox.Text) && String.IsNullOrWhiteSpace(keyBox.Text)) { status.Text=L.T("请先填写 API Key。"); return; } }
            catch(InvalidOperationException ex) { status.Text=ex.Message; return; }
            status.Text=L.T("正在用固定示例测试 API，不读取其他输入框。");
            using(var c=new CancellationTokenSource()) {
                connectionTest=c;
                try {
                    var pair=settings.Pair;
                    var result=await api.TranslatePair(endpointBox.Text,keyBox.Text.Trim(),modelBox.Text.Trim(),pair.Sample,pair,delegate{},c.Token);
                    if(connectionTest!=c || enabled) return;
                    resultBox.Text=result.Text; status.Text=L.T("API 连接成功；点击保存并开启翻译即可使用。");
                    metrics.Text=L.T("请求后首段译文 ")+result.FirstMs+L.T(" ms · 全部完成 ")+result.TotalMs+" ms";
                } catch(Exception ex) { if(connectionTest==c && !enabled) status.Text=ex is InvalidOperationException?ex.Message:L.T("API 测试失败或超时，请检查网络和密钥。"); }
                finally { if(connectionTest==c) connectionTest=null; }
            }
        }
    }

    public static class Program {
        [STAThread] public static void Main(string[] args) {
            if(args.Length>0 && args[0]=="--self-test") { Environment.Exit(SelfTest.Run(args.Length>1?args[1]:"self-test.txt")); return; }
            try { L.SetLanguage(Settings.Load().UiLanguage); } catch { L.SetLanguage("en"); }
            Native.SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            if(args.Length>0 && args[0]=="--bubble-preview") { Application.Run(new BubblePreview()); return; }
            if(args.Length>0 && args[0]=="--playground") {
                var f=new Form { Text=L.T("SpokenFlow · 练习输入框"), StartPosition=FormStartPosition.CenterScreen, ClientSize=new Size(640,400), Font=new Font("Microsoft YaHei UI",12),Icon=AppIcon.Load() };
                var label=new Label { Text=L.T("输入所选语言中的任一种，观察浮窗。完成后按 Ctrl+Alt+E 替换整段。"), Location=new Point(22,22), Size=new Size(595,55) };
                var input=new TextBox { AccessibleName=L.T("练习输入框"), Multiline=true, AcceptsReturn=true, ScrollBars=ScrollBars.Vertical, Location=new Point(22,85),Size=new Size(595,280),Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right };
                f.Controls.AddRange(new Control[]{label,input}); f.Shown+=delegate { input.Focus(); }; L.Register(f); Application.Run(f); return;
            }
            bool created;
            using(var single=new Mutex(true,"Local\\SpokenFlow.Desktop",out created)) {
                if(!created) { MessageBox.Show(L.T("SpokenFlow 已在运行，请从系统托盘打开。"),"SpokenFlow"); return; }
                Application.Run(new MainForm(args.Length>0 && args[0]=="--demo",args.Length>0 && args[0]=="--inspect"));
            }
        }
    }
}
