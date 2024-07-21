using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utils.Tasks
{
    public class WorkerPool
    {
        public bool IsWorking => tasks.Any(t => t.Status.HasAll(TaskStatus.Running));
        public bool IsComplete => tasks.All(t => t.IsCanceled || t.IsCompleted || t.IsFaulted);

        private readonly List<Task> tasks = new List<Task>();
        private readonly CancellationTokenSource canceller = new CancellationTokenSource();

        public WorkerPool(Action action) : this(action, Environment.ProcessorCount) { }
        public WorkerPool(Action action, int count)
        {
            for(int i = 0; i < count; ++i)
            {
                tasks.Add(Task.Run(action, canceller.Token));
            }
        }
        public WorkerPool(IEnumerable<Action> actions)
        {
            foreach(Action action in actions)
            {
                tasks.Add(Task.Run(action, canceller.Token));
            }
        }
        public void CancelAll()
        {
            canceller.Cancel();
            foreach(Task task in tasks)
            {
                task.Wait();
                task.Dispose();
            }
        }
        public void WaitAll()
        {
            foreach(Task task in tasks)
            {
                task.Wait();
            }
        }
    }
}
