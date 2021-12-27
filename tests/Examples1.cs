using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Reactive.Samples
{
    public class Examples1
    {
        [Test]
        public async Task ShouldUseColdObservable()
        {
            var observable = new ExampleColdObservable();

            await ShouldUseObservable(observable);
        }

        [Test]
        public async Task ShouldUseHotObservable()
        {
            var observable = new ExampleHotObservable();

            await ShouldUseObservable(observable);
        }

        [Test]
        public async Task ShouldUseColdObservableAsHot()
        {
            var observable = new ExampleColdObservable();
            var hotObservable = observable.Publish();
            using var publishSubscription = hotObservable.Connect();

            await ShouldUseObservable(hotObservable);
        }

        [Test]
        public async Task ShouldUseHotObservableAsCold()
        {
            var observable = Observable.Defer(() => new ExampleHotObservable());

            await ShouldUseObservable(observable);
        }

        [Test]
        public async Task ShouldUseDedicatedThread()
        {
            using var scheduler = new EventLoopScheduler();
            var observable = new ExampleColdObservable().ObserveOn(scheduler);

            await ShouldUseObservable(observable);
        }

        [Test]
        public async Task ShouldUseColdObservableFromTimer()
        {
            var observable = Observable.Timer(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100))
                .Select(_ => ColorHelper.GetRandomColor());

            await ShouldUseObservable(observable);
        }

        [Test]
        public async Task ShouldUseHotObservableFromTimer()
        {
            var observable = Observable.Timer(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100))
                .Select(_ => ColorHelper.GetRandomColor())
                .Publish();
            using var publishSubscription = observable.Connect();

            await ShouldUseObservable(observable);
        }

        private async Task ShouldUseObservable(IObservable<Color> observable)
        {
            await Task.Delay(200).ConfigureAwait(true);

            using var subscription1 = observable.Subscribe(item =>
                Console.WriteLine($"Observed item (1) [{Thread.CurrentThread.ManagedThreadId}] {item}"));

            await Task.Delay(150).ConfigureAwait(true);

            using var subscription2 = observable.Subscribe(item =>
                Console.WriteLine($"Observed item (2) [{Thread.CurrentThread.ManagedThreadId}] {item}"));

            await Task.Delay(300);
        }
    }
}