using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Reactive.Samples
{
    public class ExampleColdObservable : IObservable<Color>
    {
        public IDisposable Subscribe(IObserver<Color> observer)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            GenerateItems(observer, cancellationTokenSource.Token);

            return new CancellationDisposable(cancellationTokenSource);
        }

        private async Task GenerateItems(IObserver<Color> observer, CancellationToken cancel)
        {
            Console.WriteLine("Generating items...");
            for (;;)
            {
                await Task.Delay(100, cancel);
                observer.OnNext(ColorHelper.GetRandomColor());
            }
        }
    }
}