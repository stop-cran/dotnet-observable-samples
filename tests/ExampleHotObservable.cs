using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Reactive.Samples
{
    public class ExampleHotObservable : IObservable<Color>, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly List<IObserver<Color>> observers = new();
        private readonly ReaderWriterLockSlim observersLock = new();
        private readonly Task generateItems;

        public ExampleHotObservable()
        {
            generateItems = GenerateItems();
        }

        public IDisposable Subscribe(IObserver<Color> observer)
        {
            observersLock.EnterWriteLock();
            try
            {
                observers.Add(observer);
            }
            finally
            {
                observersLock.ExitWriteLock();
            }

            return new DelegateDisposable(() => observers.Remove(observer));
        }

        private async Task GenerateItems()
        {
            Console.WriteLine("Generating items...");
            
            for (;;)
            {
                await Task.Delay(100, _cancellationTokenSource.Token);

                var color = ColorHelper.GetRandomColor();
                
                observersLock.EnterReadLock();
                try
                {
                    foreach (var observer in observers)
                        observer.OnNext(color);
                }
                finally
                {
                    observersLock.ExitReadLock();
                }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }
    }

    public class DelegateDisposable : IDisposable
    {
        private readonly Func<bool> _func;

        public DelegateDisposable(Func<bool> func)
        {
            _func = func;
        }

        public void Dispose()
        {
            _func();
        }
    }
}