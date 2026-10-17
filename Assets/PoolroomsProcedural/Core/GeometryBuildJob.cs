using System;
using System.Threading;
using System.Threading.Tasks;

namespace Poolrooms.Core
{
    // Main-thread owner of one pure geometry task. Survives a stopped streaming coroutine so
    // a rebuild/re-enable cannot overlap CPU builds or leave an abandoned fault unobserved.
    public sealed class GeometryBuildJob
    {
        private Task<RoomGeometry> task;
        public bool HasWork { get { return task != null; } }
        public bool IsCompleted { get { return task != null && task.IsCompleted; } }

        public void Start(Func<RoomGeometry> build)
        {
            if (task != null) throw new InvalidOperationException("Finish or discard the previous geometry job first.");
            if (build == null) throw new ArgumentNullException(nameof(build));
            task = Task.Run(build);
            // The world can be destroyed before the worker finishes. Observe faults without
            // calling Unity from the thread; a live owner still reports them when taking the result.
            task.ContinueWith(failed => { var observed = failed.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public RoomGeometry TakeCompleted()
        {
            RequireCompleted();
            var completed = task; task = null;
            return completed.GetAwaiter().GetResult();
        }

        public void DiscardCompleted()
        {
            RequireCompleted();
            var observed = task.Exception; task = null;
        }

        private void RequireCompleted()
        {
            if (!IsCompleted) throw new InvalidOperationException("The geometry job has not finished.");
        }
    }
}
