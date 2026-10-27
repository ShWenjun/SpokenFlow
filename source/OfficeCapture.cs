using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SpokenFlow {
    // All objects are acquired from the foreground document window and released in one call.
    // No running-instance lookup or full-document fallback is used.
    public static class OfficeCapture {
        [DllImport("oleacc.dll")] private static extern int AccessibleObjectFromWindow(IntPtr hwnd,uint id,ref Guid iid,[MarshalAs(UnmanagedType.Interface)] out object obj);
        private sealed class ComScope : IDisposable {
            private readonly List<object> objects=new List<object>();
            public dynamic Keep(object o) { if(o!=null && Marshal.IsComObject(o)) objects.Add(o); return o; }
            public void Dispose() { for(int i=objects.Count-1;i>=0;i--) try { Marshal.ReleaseComObject(objects[i]); } catch { } }
        }
        private static object WindowObject(IntPtr foreground,string app) {
            string target=app=="winword"?"_WwG":app=="excel"?"EXCEL7":app=="powerpnt"?"paneClassDC":null;
            if(target==null) return null;
            IntPtr found=IntPtr.Zero,focus=Native.Gui(foreground).Focus;
            if(WindowTools.Class(focus)==target) found=focus;
            else WindowTools.EnumChildWindows(foreground,delegate(IntPtr h,IntPtr state) { if(WindowTools.Class(h)==target) { found=h; return false; } return true; },IntPtr.Zero);
            if(found==IntPtr.Zero) return null;
            Guid iid=new Guid("00020400-0000-0000-C000-000000000046"); object result;
            return AccessibleObjectFromWindow(found,0xfffffff0,ref iid,out result)==0?result:null;
        }
        public static Snapshot Read(IntPtr fg,string app,out string status) { return Access(fg,app,null,null,false,out status); }
        public static string Selected(IntPtr fg,string app) { string status; var r=Access(fg,app,null,null,true,out status); return r==null?null:r.Text; }
        public static void Replace(Snapshot expected,string text) {
            string status; if(Access(expected.Foreground,expected.Adapter.Substring(7),expected,text,false,out status)==null) throw new InvalidOperationException(status);
        }
        private static Snapshot Access(IntPtr fg,string app,Snapshot expected,string replacement,bool selectedOnly,out string status) {
            status=L.T("Office 当前区域暂不可读取");
            if(Native.GetForegroundWindow()!=fg) return null;
            using(var refs=new ComScope()) {
                try {
                    dynamic window=refs.Keep(WindowObject(fg,app)); if(window==null) return null;
                    dynamic application=refs.Keep(window.Application);
                    if(app=="winword") {
                        // A focused ribbon/search box is not a document editing position.
                        if(WindowTools.Class(Native.Gui(fg).Focus)!="_WwG") { status=L.T("请将光标放入 Word 正文"); return null; }
                        dynamic selection=refs.Keep(window.Selection),document=refs.Keep(selection.Document);
                        if(!selectedOnly && (bool)document.ReadOnly) { status=L.T("Word 文档为只读状态"); return null; }
                        dynamic original=refs.Keep(selection.Range),range=refs.Keep(original.Duplicate);
                        if(!selectedOnly) { range.Collapse(1); range.Expand(4); }
                        string raw=(string)range.Text??"",text=TextScopes.WithoutTerminator(raw);
                        if(selectedOnly && (int)range.Start==(int)range.End) return null;
                        if(text.Length>TextCapture.Limit) { status=L.T("当前段落超过 6000 字"); return null; }
                        range.End=(int)range.End-(raw.Length-text.Length);
                        var result=new Snapshot { Foreground=fg,App="Word",Adapter="office:winword",Scope=selectedOnly?L.T("选中文字"):L.T("当前段落"),Identity="word:"+fg+":"+(string)document.FullName+":"+(int)range.StoryType+":"+(int)range.Start,Start=(int)range.Start,End=(int)range.End,Text=text,Anchor=WindowTools.Anchor(fg) };
                        if(expected!=null) {
                            RequireCurrent(result,expected); if((int)document.ProtectionType!=-1) throw new InvalidOperationException(L.T("文档受到保护，请复制译文手动使用。"));
                            // Preserve the paragraph/table terminator; replacing text can unify inline formatting.
                            range.Text=replacement; range.Collapse(0); range.Select();
                        }
                        return result;
                    }
                    if(app=="powerpnt") {
                        dynamic selection=refs.Keep(window.Selection);
                        if((int)selection.Type!=3) { status=L.T("请在 PowerPoint 文本框内编辑文字"); return null; }
                        dynamic selected=refs.Keep(selection.TextRange);
                        dynamic frame=refs.Keep(selected.Parent),shape=refs.Keep(frame.Parent);
                        dynamic range=selectedOnly?selected:refs.Keep(frame.TextRange);
                        dynamic slide=refs.Keep(shape.Parent),presentation=refs.Keep(application.ActivePresentation);
                        string text=(string)range.Text??""; if(text.Length>TextCapture.Limit || selectedOnly && text.Length==0) return null;
                        var result=new Snapshot { Foreground=fg,App="PowerPoint",Adapter="office:powerpnt",Scope=selectedOnly?L.T("选中文字"):L.T("当前文本框"),Identity="ppt:"+fg+":"+(string)presentation.FullName+":"+(int)slide.SlideID+":"+(int)shape.Id,Text=text,Anchor=WindowTools.Anchor(fg) };
                        if(expected!=null) { RequireCurrent(result,expected); if((int)presentation.ReadOnly!=0) throw new InvalidOperationException(L.T("演示文稿为只读状态。")); range.Text=replacement; }
                        return result;
                    }
                    // Excel rejects many object-model calls during edit mode. The UIA adapter below
                    // reads only the active editor; never substitute the previous committed Value2.
                    status=L.T("Excel 请进入单元格编辑模式；不读取整张工作表");
                    return null;
                } catch(InvalidOperationException) { if(expected!=null) throw; return null; }
                catch { if(expected!=null) throw new InvalidOperationException(L.T("Office 当前区域无法核对或正在忙，已取消替换。")); return null; }
            }
        }
        private static void RequireCurrent(Snapshot actual,Snapshot expected) {
            if(!TextCapture.Same(actual,expected) || Native.GetForegroundWindow()!=expected.Foreground) throw new InvalidOperationException(L.T("文档、编辑区域或原文已变化，取消替换。"));
        }
    }
}
