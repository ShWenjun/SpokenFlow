using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpokenFlow {
    public sealed class SlimScrollBar : Control {
        private int position,extent=1,viewport=1,startY,startValue;
        private bool over,dragging;
        public event Action<int> ValueChanged;
        public int Value { get { return position; } }
        public SlimScrollBar() { Width=16; TabStop=false; AccessibleName=L.T("译文滚动条"); SetStyle(ControlStyles.Selectable,false); SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true); }
        public void SetRange(int content,int visible,int value) { extent=Math.Max(1,content); viewport=Math.Max(1,visible); position=Math.Max(0,Math.Min(Maximum,value)); Visible=Maximum>0; Invalidate(); }
        private int Maximum { get { return Math.Max(0,extent-viewport); } }
        private int ThumbHeight { get { return Math.Min(Height,Math.Max(26,(int)((long)Height*viewport/extent))); } }
        private int ThumbY { get { return Maximum==0?0:(int)((long)position*Math.Max(0,Height-ThumbHeight)/Maximum); } }
        private void SetPosition(int v) { v=Math.Max(0,Math.Min(Maximum,v)); if(v==position) return; position=v; Invalidate(); if(ValueChanged!=null) ValueChanged(position); }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            if(over||dragging) using(var track=new SolidBrush(Color.FromArgb(Appearance.Light?230:47,Appearance.Light?235:64,Appearance.Light?237:77))) e.Graphics.FillRectangle(track,Width-11,0,10,Height);
            int width=over||dragging?7:4; var r=new Rectangle(Width-width-3,ThumbY,width,Math.Max(width,ThumbHeight));
            using(var b=new SolidBrush(over||dragging?Appearance.Accent:Appearance.Muted)) {
                e.Graphics.FillRectangle(b,r.X,r.Y+width/2,r.Width,Math.Max(0,r.Height-width));
                e.Graphics.FillEllipse(b,r.X,r.Y,width,width); e.Graphics.FillEllipse(b,r.X,r.Bottom-width,width,width);
            }
        }
        protected override void OnMouseEnter(EventArgs e) { over=true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { over=false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) {
            if(e.Button!=MouseButtons.Left) return;
            if(e.Y<ThumbY||e.Y>ThumbY+ThumbHeight) SetPosition((int)((long)(e.Y-ThumbHeight/2)*Maximum/Math.Max(1,Height-ThumbHeight)));
            startY=e.Y; startValue=position; dragging=true; Capture=true; Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e) { if(dragging) SetPosition(startValue+(int)((long)(e.Y-startY)*Maximum/Math.Max(1,Height-ThumbHeight))); }
        protected override void OnMouseUp(MouseEventArgs e) { if(e.Button==MouseButtons.Left) { dragging=false; Capture=false; Invalidate(); } }
        protected override void OnMouseWheel(MouseEventArgs e) { SetPosition(position-e.Delta/120*48); }
    }
    public sealed class PassiveRichTextBox : RichTextBox {
        private int start;
        public bool Selecting { get; private set; }
        public event Action<string,Point> TextChosen;
        public event Action<int> Wheel;
        public PassiveRichTextBox() {
            ReadOnly=true; BorderStyle=BorderStyle.None; ScrollBars=RichTextBoxScrollBars.None; WordWrap=true;
            DetectUrls=false; HideSelection=false; TabStop=false; Font=new Font("Segoe UI",11.5f);
            AccessibleName=L.T("可选择的译文");
        }
        protected override void WndProc(ref Message m) {
            if(m.Msg==0x21) { m.Result=new IntPtr(3); return; }
            if(m.Msg==0x20A) { if(Wheel!=null) Wheel((short)((m.WParam.ToInt64()>>16)&0xffff)); return; }
            if(m.Msg==0x201) { start=GetCharIndexFromPosition(PointToClient(Cursor.Position)); Select(start,0); Selecting=true; Capture=true; return; }
            if(m.Msg==0x200 && Selecting) {
                Point p=PointToClient(Cursor.Position);
                if(p.Y<0 && Wheel!=null) Wheel(120); else if(p.Y>Height && Wheel!=null) Wheel(-120);
                int end=GetCharIndexFromPosition(new Point(Math.Max(0,Math.Min(Width-1,p.X)),Math.Max(0,Math.Min(Height-1,p.Y))));
                Select(Math.Min(start,end),Math.Abs(start-end)); return;
            }
            if(m.Msg==0x202 && Selecting) { Selecting=false; Capture=false; if(SelectionLength>0 && TextChosen!=null) TextChosen(SelectedText,Cursor.Position); return; }
            if(m.Msg==0x203) {
                int i=GetCharIndexFromPosition(PointToClient(Cursor.Position)),a=i,b=i;
                while(a>0 && Char.IsLetterOrDigit(Text[a-1])) a--;
                while(b<TextLength && Char.IsLetterOrDigit(Text[b])) b++;
                Select(a,b-a); if(b>a && TextChosen!=null) TextChosen(SelectedText,Cursor.Position); return;
            }
            base.WndProc(ref m);
        }
    }
    public sealed class TranslationView : UserControl {
        private readonly PassiveRichTextBox editor;
        private readonly SlimScrollBar scroll;
        private int contentHeight;
        public event Action<string,Point> TextChosen;
        public bool Selecting { get { return editor.Selecting; } }
        public string Content { get { return editor.Text; } }
        [DllImport("user32.dll",CharSet=CharSet.Auto)] private static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,ref Native.POINT p);
        public TranslationView() {
            SetStyle(ControlStyles.Selectable,false); TabStop=false;
            editor=new PassiveRichTextBox { Dock=DockStyle.Fill };
            scroll=new SlimScrollBar { Dock=DockStyle.Right };
            Controls.Add(editor); Controls.Add(scroll); Padding=new Padding(0,0,3,0);
            editor.TextChosen+=delegate(string s,Point p) { if(TextChosen!=null) TextChosen(s,p); };
            editor.Wheel+=ScrollByWheel;
            editor.ContentsResized+=delegate(object sender,ContentsResizedEventArgs e) { contentHeight=e.NewRectangle.Height+8; SyncScroll(); };
            editor.VScroll+=delegate { SyncScroll(); };
            scroll.ValueChanged+=SetOffset;
            SizeChanged+=delegate { MeasureContent(); };
            ApplyTheme();
        }
        public void ApplyTheme() { BackColor=editor.BackColor=scroll.BackColor=Appearance.Background; editor.ForeColor=Appearance.Foreground; scroll.Invalidate(); }
        private int Offset { get { if(!editor.IsHandleCreated) return 0; var p=new Native.POINT(); SendMessage(editor.Handle,0x4DD,IntPtr.Zero,ref p); return p.Y; } }
        private void SetOffset(int value) { if(!editor.IsHandleCreated) return; var p=new Native.POINT { Y=value }; SendMessage(editor.Handle,0x4DE,IntPtr.Zero,ref p); SyncScroll(); }
        private void MeasureContent() {
            if(!editor.IsHandleCreated) return;
            var last=editor.GetPositionFromCharIndex(editor.TextLength); contentHeight=Math.Max(editor.Font.Height+8,last.Y+Offset+editor.Font.Height+8); SyncScroll();
        }
        private void SyncScroll() { scroll.SetRange(contentHeight,editor.ClientSize.Height,Offset); }
        public void UpdateText(string text) {
            text=text??"";
            if(editor.Text==text || editor.Selecting) return;
            int offset=Offset,sel=editor.SelectionStart,len=editor.SelectionLength;
            bool append=text.StartsWith(editor.Text,StringComparison.Ordinal);
            editor.Text=text;
            if(append) editor.Select(Math.Min(sel,text.Length),Math.Min(len,Math.Max(0,text.Length-sel))); else editor.Select(0,0);
            MeasureContent(); SetOffset(append?offset:0);
        }
        public void ScrollByWheel(int delta) { SetOffset(Math.Max(0,Math.Min(Math.Max(0,contentHeight-editor.Height),Offset-delta/120*Math.Max(1,SystemInformation.MouseWheelScrollLines)*editor.Font.Height))); }
        protected override void OnMouseWheel(MouseEventArgs e) { ScrollByWheel(e.Delta); }
    }
}
