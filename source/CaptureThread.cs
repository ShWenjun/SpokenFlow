using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpokenFlow {
    public static class CaptureThread {
        private static readonly Control dispatcher;
        static CaptureThread() {
            Control control=null; var ready=new ManualResetEvent(false);
            var worker=new Thread(delegate() { control=new Control(); IntPtr handle=control.Handle; ready.Set(); Application.Run(); control.Dispose(); });
            worker.IsBackground=true; worker.Name="SpokenFlow accessibility"; worker.SetApartmentState(ApartmentState.STA); worker.Start(); ready.WaitOne(); ready.Dispose(); dispatcher=control;
        }
        public static Task<T> Run<T>(Func<T> operation) {
            var done=new TaskCompletionSource<T>();
            dispatcher.BeginInvoke((Action)delegate { try { done.SetResult(operation()); } catch(Exception ex) { done.SetException(ex); } });
            return done.Task;
        }
    }
}
