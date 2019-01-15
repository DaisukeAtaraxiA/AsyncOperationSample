using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace AsyncOperationSample
{
    public delegate void ProgressChangedEventHandler(ProgressChangedEventArgs e);
    public delegate void WorkCompletedEventHandler(object sender, WorkCompletedEventArgs e);

    public class WorkCompletedEventArgs : AsyncCompletedEventArgs
    {
        public WorkCompletedEventArgs(Exception e, bool cancelled, object state)
            : base(e, cancelled, state) { }
    }

    class Worker
    {
        private delegate void WorkerEventHandler(AsyncOperation asyncOp);

        private SendOrPostCallback onProgressReportDelegate;
        private SendOrPostCallback onCompletedDelegate;

        private HybridDictionary userStateToLifetime = new HybridDictionary();

        ////////////////////////////////////////////////////////////////////////
        # region Public events

        public event ProgressChangedEventHandler ProgressChanged;
        public event WorkCompletedEventHandler Completed;

        # endregion

        ////////////////////////////////////////////////////////////////////////
        #region Construction and destruction

        public Worker()
        {
            InitializeDelegates();
        }

        protected virtual void InitializeDelegates()
        {
            onProgressReportDelegate = new SendOrPostCallback(ReportProgress);
            onCompletedDelegate = new SendOrPostCallback(WorkCompleted);
        }

        #endregion // Construction and destruction

        ////////////////////////////////////////////////////////////////////////
        public void RunAsync(object taskId)
        {
            var asyncOp = AsyncOperationManager.CreateOperation(taskId);

            lock (userStateToLifetime.SyncRoot)
            {
                if (userStateToLifetime.Contains(taskId))
                {
                    throw new ArgumentException(
                        "Task ID parameter must be unique", "taskId");
                }

                userStateToLifetime[taskId] = asyncOp;
            }

            var workerDelegate = new WorkerEventHandler(RunWorker);
            workerDelegate.BeginInvoke(asyncOp, null, null);
        }

        private bool TaskCancelled(object taskId)
        {
            return userStateToLifetime[taskId] == null;
        }

        public void CancelAsync(object taskId)
        {
            var asyncOp = userStateToLifetime[taskId] as AsyncOperation;
            if (asyncOp != null)
            {
                lock (userStateToLifetime.SyncRoot)
                {
                    userStateToLifetime.Remove(taskId);

                    var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    System.Console.WriteLine($"CancelAsync ({tid})");
                }
            }
        }

        private void RunWorker(AsyncOperation asyncOp)
        {
            Exception e = null;

            // Do something
            var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;
            System.Console.WriteLine("Begin RunWorker");
            System.Console.WriteLine($"Thread Id {tid}");
            for (int i = 0; i < 5; i++)
            {
                if (!TaskCancelled(asyncOp.UserSuppliedState))
                {
                    System.Console.WriteLine($"Running RunWorker ({i + 1} / 5)");
                    Thread.Sleep(2000);
                    var args = new ProgressChangedEventArgs(
                        (int)((float)(i + 1) / (float)5),
                        asyncOp.UserSuppliedState);

                    asyncOp.Post(this.onProgressReportDelegate, args);
                }
            }
            System.Console.WriteLine("End RunWorker");

            this.CompletionMethod(e, false, asyncOp);
        }

        private void WorkCompleted(object operationState)
        {
            var e = operationState as WorkCompletedEventArgs;

            OnCompleted(e);
        }

        private void ReportProgress(object operationState)
        {
            var e = operationState as ProgressChangedEventArgs;

            OnProgressChanged(e);
        }

        private void OnCompleted(WorkCompletedEventArgs e)
        {
            if (Completed != null)
            {
                Completed(this, e);
            }
        }

        private void OnProgressChanged(ProgressChangedEventArgs e)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(e);
            }
        }

        private void CompletionMethod(Exception exception, bool cancelled,
                                      AsyncOperation asyncOp)
        {
            if (!cancelled)
            {
                lock (userStateToLifetime.SyncRoot)
                {
                    userStateToLifetime.Remove(asyncOp.UserSuppliedState);
                }
            }

            var e = new WorkCompletedEventArgs(
                exception, cancelled, asyncOp.UserSuppliedState);

            asyncOp.PostOperationCompleted(onCompletedDelegate, e);
        }
    }
}