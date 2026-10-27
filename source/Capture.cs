using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using System.Windows.Forms;

namespace SpokenFlow {
    public static class Native {
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
        [DllImport("user32.dll")] public static extern bool GetGUIThreadInfo(uint id, ref GUIINFO info);
        [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hwnd, ref POINT p);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hwnd, int id);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] public static extern uint GetClipboardSequenceNumber();
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] public static extern uint SendInput(uint count, INPUT[] input, int size);
        [DllImport("imm32.dll")] public static extern IntPtr ImmGetContext(IntPtr hwnd);
        [DllImport("imm32.dll")] public static extern bool ImmReleaseContext(IntPtr hwnd, IntPtr imc);
        [DllImport("imm32.dll", CharSet=CharSet.Unicode)] public static extern int ImmGetCompositionStringW(IntPtr imc, uint index, IntPtr buffer, uint length);
        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] public struct GUIINFO { public int Size; public uint Flags; public IntPtr Active, Focus, Capture, MenuOwner, MoveSize, Caret; public RECT Rect; }
        [StructLayout(LayoutKind.Sequential)] public struct INPUT { public uint type; public UNION data; }
        [StructLayout(LayoutKind.Explicit)] public struct UNION { [FieldOffset(0)] public KEY keyboard; [FieldOffset(0)] public MOUSE mouse; }
        [StructLayout(LayoutKind.Sequential)] public struct KEY { public ushort vk, scan; public uint flags, time; public UIntPtr extra; }
        [StructLayout(LayoutKind.Sequential)] public struct MOUSE { public int x,y; public uint mouseData,flags,time; public UIntPtr extra; }
        public static bool ModifiersDown() { return (GetAsyncKeyState(0x11) & 0x8000) != 0 || (GetAsyncKeyState(0x12) & 0x8000) != 0 || (GetAsyncKeyState(0x10) & 0x8000) != 0; }
        public static GUIINFO Gui(IntPtr hwnd) { uint pid; uint tid=GetWindowThreadProcessId(hwnd,out pid); var g=new GUIINFO(); g.Size=Marshal.SizeOf(g); GetGUIThreadInfo(tid,ref g); return g; }
        public static bool Composing(IntPtr foreground) {
            var g=Gui(foreground); if(g.Focus==IntPtr.Zero) return false;
            IntPtr ctx=ImmGetContext(g.Focus); if(ctx==IntPtr.Zero) return false;
            try { return ImmGetCompositionStringW(ctx,8,IntPtr.Zero,0)>0; } finally { ImmReleaseContext(g.Focus,ctx); }
        }
        public static void Paste() {
            var keys=new INPUT[4]; ushort[] v={0x11,0x56,0x56,0x11};
            for(int i=0;i<4;i++) { keys[i].type=1; keys[i].data.keyboard.vk=v[i]; keys[i].data.keyboard.flags=i>=2?2u:0u; }
            if(SendInput(4,keys,Marshal.SizeOf(typeof(INPUT)))!=4) throw new InvalidOperationException(L.T("无法向目标程序粘贴。请确认它没有以管理员权限运行。"));
        }
    }
    public sealed class Snapshot {
        public AutomationElement Element;
        public string Identity, Text, App;
        public string Adapter="uia",Scope=L.T("当前输入框");
        public TextPatternRange Range;
        public IntPtr NativeHandle;
        public int Start,End;
        public IntPtr Foreground;
        public Point Anchor;
        public Snapshot WithText(string text) { var copy=(Snapshot)MemberwiseClone(); copy.Text=text; copy.End=Start+text.Length; return copy; }
    }
    public static class TextCapture {
        public const int Limit=6000;
        public static Snapshot Read(out string status) {
            status=L.T("等待可读取的输入框");
            var foreground=Native.GetForegroundWindow();
            if(foreground==IntPtr.Zero) return null;
            string app=WindowTools.ProcessName(foreground).ToLowerInvariant();
            if(app=="spokenflow" && Process.GetCurrentProcess().Id==GetPid(foreground)) return null;
            if(Native.Composing(foreground)) { status=L.T("正在选词，等待文字上屏"); return null; }
            if(app=="notepad++") { var native=ScintillaCapture.Read(foreground); if(native!=null) return native; }
            if(app.StartsWith("pycharm")) { var java=JavaCapture.Read(foreground,false,out status); if(java!=null) return java; }
            if(app=="winword" || app=="powerpnt") return OfficeCapture.Read(foreground,app,out status);
            var e=AutomationElement.FocusedElement;
            if(e==null) return null;
            var c=e.Current;
            if(c.ProcessId==Process.GetCurrentProcess().Id) { status=L.T("已开启；请切换到其他软件的输入框"); return null; }
            if(c.IsPassword) { status=L.T("密码框不读取"); return null; }
            if(!c.IsEnabled || !c.HasKeyboardFocus || !Belongs(e,foreground)) return null;
            // Some browser editors focus a text leaf inside the actual editable document.
            for(int depth=0;e!=null && depth<8;depth++,e=TreeWalker.RawViewWalker.GetParent(e)) {
                c=e.Current; if(c.IsPassword || c.ControlType==ControlType.Window) break;
                if(!AcceptsEditor(c.ControlType,WindowTools.IsBrowser(app))) continue;
                bool paragraph=app=="winword"||app=="wps";
                bool line=WindowTools.IsEditor(app);
                bool cell=app=="excel"||app=="et";
                if(cell && c.ControlType!=ControlType.Edit) continue;
                if(app=="excel" && c.AutomationId!="CellEdit" && c.ClassName!="EXCEL6" && WindowTools.Class(Native.Gui(foreground).Focus)!="EXCEL6") continue;
                if(app=="weixin"||app=="wechat") {
                    if(app=="weixin" && !(c.AutomationId??"").EndsWith("chat_input_field",StringComparison.Ordinal)) continue;
                    if(app=="wechat" && (c.ClassName??"").IndexOf("RichEdit",StringComparison.OrdinalIgnoreCase)<0) continue;
                }
                // Chromium's new-tab search box is an editable ComboBox. Read only
                // its writable ValuePattern; never the suggestions or page ancestor.
                if(c.ControlType==ControlType.ComboBox && !HasWritableValue(e)) {
                    status=L.T("此浏览器组合框没有可写文本值，已跳过"); return null;
                }
                string value=null; TextPatternRange range=null;
                object obj;
                if((paragraph||line) && e.TryGetCurrentPattern(TextPattern.Pattern,out obj)) {
                    var pattern=(TextPattern)obj; var selections=pattern.GetSelection(); if(selections.Length!=1) continue;
                    range=selections[0].Clone(); range.MoveEndpointByRange(TextPatternRangeEndpoint.End,range,TextPatternRangeEndpoint.Start);
                    range.ExpandToEnclosingUnit(line?TextUnit.Line:TextUnit.Paragraph);
                    object ro=range.GetAttributeValue(TextPattern.IsReadOnlyAttribute); if(!(ro is bool)|| (bool)ro) continue;
                    value=range.GetText(Limit+1);
                    // Remove structural paragraph boundaries from the editable replacement range.
                    for(int n=0;n<3 && value.Length>0 && (value.EndsWith("\r")||value.EndsWith("\n")||value.EndsWith("\a"));n++) {
                        range.MoveEndpointByUnit(TextPatternRangeEndpoint.End,TextUnit.Character,-1); value=range.GetText(Limit+1);
                    }
                } else if(!paragraph && !line) value=ReadText(e);
                else if(line && e.TryGetCurrentPattern(ValuePattern.Pattern,out obj)) {
                    // VS Code's accessibility editor exposes a bounded current-line value.
                    var pattern=(ValuePattern)obj;
                    if(!pattern.Current.IsReadOnly && (app=="code" || c.ClassName=="Edit")) {
                        string v=pattern.Current.Value;
                        if(v.IndexOf('\r')<0 && v.IndexOf('\n')<0) value=v;
                    }
                }
                if(value==null) { if(c.ControlType==ControlType.ComboBox) return null; continue; }
                if(cell && value.TrimStart().StartsWith("=",StringComparison.Ordinal)) { status=L.T("公式单元格不自动翻译，避免改动公式"); return null; }
                if(value.Length>Limit) { status=L.T("当前区域超过 6000 字，已暂停翻译"); return null; }
                string identity=c.ProcessId+":"+String.Join(".",Array.ConvertAll(e.GetRuntimeId(),x=>x.ToString()));
                if(range!=null) {
                    var pattern=(TextPattern)e.GetCurrentPattern(TextPattern.Pattern); var prefix=pattern.DocumentRange.Clone();
                    prefix.MoveEndpointByRange(TextPatternRangeEndpoint.End,range,TextPatternRangeEndpoint.Start);
                    string before=prefix.GetText(2000001); if(before.Length>2000000) { status=L.T("文档过长，无法准确定位当前段落"); return null; }
                    identity+=":"+before.Length;
                }
                if(cell||WindowTools.IsEditor(app)) identity+=":"+WindowTools.Title(foreground)+":"+c.Name;
                string scope=paragraph?L.T("当前段落"):line?L.T("当前行"):cell?L.T("编辑中的单元格"):app=="powerpnt"||app=="wpp"?L.T("当前文本框"):L.T("当前输入框");
                // Never fall back to the entire Office document when no scoped interface exists.
                if((app=="powerpnt"||app=="wpp") && c.ControlType!=ControlType.Edit) continue;
                return new Snapshot { Element=e,Range=range,Identity=identity,Text=value,App=app,Foreground=foreground,Anchor=Anchor(e,foreground),Scope=scope };
            }
            status=app=="weixin"||app=="wechat"?L.T("微信未暴露聊天输入框接口；此版本未完成适配"):app.StartsWith("pycharm")?L.T("PyCharm 需启用 Java Access Bridge 与读屏支持"):app=="code"?L.T("VS Code 需开启编辑器辅助功能支持"):L.T("此编辑区域未提供可准确定位的文本接口");
            return null;
        }
        private static int GetPid(IntPtr h) { uint pid; Native.GetWindowThreadProcessId(h,out pid); return (int)pid; }
        public static bool AcceptsEditor(ControlType type,bool browser) {
            return type==ControlType.Edit || type==ControlType.Document || browser && type==ControlType.ComboBox;
        }
        private static bool HasWritableValue(AutomationElement e) {
            object pattern; return e.TryGetCurrentPattern(ValuePattern.Pattern,out pattern) && !((ValuePattern)pattern).Current.IsReadOnly;
        }
        public static bool Belongs(AutomationElement element,IntPtr foreground) {
            var e=element;
            for(int i=0;e!=null && i<40;i++,e=TreeWalker.RawViewWalker.GetParent(e)) {
                IntPtr h=(IntPtr)e.Current.NativeWindowHandle;
                if(h!=IntPtr.Zero && (h==foreground || WindowTools.IsChild(foreground,h))) return true;
                if(e.Current.ControlType==ControlType.Window) return false;
            }
            return false;
        }
        public static string ReadText(AutomationElement e) {
            if(e.Current.IsPassword) return null;
            object obj;
            if(e.TryGetCurrentPattern(ValuePattern.Pattern,out obj)) {
                var p=(ValuePattern)obj;
                if(p.Current.IsReadOnly) return null;
                return p.Current.Value;
            }
            if(e.Current.ControlType==ControlType.ComboBox) return null;
            if(e.TryGetCurrentPattern(TextPattern.Pattern,out obj)) {
                var range=((TextPattern)obj).DocumentRange;
                var readOnly=range.GetAttributeValue(TextPattern.IsReadOnlyAttribute);
                if(!(readOnly is bool) || (bool)readOnly) return null;
                return range.GetText(Limit+1);
            }
            return null;
        }
        public static Point Anchor(AutomationElement e, IntPtr fg) {
            try {
                var g=Native.Gui(fg);
                if(g.Caret!=IntPtr.Zero) {
                    var pt=new Native.POINT { X=g.Rect.Left, Y=g.Rect.Bottom+10 };
                    if(Native.ClientToScreen(g.Caret,ref pt)) return new Point(pt.X,pt.Y);
                }
                object o;
                if(e.TryGetCurrentPattern(TextPattern.Pattern,out o)) {
                    var ranges=((TextPattern)o).GetSelection();
                    if(ranges.Length>0) {
                        var r=ranges[0].Clone(); r.ExpandToEnclosingUnit(TextUnit.Character);
                        var rects=r.GetBoundingRectangles();
                        if(rects.Length>0) return new Point((int)rects[0].Left,(int)rects[0].Bottom+10);
                    }
                }
                var box=e.Current.BoundingRectangle;
                return new Point((int)box.Left,(int)box.Bottom+8);
            } catch { return Cursor.Position; }
        }
        public static bool SameField(Snapshot a,Snapshot b) { return a!=null && b!=null && a.Foreground==b.Foreground && a.Identity==b.Identity; }
        public static bool Same(Snapshot a,Snapshot b) { return SameField(a,b) && a.Text==b.Text; }
        public static string Replace(Snapshot expected,string replacement) {
            var now=CaptureThread.Run(delegate { string description; return Read(out description); }).GetAwaiter().GetResult();
            if(!Same(now,expected)) throw new InvalidOperationException(L.T("输入框或原文已变化，请等待最新译文后再替换。"));
            if(now.Adapter.StartsWith("office:")) {
                CaptureThread.Run(delegate { OfficeCapture.Replace(now,replacement); return true; }).GetAwaiter().GetResult();
                return L.T("已替换")+now.Scope+L.T("；Ctrl+Alt+Z 可恢复原文");
            }
            if(now.Adapter=="scintilla" || now.Adapter=="java") {
                CaptureThread.Run(delegate { if(now.Adapter=="scintilla") ScintillaCapture.Select(now); else JavaCapture.Select(now); return true; }).GetAwaiter().GetResult();
                if(Native.GetForegroundWindow()!=now.Foreground) throw new InvalidOperationException(L.T("焦点已改变，取消替换。"));
                PasteText(replacement); return L.T("已替换")+now.Scope+L.T("；Ctrl+Alt+Z 可恢复原文");
            }
            object pattern;
            if(now.Range==null && now.Element.TryGetCurrentPattern(ValuePattern.Pattern,out pattern)) {
                if(Native.GetForegroundWindow()!=now.Foreground) throw new InvalidOperationException(L.T("焦点已改变，取消替换。"));
                ((ValuePattern)pattern).SetValue(replacement);
                return L.T("已替换整段；Ctrl+Alt+Z 可恢复原文");
            }
            if(!now.Element.TryGetCurrentPattern(TextPattern.Pattern,out pattern)) throw new InvalidOperationException(L.T("此输入框不支持替换。"));
            var text=(TextPattern)pattern;
            var range=now.Range??text.DocumentRange;
            if(range.GetText(Limit+1)!=expected.Text) throw new InvalidOperationException(L.T("文字已变化，取消替换。"));
            // Select only this control's verified document range; never send a global Ctrl+A.
            range.Select();
            var selected=text.GetSelection();
            if(selected.Length!=1 || selected[0].GetText(Limit+1)!=expected.Text) throw new InvalidOperationException(L.T("无法准确选中原文，取消替换。"));
            var focus=AutomationElement.FocusedElement;
            if(focus==null || !Automation.Compare(focus,now.Element) || Native.GetForegroundWindow()!=now.Foreground) throw new InvalidOperationException(L.T("焦点已改变，取消替换。"));
            PasteText(replacement);
            return L.T("已发送粘贴；Ctrl+Alt+Z 可恢复原文");
        }
        private static void PasteText(string replacement) {
            IDataObject backup=Clipboard.GetDataObject();
            Clipboard.SetText(replacement);
            uint sequence=Native.GetClipboardSequenceNumber();
            try { Native.Paste(); }
            finally {
                var restore=new System.Windows.Forms.Timer { Interval=700 };
                restore.Tick+=delegate {
                    restore.Stop(); restore.Dispose();
                    try { if(Native.GetClipboardSequenceNumber()==sequence) { if(backup==null) Clipboard.Clear(); else Clipboard.SetDataObject(backup,true); } } catch { }
                };
                restore.Start();
            }
        }
    }
}
