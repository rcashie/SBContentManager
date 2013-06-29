using System;
using System.Collections.Generic;
using System.Threading;

namespace AssetBasedContentManager
{
    /// <summary>
    /// An object that executes a task.
    /// </summary>
    public interface ITask
    {
        void Execute();
    }

    /// <summary>
    /// Thread worker object.
    /// </summary>
    public sealed class ThreadWorker : IDisposable
    {
        /// <summary>
        ///  The queue of tasks.
        /// </summary>
        private Queue<ITask> mTaskQueue;

        /// <summary>
        /// Task count.
        /// </summary>
        private int mTaskCount;

        /// <summary>
        /// An empty task used for syncing.
        /// </summary>
        private SyncTask mSyncTask;

        /// <summary>
        /// Thread event wait handle.
        /// </summary>
        private EventWaitHandle mThreadWaitHandle;

        /// <summary>
        /// Caller thread wait handle.
        /// </summary>
        private EventWaitHandle mSyncWaitHandle;

        /// <summary>
        /// The thread object.
        /// </summary>
        private Thread mThread;

#if XBOX
        /// <summary>
        ///  XBOX 360 hardware thread.
        /// </summary>
        private int mProcessorAffinity;
#endif // XBOX

        /// <summary>
        /// Initializes a new instance of the ThreadWorker class.
        /// </summary>
#if XBOX
        public ThreadWorker(int _processorAffinity)
        {
            mProcessorAffinity = _processorAffinity;
#else
        public ThreadWorker()
        {
#endif // XBOX
            mTaskQueue = new Queue<ITask>();

            mThreadWaitHandle = new AutoResetEvent(false);
            mSyncWaitHandle = new AutoResetEvent(false);
            mSyncTask = new SyncTask(mSyncWaitHandle);

            // Start the worker thread
            mThread = new Thread(Work);
            mThread.Start();
            mTaskCount = 0;
        }

        /// <summary>
        /// Returns the current task count.
        /// </summary>
        public int TaskCount
        {
            get { return mTaskCount; }
        }

        /// <summary>
        /// Dispose of used resources.
        /// </summary>
        public void Dispose()
        {
            mThread.Abort();
            mThreadWaitHandle.Dispose();
            mSyncWaitHandle.Dispose();
        }

        /// <summary>
        /// Enqueue a task.
        /// </summary>
        public void EnqueueTask(ITask _task)
        {
            lock (mTaskQueue)
            {
                mTaskQueue.Enqueue(_task);
            }

            Interlocked.Increment(ref mTaskCount);
            mThreadWaitHandle.Set();
        }

        /// <summary>
        /// Blocks the current / calling thread until all tasks are complete.
        /// </summary>
        public void Synchronize()
        {
            // Tell the worker to signal us when it's out of tasks.
            EnqueueTask(mSyncTask);

            // Wait for the signal (Block the current thread)
            mSyncWaitHandle.WaitOne();
        }

        /// <summary>
        /// Work function.
        /// </summary>
        private void Work()
        {
#if XBOX
            Thread.CurrentThread.SetProcessorAffinity(mProcessorAffinity);
#endif  // XBOX

            do
            {
                // No more objects to update - wait for a signal
                if (mTaskCount == 0)
                {
                    mThreadWaitHandle.WaitOne();
                }
                else
                {
                    // Get the next task
                    ITask _nextTask = null;
                    lock (mTaskQueue)
                    {
                        _nextTask = mTaskQueue.Dequeue();
                    }

                    // Execute it.
                    _nextTask.Execute();
                    Interlocked.Decrement(ref mTaskCount);
                }
            }
            while (true);
        }

        /// <summary>
        /// Sync task.
        /// </summary>
        private sealed class SyncTask : ITask
        {
            /// <summary>
            /// Caller thread wait handle.
            /// </summary>
            private EventWaitHandle mWaitHandle;

            public SyncTask(EventWaitHandle _waitHandle)
            {
                mWaitHandle = _waitHandle;
            }

            void ITask.Execute()
            {
                // Signal the waiting thread.
                mWaitHandle.Set();
            }
        }
    }
}
