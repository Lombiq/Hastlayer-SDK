using System.Diagnostics.CodeAnalysis;

namespace System.Threading.Tasks
{
    public static class TaskExtensions
    {
        /// <summary>
        /// A shortcut for <see cref="Task.ContinueWith(Action{Task, object?}, object?, TaskScheduler)"/> without
        /// having to handle the task objects.
        /// </summary>
        /// <param name="originalTask">Its result is the input of <paramref name="thenFunction"/>.</param>
        /// <param name="thenFunction">Executed once <paramref name="originalTask"/> is complete.</param>
        /// <param name="scheduler">If <see langword="null" /> then <see cref="TaskScheduler.Default"/>.</param>
        public static Task<TOut> ThenAsync<TIn, TOut>(this Task<TIn> originalTask, Func<TIn, TOut> thenFunction, TaskScheduler scheduler = null) =>
            originalTask.ContinueWith(task => thenFunction(task.Result), scheduler ?? TaskScheduler.Default);

        [SuppressMessage("Usage", "VSTHRD103:Call async methods when in an async method", Justification = "ContinueWith only fires when The task is done.")]
        public static Task<TOut> ThenAsync<TIn, TOut>(this Task<TIn> originalTask, Func<TIn, Task<TOut>> thenFunction, TaskScheduler scheduler = null) =>
            originalTask.ContinueWith(task => thenFunction(task.Result), scheduler ?? TaskScheduler.Default).Unwrap();

        public static Task<T> ThenAsync<T>(this Task originalTask, Func<T> thenFunction, TaskScheduler scheduler = null) =>
            originalTask.ContinueWith(task => thenFunction(), scheduler ?? TaskScheduler.Default);

        public static Task<T> ThenAsync<T>(this Task originalTask, Func<Task<T>> thenFunction, TaskScheduler scheduler = null) =>
            originalTask.ContinueWith(task => thenFunction(), scheduler ?? TaskScheduler.Default).Unwrap();

        public static Task ThenAsync(this Task originalTask, Action thenAction, TaskScheduler scheduler = null) =>
            originalTask.ContinueWith(task => thenAction(), scheduler ?? TaskScheduler.Default);
    }
}
