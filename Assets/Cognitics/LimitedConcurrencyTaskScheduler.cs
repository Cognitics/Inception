
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Cognitics
{
    public class LimitedConcurrencyTaskScheduler : TaskScheduler, IDisposable
    {
        public readonly TaskFactory Factory;

        BlockingCollection<Task> Tasks = new BlockingCollection<Task>();
        readonly List<Thread> Threads;

        public LimitedConcurrencyTaskScheduler(int num_threads = 1)
        {
            Factory = new TaskFactory(this);
            Threads = new List<Thread>(num_threads);
            for (int i = 0; i < num_threads; ++i)
            {
                Threads.Add(new Thread(new ThreadStart(Execute)));
                if (!Threads[i].IsAlive)
                    Threads[i].Start();
            }
        }

        private void Execute()
        {
            foreach (var task in Tasks.GetConsumingEnumerable())
                TryExecuteTask(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks() => Tasks.ToArray();
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;

        protected override void QueueTask(Task task)
        {
            if(task != null)
                Tasks.Add(task);
        }

        void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            Tasks.CompleteAdding();
            Tasks.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


    }


}

