using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SpokenFlow {
    public static class JavaCapture {
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] private static extern IntPtr LoadLibraryW(string path);
        [DllImport("kernel32.dll")] private static extern bool FreeLibrary(IntPtr module);
        [DllImport("kernel32.dll",CharSet=CharSet.Ansi)] private static extern IntPtr GetProcAddress(IntPtr module,string name);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void StartBridge();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate bool Focus(IntPtr hwnd,out int vm,out long context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate bool Context(int vm,long context,out ContextInfo info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate bool TextInfoCall(int vm,long context,out TextInfo info,int x,int y);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl,CharSet=CharSet.Unicode)] private delegate bool TextRange(int vm,long context,int start,int end,StringBuilder text,short length);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate bool LineBounds(int vm,long context,int index,out int start,out int end);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate bool SelectionInfoCall(int vm,long context,out SelectionInfo info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate bool SelectRange(int vm,long context,int start,int end);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void Release(int vm,long context);
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] private struct ContextInfo {
            [MarshalAs(UnmanagedType.ByValTStr,SizeConst=1024)] public string Name;
            [MarshalAs(UnmanagedType.ByValTStr,SizeConst=1024)] public string Description;
            [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)] public string Role;
            [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)] public string EnglishRole;
            [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)] public string States;
            [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)] public string EnglishStates;
            public int Index,Children,X,Y,Width,Height,Component,Action,Selection,Text,Interfaces;
        }
        [StructLayout(LayoutKind.Sequential)] private struct TextInfo { public int Count,Caret,AtPoint; }
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] private struct SelectionInfo { public int Start,End; [MarshalAs(UnmanagedType.ByValTStr,SizeConst=1024)] public string Text; }
        private static IntPtr module;
        private static Focus focus; private static Context context; private static TextInfoCall info; private static TextRange range;
        private static LineBounds line; private static SelectionInfoCall selection; private static SelectRange select; private static Release release;
        private static DateTime retry=DateTime.MinValue;
        private static T Export<T>(string name) { IntPtr p=GetProcAddress(module,name); if(p==IntPtr.Zero) throw new InvalidOperationException(L.T("Java Access Bridge 版本不兼容")); return (T)(object)Marshal.GetDelegateForFunctionPointer(p,typeof(T)); }
        private static bool Ensure(IntPtr fg) {
            if(module!=IntPtr.Zero) return true; if(DateTime.UtcNow<retry) return false; retry=DateTime.UtcNow.AddSeconds(10);
            string[] paths=new[]{Path.Combine(Environment.SystemDirectory,"WindowsAccessBridge-64.dll"),""};
            try { uint pid; Native.GetWindowThreadProcessId(fg,out pid); string exe=Process.GetProcessById((int)pid).MainModule.FileName; paths[1]=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(exe),"../jbr/bin/WindowsAccessBridge-64.dll")); } catch { }
            foreach(string path in paths) {
                if(!File.Exists(path)) continue; module=LoadLibraryW(path); if(module==IntPtr.Zero) continue;
                try {
                    focus=Export<Focus>("getAccessibleContextWithFocus"); context=Export<Context>("getAccessibleContextInfo");
                    info=Export<TextInfoCall>("getAccessibleTextInfo"); range=Export<TextRange>("getAccessibleTextRange"); line=Export<LineBounds>("getAccessibleTextLineBounds");
                    selection=Export<SelectionInfoCall>("getAccessibleTextSelectionInfo"); select=Export<SelectRange>("selectTextRange"); release=Export<Release>("releaseJavaObject");
                    Export<StartBridge>("Windows_run")(); return true;
                } catch { FreeLibrary(module); module=IntPtr.Zero; }
            }
            return false;
        }
        public static Snapshot Read(IntPtr fg,bool selectedOnly,out string status) {
            status=L.T("PyCharm 需启用 Java Access Bridge 和读屏支持后重启");
            if(!Ensure(fg)) return null;
            int vm; long handle; if(!focus(fg,out vm,out handle) || handle==0) return null;
            try { return ReadContext(fg,vm,handle,selectedOnly,out status); } finally { release(vm,handle); }
        }
        private static Snapshot ReadContext(IntPtr fg,int vm,long handle,bool selectedOnly,out string status) {
            status=L.T("等待 PyCharm 的可编辑文本"); ContextInfo c; TextInfo t;
            if(!context(vm,handle,out c) || c.Text==0 || (c.EnglishRole??"").IndexOf("password",StringComparison.OrdinalIgnoreCase)>=0) return null;
            if(!(c.EnglishStates??"").Contains("focused") || !selectedOnly && !(c.EnglishStates??"").Contains("editable")) return null;
            if(!info(vm,handle,out t,c.X,c.Y)) return null;
            int a,b;
            if(selectedOnly) { SelectionInfo s; if(!selection(vm,handle,out s) || s.Start==s.End) return null; a=s.Start; b=s.End; }
            else { if(t.Count==0) { a=b=0; } else { if(!line(vm,handle,Math.Min(t.Count-1,Math.Max(0,t.Caret)),out a,out b)) return null; b=Math.Min(t.Count,b+1); } }
            if(a<0 || b<a || b>t.Count || b-a>TextCapture.Limit) return null;
            var buffer=new StringBuilder(b-a+1); if(b>a && !range(vm,handle,a,b-1,buffer,(short)buffer.Capacity)) return null;
            string raw=buffer.ToString(),text=selectedOnly?raw:TextScopes.WithoutTerminator(raw); b=a+text.Length;
            return new Snapshot { Foreground=fg,Adapter="java",App="PyCharm",Scope=selectedOnly?L.T("选中文字"):L.T("当前行"),Text=text,Start=a,End=b,Identity="java:"+fg+":"+WindowTools.Title(fg)+":"+c.Name+":"+c.Index+":"+a,Anchor=new Point(c.X+20,c.Y+Math.Min(c.Height,80)) };
        }
        public static void Select(Snapshot expected) {
            // This method runs on the same message-pumping capture thread as Windows_run.
            int vm; long handle; if(!focus(expected.Foreground,out vm,out handle) || handle==0) throw new InvalidOperationException(L.T("PyCharm 焦点已变化。"));
            try {
                string status; var now=ReadContext(expected.Foreground,vm,handle,false,out status);
                if(!TextCapture.Same(now,expected) || Native.GetForegroundWindow()!=expected.Foreground) throw new InvalidOperationException(L.T("PyCharm 当前行已变化。"));
                if(!select(vm,handle,expected.Start,expected.End)) throw new InvalidOperationException(L.T("PyCharm 不允许选择当前文本，请复制译文使用。"));
                SelectionInfo s; if(!selection(vm,handle,out s) || s.Start!=expected.Start || s.End!=expected.End) throw new InvalidOperationException(L.T("无法核对 PyCharm 文本选区。"));
            } finally { release(vm,handle); }
        }
    }
}
