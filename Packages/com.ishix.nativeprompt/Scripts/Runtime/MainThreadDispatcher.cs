using System;
using System.Threading;

namespace NativePrompt
{
    internal interface IMainThreadDispatcher
    {
        void Post(Action action);
    }

    internal sealed class UnityMainThreadDispatcher : IMainThreadDispatcher
    {
        private readonly SynchronizationContext _context;
        private readonly int _mainThreadId;

        internal UnityMainThreadDispatcher()
        {
            _context = SynchronizationContext.Current;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public void Post(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
            {
                action();
                return;
            }

            if (_context == null)
            {
                throw new InvalidOperationException(
                    "The Unity main-thread synchronization context has not been captured.");
            }

            _context.Post(_ => action(), null);
        }
    }
}
