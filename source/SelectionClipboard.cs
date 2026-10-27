using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpokenFlow {
    public static class SelectionRetry {
        public static async Task<string> Read(Func<Task<string>> read,Func<bool> valid,Func<int,Task> wait) {
            for(int attempt=0;attempt<3;attempt++) {
                if(!valid()) return null;
                string text=await read();
                if(!valid()) return null;
                if(text!=null) return text;
                if(attempt<2) await wait(140);
            }
            return null;
        }
    }
    public interface ISelectionClipboard {
        uint Sequence { get; }
        bool TargetOwnsClipboard { get; }
        object Backup();
        void Copy();
        string ReadText();
        void Restore(object backup);
    }
    public static class SelectionCopy {
        // No clearing or sentinel: unchanged clipboard content is never translated.
        // The interface also lets offline tests simulate concurrent clipboard writers.
        public static async Task<string> Read(ISelectionClipboard clipboard,Func<bool> valid,Func<int,Task> wait) {
            uint copiedSequence=0; bool copied=false; object backup=null;
            try {
                if(!valid()) return null;
                uint before=clipboard.Sequence;
                backup=clipboard.Backup();
                if(!valid() || clipboard.Sequence!=before) return null;
                clipboard.Copy();
                for(int i=0;i<12;i++) {
                    await wait(40);
                    uint current=clipboard.Sequence;
                    if(current!=before) {
                        if(!clipboard.TargetOwnsClipboard) return null;
                        copied=true; copiedSequence=current;
                        if(!valid()) return null;
                        string text=clipboard.ReadText();
                        if(clipboard.Sequence!=current || !valid()) return null;
                        if(String.IsNullOrWhiteSpace(text) || text.Length>TextCapture.Limit || !Rules.HasLanguageText(text)) return null;
                        return text.Trim();
                    }
                    if(!valid()) return null;
                }
                return null;
            } catch { return null; }
            finally {
                // Do not overwrite a newer clipboard value from the user/another app.
                try { if(copied && clipboard.Sequence==copiedSequence && clipboard.TargetOwnsClipboard) clipboard.Restore(backup); } catch { }
                var disposable=backup as IDisposable; if(disposable!=null) disposable.Dispose();
            }
        }
    }
    public sealed class WindowsSelectionClipboard : ISelectionClipboard {
        private sealed class SavedClipboard : IDisposable {
            public readonly DataObject Data=new DataObject();
            public readonly List<IDisposable> Owned=new List<IDisposable>();
            public void Dispose() { foreach(var item in Owned) item.Dispose(); }
        }
        [DllImport("user32.dll")] private static extern IntPtr GetClipboardOwner();
        private readonly IntPtr foreground;
        public WindowsSelectionClipboard(IntPtr foreground) { this.foreground=foreground; }
        public uint Sequence { get { return Native.GetClipboardSequenceNumber(); } }
        public bool TargetOwnsClipboard {
            get {
                IntPtr owner=GetClipboardOwner(); if(owner==IntPtr.Zero) return false;
                uint ownerPid,targetPid; Native.GetWindowThreadProcessId(owner,out ownerPid); Native.GetWindowThreadProcessId(foreground,out targetPid);
                return ownerPid!=0 && ownerPid==targetPid || WindowTools.GetAncestor(owner,2)==foreground;
            }
        }
        public object Backup() {
            // Materialize before Ctrl+C; a live IDataObject may use delayed rendering
            // whose backing data disappears when the target replaces the clipboard.
            IDataObject original=Clipboard.GetDataObject();
            if(original==null) return null;
            var snapshot=new SavedClipboard(); long budget=16*1024*1024;
            try {
            foreach(string format in original.GetFormats(false)) {
                object value=original.GetData(format,false);
                if(value==null) throw new InvalidOperationException("Clipboard format unavailable");
                var stream=value as MemoryStream;
                var image=value as Image;
                long size=stream!=null?stream.Length:image!=null?(long)image.Width*image.Height*4:value is byte[]?((byte[])value).Length:value is string?((string)value).Length*2L:0;
                if(value is string[]) foreach(var item in (string[])value) size+=(item??"").Length*2L;
                budget-=size; if(budget<0) throw new InvalidOperationException("Clipboard backup is too large");
                if(stream!=null) { value=new MemoryStream(stream.ToArray()); snapshot.Owned.Add((IDisposable)value); }
                else if(image!=null) { value=image.Clone(); snapshot.Owned.Add((IDisposable)value); }
                else if(value is byte[]) value=((byte[])value).Clone();
                else if(value is string[]) value=((string[])value).Clone();
                else if(!(value is string)) throw new InvalidOperationException("Clipboard format cannot be preserved");
                snapshot.Data.SetData(format,false,value);
            }
            return snapshot;
            } catch { snapshot.Dispose(); throw; }
        }
        public void Copy() {
            if(Native.GetForegroundWindow()!=foreground || Native.ModifiersDown() || Control.MouseButtons!=MouseButtons.None)
                throw new InvalidOperationException("Selection changed");
            var keys=new Native.INPUT[4]; ushort[] values={0x11,0x43,0x43,0x11};
            for(int i=0;i<4;i++) { keys[i].type=1; keys[i].data.keyboard.vk=values[i]; keys[i].data.keyboard.flags=i>=2?2u:0u; }
            if(Native.SendInput(4,keys,Marshal.SizeOf(typeof(Native.INPUT)))!=4) {
                // Release only our synthetic modifiers if the input sequence was partial.
                var release=new Native.INPUT[2];
                for(int i=0;i<2;i++) { release[i].type=1; release[i].data.keyboard.vk=i==0?(ushort)0x43:(ushort)0x11; release[i].data.keyboard.flags=2; }
                Native.SendInput(2,release,Marshal.SizeOf(typeof(Native.INPUT)));
                throw new InvalidOperationException("Copy input failed");
            }
        }
        public string ReadText() { return Clipboard.ContainsText()?Clipboard.GetText():null; }
        public void Restore(object backup) { if(backup==null) Clipboard.Clear(); else Clipboard.SetDataObject(((SavedClipboard)backup).Data,true,3,30); }
    }
}
