using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SpokenFlow {
    public static class WindowTools {
        public delegate bool EnumWindow(IntPtr window,IntPtr state);
        [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr parent,EnumWindow callback,IntPtr state);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern int GetClassName(IntPtr hwnd,StringBuilder text,int max);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd,StringBuilder text,int max);
        [DllImport("user32.dll")] public static extern bool IsChild(IntPtr parent,IntPtr child);
        [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr hwnd,uint flags);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd,out Native.RECT rect);
        [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="SendMessageTimeoutW",SetLastError=true)] private static extern IntPtr Message(IntPtr h,uint msg,IntPtr w,IntPtr l,uint flags,uint timeout,out IntPtr result);
        [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="SendMessageTimeoutW",SetLastError=true)] private static extern IntPtr TextMessage(IntPtr h,uint msg,IntPtr w,StringBuilder l,uint flags,uint timeout,out IntPtr result);
        public static string Class(IntPtr hwnd) { var b=new StringBuilder(256); GetClassName(hwnd,b,b.Capacity); return b.ToString(); }
        public static string Title(IntPtr hwnd) { var b=new StringBuilder(512); GetWindowText(hwnd,b,b.Capacity); return b.ToString(); }
        public static long Send(IntPtr h,uint msg,long w,long l) { IntPtr result; if(Message(h,msg,(IntPtr)w,(IntPtr)l,2,200,out result)==IntPtr.Zero) throw new InvalidOperationException(L.T("目标程序暂时无响应")); return result.ToInt64(); }
        public static string ReadText(IntPtr h,int limit) {
            long length=Send(h,14,0,0); if(length<0 || length>limit) return null;
            var b=new StringBuilder((int)length+2); IntPtr result;
            if(TextMessage(h,13,(IntPtr)b.Capacity,b,2,200,out result)==IntPtr.Zero) return null; return b.ToString();
        }
        public static string ProcessName(IntPtr hwnd) { uint pid; Native.GetWindowThreadProcessId(hwnd,out pid); try { return Process.GetProcessById((int)pid).ProcessName; } catch { return ""; } }
        public static bool IsBrowser(string app) { return app=="msedge" || app=="chrome" || app=="firefox"; }
        public static bool IsEditor(string app) { return app=="code" || app=="notepad++" || app.StartsWith("pycharm") || app=="notepad"; }
        public static bool IsOffice(string app) { return app=="winword" || app=="excel" || app=="powerpnt" || app=="wps" || app=="et" || app=="wpp"; }
        public static Point Anchor(IntPtr fg) {
            var g=Native.Gui(fg); if(g.Caret!=IntPtr.Zero) { var p=new Native.POINT { X=g.Rect.Left,Y=g.Rect.Bottom+12 }; if(Native.ClientToScreen(g.Caret,ref p)) return new Point(p.X,p.Y); }
            Native.RECT r; if(GetWindowRect(g.Focus,out r)) return new Point(r.Left+20,r.Bottom+8); return Cursor.Position;
        }
    }
    public static class TextScopes {
        public static void Line(string text,int caret,out int start,out int end) {
            caret=Math.Max(0,Math.Min(text.Length,caret)); start=caret; end=caret;
            while(start>0 && text[start-1]!='\r' && text[start-1]!='\n') start--;
            while(end<text.Length && text[end]!='\r' && text[end]!='\n') end++;
        }
        public static string WithoutTerminator(string text) { return (text??"").TrimEnd('\r','\n','\a'); }
    }
    public static class ScintillaCapture {
        public static Snapshot Read(IntPtr fg,bool selectedOnly=false) {
            var h=Native.Gui(fg).Focus; if(WindowTools.Class(h)!="Scintilla") return null;
            if(WindowTools.Send(h,2140,0,0)!=0 && !selectedOnly) return null;
            string all=WindowTools.ReadText(h,2000000); if(all==null) return null;
            int cp=(int)WindowTools.Send(h,2137,0,0); Encoding encoding=cp==65001?Encoding.UTF8:Encoding.GetEncoding(cp==0?System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage:cp);
            long pos=WindowTools.Send(h,2008,0,0), anchor=WindowTools.Send(h,2009,0,0);
            byte[] bytes=encoding.GetBytes(all); if(pos<0 || pos>bytes.Length || anchor<0 || anchor>bytes.Length) return null;
            int start,end;
            if(selectedOnly) { if(pos==anchor) return null; start=encoding.GetCharCount(bytes,0,(int)Math.Min(pos,anchor)); end=encoding.GetCharCount(bytes,0,(int)Math.Max(pos,anchor)); }
            else TextScopes.Line(all,encoding.GetCharCount(bytes,0,(int)pos),out start,out end);
            string text=all.Substring(start,end-start); if(text.Length>TextCapture.Limit) return null;
            long doc=WindowTools.Send(h,2357,0,0);
            return new Snapshot { Foreground=fg,App="Notepad++",Text=text,Adapter="scintilla",Scope=selectedOnly?L.T("选中文字"):L.T("当前行"),Identity=fg+":"+h+":"+doc+":"+start,NativeHandle=h,Start=start,End=end,Anchor=WindowTools.Anchor(fg) };
        }
        public static void Select(Snapshot source) {
            var now=Read(source.Foreground); if(!TextCapture.Same(now,source)) throw new InvalidOperationException(L.T("当前行已变化，取消替换。"));
            var h=now.NativeHandle; string all=WindowTools.ReadText(h,2000000); if(all==null) throw new InvalidOperationException(L.T("无法核对编辑内容。"));
            int cp=(int)WindowTools.Send(h,2137,0,0); var enc=cp==65001?Encoding.UTF8:Encoding.GetEncoding(cp==0?System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage:cp);
            int a=enc.GetByteCount(all.Substring(0,now.Start)),b=enc.GetByteCount(all.Substring(0,now.End));
            WindowTools.Send(h,2160,a,b);
            if(WindowTools.Send(h,2143,0,0)!=a || WindowTools.Send(h,2145,0,0)!=b) throw new InvalidOperationException(L.T("无法准确选中当前行。"));
        }
    }
}
