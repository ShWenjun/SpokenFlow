using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SpokenFlow {
    public static class AppIcon {
        public static Icon Load() {
            try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { return SystemIcons.Application; }
        }
    }
    public sealed class Bubble : Form {
        private readonly Label heading,foot,selectButton,copyButton,styleButton,followButton,closeButton,minimizeButton;
        private readonly MiniBubbleHandle miniHandle;
        private readonly TranslationView body;
        private readonly ToolTip tips=new ToolTip();
        private readonly bool secondary;
        private Point lastAnchor,dragCursor,dragOrigin;
        private Rectangle resizeOrigin;
        private Size expandedSize;
        private bool collapsed,miniPressed,miniMoved;
        private bool pinned,dragging,resizing;
        private int resizeEdges;
        public event Action SelectionToggled,AppearanceRequested,Dismissed;
        public event Action<bool> CollapsedChanged;
        public bool IsCollapsed { get { return collapsed; } }
        public event Action<string,Point> TextChosen;
        public bool Interacting { get { return dragging || resizing || miniPressed || body.Selecting; } }
        public bool PointerInside { get { return Visible && Bounds.Contains(Cursor.Position); } }
        public string DisplayText { get { return body.Content; } }
        public Bubble(bool selectionPopup=false) {
            secondary=selectionPopup;
            Text=secondary?L.T("SpokenFlow · 划词译文"):L.T("SpokenFlow · 翻译浮窗");
            FormBorderStyle=FormBorderStyle.None; ShowInTaskbar=false; TopMost=true;
            StartPosition=FormStartPosition.Manual; MinimumSize=new Size(320,180);
            ClientSize=secondary?new Size(350,215):new Size(Appearance.Current.Width,Appearance.Current.Height);
            Font=new Font("Microsoft YaHei UI",9); Icon=AppIcon.Load();
            heading=new Label { Location=new Point(14,8),Height=26,Text="SpokenFlow",Cursor=Cursors.SizeAll,AutoEllipsis=true };
            foot=new Label { Height=20,AutoEllipsis=true,Font=new Font("Microsoft YaHei UI",8) };
            body=new TranslationView(); Controls.AddRange(new Control[]{heading,foot,body});
            selectButton=ActionLabel(L.T("划词：关"),14,37,88,delegate { if(SelectionToggled!=null) SelectionToggled(); });
            copyButton=ActionLabel(L.T("复制"),secondary?14:108,37,48,delegate { try { if(body.Content.Length>0) { Clipboard.SetText(body.Content); foot.Text=L.T("已复制"); } } catch { foot.Text=L.T("复制失败，请重试"); } });
            styleButton=ActionLabel(L.T("外观"),secondary?74:162,37,48,delegate { if(AppearanceRequested!=null) AppearanceRequested(); });
            followButton=ActionLabel("↺",216,37,32,delegate { pinned=false; SaveGeometry(); Place(lastAnchor); });
            closeButton=ActionLabel("×",0,7,26,delegate { Hide(); if(Dismissed!=null) Dismissed(); });
            minimizeButton=ActionLabel("−",0,7,26,delegate { SetCollapsed(true); });
            minimizeButton.AccessibleName=L.T("最小化浮窗并暂停翻译");
            minimizeButton.Visible=!secondary;
            miniHandle=new MiniBubbleHandle { Visible=false,Cursor=Cursors.Hand,AccessibleName=L.T("SpokenFlow 已暂停，点击展开翻译，拖动移动") };
            Controls.Add(miniHandle);
            miniHandle.MouseDown+=delegate(object s,MouseEventArgs e) {
                if(e.Button!=MouseButtons.Left) return;
                miniPressed=true; miniMoved=false; dragCursor=Cursor.Position; dragOrigin=Location; miniHandle.Capture=true;
            };
            miniHandle.MouseMove+=delegate {
                if(!miniPressed) return; var p=Cursor.Position;
                if(Math.Abs(p.X-dragCursor.X)>SystemInformation.DragSize.Width/2 || Math.Abs(p.Y-dragCursor.Y)>SystemInformation.DragSize.Height/2) miniMoved=true;
                if(miniMoved) { Location=Clamp(new Point(dragOrigin.X+p.X-dragCursor.X,dragOrigin.Y+p.Y-dragCursor.Y)); pinned=true; }
            };
            miniHandle.MouseUp+=delegate(object s,MouseEventArgs e) {
                if(e.Button!=MouseButtons.Left || !miniPressed) return;
                miniPressed=false; miniHandle.Capture=false;
                if(miniMoved) SaveGeometry(); else SetCollapsed(false);
            };
            L.SetToolTip(tips,minimizeButton,L.T("收为小图标并暂停翻译请求；点击小图标恢复"));
            L.SetToolTip(tips,miniHandle,L.T("翻译已暂停 · 点击展开并翻译最新内容 · 可拖动"));
            selectButton.Visible=!secondary; followButton.Visible=!secondary; closeButton.Visible=secondary;
            L.SetToolTip(tips,selectButton,L.T("开启后，选中文字会按设置中的两种语言互译")); L.SetToolTip(tips,followButton,L.T("恢复跟随输入位置"));
            L.SetToolTip(tips,heading,L.T("拖动标题移动浮窗；拖动边缘调整大小"));
            body.TextChosen+=delegate(string text,Point p) { if(TextChosen!=null) TextChosen(text,p); };
            heading.MouseDown+=delegate(object s,MouseEventArgs e) { if(e.Button!=MouseButtons.Left) return; dragging=true; dragCursor=Cursor.Position; dragOrigin=Location; heading.Capture=true; };
            heading.MouseMove+=delegate { if(!dragging) return; var p=Cursor.Position; Location=Clamp(new Point(dragOrigin.X+p.X-dragCursor.X,dragOrigin.Y+p.Y-dragCursor.Y)); pinned=true; };
            heading.MouseUp+=delegate { if(!dragging) return; dragging=false; heading.Capture=false; SaveGeometry(); };
            heading.DoubleClick+=delegate { if(!secondary) { pinned=false; SaveGeometry(); Place(lastAnchor); } };
            foreach(int edges in new[]{1,2,4,8,5,6,9,10}) AddGrip(edges);
            pinned=!secondary && Appearance.Current.Pinned;
            if(pinned) Location=Clamp(new Point(Appearance.Current.X,Appearance.Current.Y));
            Appearance.Changed+=ApplyAppearance; ApplyAppearance(); Arrange(); L.Register(this);
            selectButton.TextChanged+=delegate { Arrange(); }; copyButton.TextChanged+=delegate { Arrange(); }; styleButton.TextChanged+=delegate { Arrange(); };
        }
        private Label ActionLabel(string text,int x,int y,int w,EventHandler click) {
            var l=new Label { Text=text,Location=new Point(x,y),Size=new Size(w,25),TextAlign=ContentAlignment.MiddleCenter,Cursor=Cursors.Hand,AutoEllipsis=true };
            l.Click+=click; Controls.Add(l); return l;
        }
        private void AddGrip(int edges) {
            var l=new Label { Tag=edges,Cursor=edges==1||edges==2?Cursors.SizeWE:edges==4||edges==8?Cursors.SizeNS:edges==5||edges==10?Cursors.SizeNWSE:Cursors.SizeNESW };
            Controls.Add(l);
            l.MouseDown+=delegate(object s,MouseEventArgs e) { if(e.Button!=MouseButtons.Left) return; resizing=true; resizeEdges=edges; resizeOrigin=Bounds; dragCursor=Cursor.Position; l.Capture=true; };
            l.MouseMove+=delegate {
                if(!resizing || resizeEdges!=edges) return;
                var p=Cursor.Position; int dx=p.X-dragCursor.X,dy=p.Y-dragCursor.Y;
                int w=resizeOrigin.Width+((edges&1)!=0?-dx:(edges&2)!=0?dx:0);
                int h=resizeOrigin.Height+((edges&4)!=0?-dy:(edges&8)!=0?dy:0);
                var screen=Screen.FromRectangle(resizeOrigin).WorkingArea;
                w=Math.Max(320,Math.Min(Math.Min(1200,screen.Width),w)); h=Math.Max(180,Math.Min(Math.Min(900,screen.Height),h));
                Bounds=new Rectangle((edges&1)!=0?resizeOrigin.Right-w:resizeOrigin.X,(edges&4)!=0?resizeOrigin.Bottom-h:resizeOrigin.Y,w,h); pinned=true;
            };
            l.MouseUp+=delegate { if(!resizing) return; resizing=false; l.Capture=false; Location=Clamp(Location); SaveGeometry(); };
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams { get { var p=base.CreateParams; p.ExStyle|=0x08000000|0x80; return p; } }
        protected override void WndProc(ref Message m) { if(m.Msg==0x21) { m.Result=(IntPtr)3; return; } base.WndProc(ref m); }
        protected override void OnResize(EventArgs e) { base.OnResize(e); if(body!=null) Arrange(); }
        private void Arrange() {
            if(miniHandle==null) return;
            if(collapsed) { miniHandle.Bounds=ClientRectangle; miniHandle.BringToFront(); Invalidate(); return; }
            int x=14;
            foreach(var button in new[]{selectButton,copyButton,styleButton,followButton}) {
                if(secondary && (button==selectButton || button==followButton)) continue;
                int w=button==followButton?26:Math.Max(38,TextRenderer.MeasureText(button.Text,button.Font).Width+10);
                button.SetBounds(x,37,w,25); x+=w+4;
            }
            heading.Width=Width-56; closeButton.Location=new Point(Width-40,7);
            minimizeButton.Location=new Point(Width-40,7);
            body.Bounds=new Rectangle(14,70,Width-28,Height-107); foot.Bounds=new Rectangle(14,Height-29,Width-42,20);
            foreach(Control c in Controls) if(c.Tag is int) {
                int a=(int)c.Tag; bool left=(a&1)!=0,right=(a&2)!=0,top=(a&4)!=0,bottom=(a&8)!=0;
                c.Bounds=new Rectangle(right?Width-7:0,bottom?Height-7:0,left||right?7:Width,top||bottom?7:Height);
                if(a==1||a==2) c.Bounds=new Rectangle(left?0:Width-7,7,7,Height-14);
                if(a==4||a==8) c.Bounds=new Rectangle(7,top?0:Height-7,Width-14,7);
                c.BringToFront();
            }
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e); if(collapsed) return;
            using(var p=new Pen(Appearance.Muted)) for(int d=7;d<=13;d+=3) e.Graphics.DrawLine(p,Width-d,Height-9,Width-9,Height-d);
        }
        private void ApplyAppearance() {
            BackColor=Appearance.Background; ForeColor=Appearance.Foreground; Opacity=Appearance.Current.Opacity/100.0;
            foreach(Control c in Controls) { c.BackColor=BackColor; c.ForeColor=ForeColor; }
            foot.ForeColor=Appearance.Muted; body.ApplyTheme(); SetSelectionEnabled(Appearance.Current.SelectionEnabled);
            Invalidate();
        }
        public void SetSelectionEnabled(bool value) { selectButton.Text=value?L.T("划词：开"):L.T("划词：关"); selectButton.ForeColor=value?Appearance.Accent:Appearance.Muted; }
        private Point Clamp(Point p) { var s=Screen.FromPoint(p).WorkingArea; return new Point(Math.Max(s.Left,Math.Min(p.X,s.Right-Width)),Math.Max(s.Top,Math.Min(p.Y,s.Bottom-Height))); }
        private void Place(Point anchor) {
            if(collapsed) { Location=Clamp(Location); return; }
            if(Interacting) return; if(pinned) { Location=Clamp(Location); return; }
            var screen=Screen.FromPoint(anchor).WorkingArea;
            Location=Clamp(new Point(anchor.X,anchor.Y+Height>screen.Bottom?anchor.Y-Height-24:anchor.Y));
        }
        private void SaveGeometry() {
            if(secondary) return;
            var a=Appearance.Current; a.Width=collapsed?expandedSize.Width:Width; a.Height=collapsed?expandedSize.Height:Height; a.Pinned=pinned; a.X=Left; a.Y=Top; Appearance.Save();
        }
        public void SetCollapsed(bool value) {
            if(secondary || collapsed==value) return;
            if(value) expandedSize=Size;
            collapsed=value;
            SuspendLayout();
            foreach(Control c in Controls) c.Visible=!value;
            miniHandle.Visible=value;
            selectButton.Visible=!value && !secondary; followButton.Visible=!value && !secondary;
            closeButton.Visible=!value && secondary; minimizeButton.Visible=!value && !secondary;
            MinimumSize=value?new Size(52,52):new Size(320,180);
            Size=value?new Size(52,52):expandedSize;
            if(Region!=null) { var old=Region; Region=null; old.Dispose(); }
            if(value) using(var path=new System.Drawing.Drawing2D.GraphicsPath()) { path.AddEllipse(0,0,52,52); Region=new Region(path); }
            Location=Clamp(Location);
            ResumeLayout(); Arrange();
            if(CollapsedChanged!=null) CollapsedChanged(value);
        }
        public void ResetTransientPosition() { if(secondary) pinned=false; }
        public void Present(Point anchor,string title,string text,string hint) {
            lastAnchor=anchor; heading.Text=title; foot.Text=hint; body.UpdateText(text); Place(anchor); if(!Visible) Show();
        }
        public void State(Point anchor,string title,string hint) { Present(anchor,title,body.Content.Length==0?L.T("等待翻译…"):body.Content,hint); }
        protected override void Dispose(bool disposing) { if(disposing) { Appearance.Changed-=ApplyAppearance; tips.Dispose(); } base.Dispose(disposing); }
    }
    internal sealed class MiniBubbleHandle : Control {
        public MiniBubbleHandle() { SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true); TabStop=false; }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using(var fill=new SolidBrush(Appearance.Background)) e.Graphics.FillEllipse(fill,0,0,Width-1,Height-1);
            using(var border=new Pen(Appearance.Accent,1.5f)) e.Graphics.DrawEllipse(border,1,1,Width-3,Height-3);
            var form=FindForm(); if(form!=null) e.Graphics.DrawIcon(form.Icon,new Rectangle(9,7,32,32));
            using(var fill=new SolidBrush(Appearance.Accent)) { e.Graphics.FillRectangle(fill,19,42,4,5); e.Graphics.FillRectangle(fill,27,42,4,5); }
        }
    }
    public sealed class BubblePreview : Form {
        private readonly Bubble bubble=new Bubble(),selection=new Bubble(true);
        private readonly Timer pulse=new Timer { Interval=120 };
        private bool longText,selectionEnabled;
        public BubblePreview() {
            Text=L.T("SpokenFlow · 浮窗外观预览（不联网）"); ClientSize=new Size(660,380); StartPosition=FormStartPosition.CenterScreen; Icon=AppIcon.Load();
            var label=new Label { Text=L.T("此预览不调用 API。可拖动标题/边缘、调整外观、开启划词后选中译文。"),Location=new Point(20,20),Size=new Size(620,45) };
            var shortButton=new Button { Text=L.T("短文本"),Location=new Point(20,70) }; var longButton=new Button { Text=L.T("长文本"),Location=new Point(110,70) };
            shortButton.Click+=delegate { longText=false; }; longButton.Click+=delegate { longText=true; };
            var input=new TextBox { Multiline=true,Location=new Point(20,115),Size=new Size(620,220),Text=L.T("预览期间可在此输入，检查浮窗是否影响输入焦点。") };
            Controls.AddRange(new Control[]{label,shortButton,longButton,input});
            bubble.SelectionToggled+=delegate { selectionEnabled=!selectionEnabled; bubble.SetSelectionEnabled(selectionEnabled); };
            bubble.TextChosen+=delegate(string text,Point p) { if(selectionEnabled) selection.Present(new Point(p.X,p.Y+16),L.T("划词预览 · 不联网"),L.T("已选中：")+text,L.T("真实运行时按所选语言互译 · 可复制")); };
            bubble.AppearanceRequested+=delegate { using(var dialog=new AppearanceDialog()) dialog.ShowDialog(this); };
            selection.AppearanceRequested+=delegate { using(var dialog=new AppearanceDialog()) dialog.ShowDialog(this); };
            pulse.Tick+=delegate {
                if(WindowState==FormWindowState.Minimized) { bubble.Hide(); selection.Hide(); return; }
                string text="Let's talk later. I'll finish what I'm working on first.";
                if(longText) for(int i=1;i<=16;i++) text+="\n\n"+i+". This is a longer paragraph to check scrolling. You can resize this window, change its color, or select these words.";
                bubble.Present(PointToScreen(new Point(240,150)),L.T("浮窗预览 · 不联网"),text,L.T("拖动标题移动 · 拖动边缘缩放"));
            };
            L.Register(this); Shown+=delegate { pulse.Start(); }; FormClosed+=delegate { pulse.Dispose(); bubble.Dispose(); selection.Dispose(); };
        }
    }
}
