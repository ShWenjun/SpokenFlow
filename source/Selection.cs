using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Windows.Forms;

namespace SpokenFlow {
    // The callback only reports a completed gesture. UI Automation runs later, off the UI thread.
    public sealed class SelectionMouse : IDisposable {
        private delegate IntPtr Callback(int code,IntPtr message,IntPtr data);
        [DllImport("user32.dll",SetLastError=true)] private static extern IntPtr SetWindowsHookEx(int id,Callback callback,IntPtr module,uint thread);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
        [DllImport("kernel32.dll",CharSet=CharSet.Auto)] private static extern IntPtr GetModuleHandle(string name);
        [StructLayout(LayoutKind.Sequential)] private struct MouseData { public Native.POINT Point; public uint Mouse,Flags,Time; public UIntPtr Extra; }
        private readonly Callback callback,keyboardCallback;
        private IntPtr hook,keyboardHook;
        private Point down,previous;
        private long lastDown;
        private bool doubleClick,pressed;
        public event Action<Point,Point,bool> Released;
        public event Action<Point> Started;
        public event Action KeyPressed;
        public SelectionMouse() {
            callback=OnMouse;
            keyboardCallback=OnKeyboard;
            hook=SetWindowsHookEx(14,callback,GetModuleHandle(null),0);
            if(hook==IntPtr.Zero) throw new InvalidOperationException(L.T("无法启动划词检测，请重启程序后重试。"));
            keyboardHook=SetWindowsHookEx(13,keyboardCallback,GetModuleHandle(null),0);
            if(keyboardHook==IntPtr.Zero) { Dispose(); throw new InvalidOperationException(L.T("无法启动划词取消检测，请重启程序后重试。")); }
        }
        private IntPtr OnKeyboard(int code,IntPtr message,IntPtr data) {
            // Only invalidate pending work; never retain key values or typed text.
            if(code>=0 && (message.ToInt32()==0x100 || message.ToInt32()==0x104)) {
                try { if((Marshal.ReadInt32(data,8)&0x10)==0 && KeyPressed!=null) KeyPressed(); } catch { }
            }
            return CallNextHookEx(keyboardHook,code,message,data);
        }
        private IntPtr OnMouse(int code,IntPtr message,IntPtr data) {
            if(code>=0 && (message.ToInt32()==0x201 || message.ToInt32()==0x202 || message.ToInt32()==0x204 || message.ToInt32()==0x207 || message.ToInt32()==0x20a)) {
                try {
                    var m=(MouseData)Marshal.PtrToStructure(data,typeof(MouseData)); var p=new Point(m.Point.X,m.Point.Y);
                    if(message.ToInt32()==0x201) {
                        long now=Environment.TickCount & 0xffffffffL;
                        doubleClick=now-lastDown<=SystemInformation.DoubleClickTime && Math.Abs(p.X-previous.X)<=SystemInformation.DoubleClickSize.Width && Math.Abs(p.Y-previous.Y)<=SystemInformation.DoubleClickSize.Height;
                        previous=down=p; lastDown=now; pressed=true;
                        if(Started!=null) Started(p);
                    } else if(message.ToInt32()==0x202) {
                        bool selected=pressed && (doubleClick || Math.Abs(p.X-down.X)>3 || Math.Abs(p.Y-down.Y)>3);
                        pressed=false; if(Released!=null) Released(down,p,selected);
                    } else { pressed=false; if(Started!=null) Started(p); }
                } catch { }
            }
            return CallNextHookEx(hook,code,message,data);
        }
        public void Dispose() {
            if(hook!=IntPtr.Zero) { UnhookWindowsHookEx(hook); hook=IntPtr.Zero; }
            if(keyboardHook!=IntPtr.Zero) { UnhookWindowsHookEx(keyboardHook); keyboardHook=IntPtr.Zero; }
        }
    }
    public static class SelectionCapture {
        public static string Read(Point start,Point point,IntPtr foreground) {
            if(Native.GetForegroundWindow()!=foreground) return null;
            uint pid; Native.GetWindowThreadProcessId(foreground,out pid);
            if(pid==Process.GetCurrentProcess().Id) return null;
            try {
                string app=WindowTools.ProcessName(foreground).ToLowerInvariant();
                if(app=="notepad++") { var sci=ScintillaCapture.Read(foreground,true); if(sci!=null) return sci.Text; }
                if(app.StartsWith("pycharm")) { string state; var java=JavaCapture.Read(foreground,true,out state); if(java!=null) return java.Text; }
                foreach(var hit in new[]{point,start}) {
                    var e=AutomationElement.FromPoint(new System.Windows.Point(hit.X,hit.Y));
                    if(e==null || !TextCapture.Belongs(e,foreground)) continue;
                    for(int i=0;e!=null && i<10;i++,e=TreeWalker.RawViewWalker.GetParent(e)) {
                        if(e.Current.IsPassword) return null;
                        string text=ReadElement(e,start,point); if(text!=null) return text;
                    }
                }
                var focused=AutomationElement.FocusedElement;
                if(focused!=null && TextCapture.Belongs(focused,foreground) && !focused.Current.IsPassword) return ReadElement(focused,start,point);
            } catch { }
            return null;
        }
        public static bool NearSelection(System.Windows.Rect[] rectangles,Point start,Point end) {
            if(rectangles==null) return false;
            foreach(var r in rectangles) {
                if(r.IsEmpty || r.Width<=0 || r.Height<=0) continue;
                foreach(var p in new[]{start,end})
                    if(p.X>=r.Left-20 && p.X<=r.Right+20 && p.Y>=r.Top-20 && p.Y<=r.Bottom+20) return true;
            }
            return false;
        }
        // An exposed empty selection must not fall back to Ctrl+C: some editors
        // copy the whole current line when there is no actual selection.
        public static bool CanCopy(Point start,Point end,IntPtr foreground) {
            if(Native.GetForegroundWindow()!=foreground) return false;
            try {
                var focus=AutomationElement.FocusedElement;
                if(focus!=null && focus.Current.IsPassword) return false;
                var e=AutomationElement.FromPoint(new System.Windows.Point(start.X,start.Y));
                if(e==null || !TextCapture.Belongs(e,foreground)) return false;
                for(int i=0;e!=null && i<10;i++,e=TreeWalker.RawViewWalker.GetParent(e)) {
                    if(e.Current.IsPassword) return false;
                    if(e.Current.ControlType==ControlType.ScrollBar || e.Current.ControlType==ControlType.Button || e.Current.ControlType==ControlType.TitleBar) return false;
                    object obj;
                    if(e.TryGetCurrentPattern(TextPattern.Pattern,out obj)) {
                        var pattern=(TextPattern)obj;
                        if(pattern.SupportedTextSelection==SupportedTextSelection.None) continue;
                        var ranges=pattern.GetSelection();
                        if(ranges==null || ranges.Length!=1) return false;
                        string text=ranges[0].GetText(TextCapture.Limit+1);
                        return !String.IsNullOrWhiteSpace(text) && text.Length<=TextCapture.Limit && NearSelection(ranges[0].GetBoundingRectangles(),start,end);
                    }
                    if(e.Current.ControlType==ControlType.Window) break;
                }
                return true;
            } catch { return false; }
        }
        private static string ReadElement(AutomationElement element,Point start,Point point) {
            try {
            object obj; if(!element.TryGetCurrentPattern(TextPattern.Pattern,out obj)) return null;
            var pattern=(TextPattern)obj;
            if(pattern.SupportedTextSelection==SupportedTextSelection.None) return null;
            var ranges=pattern.GetSelection(); if(ranges==null || ranges.Length!=1) return null;
            string value=ranges[0].GetText(TextCapture.Limit+1);
            if(String.IsNullOrWhiteSpace(value) || value.Length>TextCapture.Limit) return null;
            // Ignore an unrelated old selection exposed elsewhere in the same document.
            return NearSelection(ranges[0].GetBoundingRectangles(),start,point)?value.Trim():null;
            } catch { return null; }
        }
    }
}
