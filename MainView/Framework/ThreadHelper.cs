using System;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MainView.Framework
{
    public class ThreadHelper
    {
        public static DispatcherAwaiter UIThread => new DispatcherAwaiter();
    }

    public struct DispatcherAwaiter : INotifyCompletion
    {
        public bool IsCompleted => Application.Current.Dispatcher.CheckAccess();

        public void OnCompleted(Action continuation) => Application.Current.Dispatcher.Invoke(continuation);

        public void GetResult() { }

        public DispatcherAwaiter GetAwaiter()
        {
            return this;
        }
    }
}
