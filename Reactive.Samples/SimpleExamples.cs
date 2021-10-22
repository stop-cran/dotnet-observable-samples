using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Reactive.Samples
{
    public class SimpleExamples
    {
        [SetUp]
        public void Setup()
        {
            TestContext.Out.WriteLine($"Current thread: {Thread.CurrentThread.ManagedThreadId}");
        }

        [Test]
        public void ShouldPrint()
        {
            using var _ = Observable.Range(3, 5)
                .Subscribe(i =>
                    TestContext.Out.WriteLine($"input: {i}, thread: {Thread.CurrentThread.ManagedThreadId}"));
        }

        [Test]
        public async Task ShouldPrintFromDefaultScheduler()
        {
            using var _ = Observable.Range(3, 5)
                .ObserveOn(Scheduler.Default)
                .Subscribe(i =>
                    TestContext.Out.WriteLine($"input: {i}, thread: {Thread.CurrentThread.ManagedThreadId}"));
            await Task.Delay(100);
        }

        [Test]
        public void ShouldSelect()
        {
            using var _ = Observable.Range(3, 5)
                .TestSelect(i => i * 0.5)
                .TestSelect(i => $"test-{i}")
                .Subscribe(TestContext.Out.WriteLine);
        }

        [Test]
        public void ShouldSelectMany()
        {
            using var _ = Observable.Range(3, 5)
                .TestSelectMany(i => Observable.Range(10 * i, 2))
                .Subscribe(TestContext.Out.WriteLine);
        }

        [Test]
        public void ShouldUnicast()
        {
            var observable = Observable.Return<Func<int>>(() => new Random().Next()).Select(f => f());

            using var subscription1 = observable.Subscribe(TestContext.WriteLine);
            using var subscription2 = observable.Subscribe(TestContext.WriteLine);
        }

        [Test]
        public void ShouldMulticast()
        {
            IAsyncEnumerable<int> k;
            var observable = Observable.Return<Func<int>>(() => new Random().Next()).Select(f => f())
                .Publish();

            using var subscription1 = observable.Subscribe(TestContext.WriteLine);
            using var subscription2 = observable.Subscribe(TestContext.WriteLine);
            using var connection1 = observable.Connect();
        }

        [Test]
        public void ShouldReplaySingleValue()
        {
            var observable = Observable.Return<Func<int>>(() => new Random().Next()).Select(f => f())
                .Replay();

            using var connection1 = observable.Connect();
            using var subscription1 = observable.Subscribe(TestContext.WriteLine);
            using var subscription2 = observable.Subscribe(TestContext.WriteLine);
        }

        [Test]
        public async Task ShouldReplay()
        {
            var observable = Observable.Range(3, 5)
                .Concat(Observable.Range(12, 5)
                    .Delay(TimeSpan.FromMilliseconds(200)))
                .Publish();
            var replay = observable.Replay(2);
            using var replayConnection = replay.Connect();
            using var observableConnection = observable.Connect();

            await Task.Delay(100);

            using var observableSubscription = observable.Subscribe(v => TestContext.WriteLine($"observable: {v}"));
            using var replaySubscription = replay.Subscribe(v => TestContext.WriteLine($"replay: {v}"));

            await replay;
        }

        [Test]
        public async Task ShouldEnumerableAsync()
        {
            var observable = Observable.Interval(TimeSpan.FromMilliseconds(100)).Take(10);
            using var cancel = new CancellationTokenSource(20000);

            await foreach (var i in observable.ToAsyncEnumerable().WithCancellation(cancel.Token))
            {
                if (i == 0)
                    await Task.Delay(500, cancel.Token);
                TestContext.WriteLine(i);
            }
        }

        private volatile int queueSize;

        [Test]
        public async Task ShouldMonitorBottleneck()
        {
            async Task<Unit> TakeBottleneckAction(long i)
            {
                await Task.Delay(300);

                return default;
            }

            await Observable.Interval(TimeSpan.FromMilliseconds(60))
                .Take(10)
                .Select(i =>
                {
                    Interlocked.Increment(ref queueSize);
                    TestContext.WriteLine(queueSize);
                    return i;
                })
                .GroupBy(i => i % 2)
                .SelectMany(gr => gr.SelectMany(TakeBottleneckAction))
                .Select(i =>
                {
                    Interlocked.Decrement(ref queueSize);
                    TestContext.WriteLine(queueSize);
                    return i;
                })
                .RunAsync(default);
        }
        
        
        [Test]
        public async Task ShouldThrottle()
        {
            using var subscription1 = Observable.Interval(TimeSpan.FromMilliseconds(10))
                .Take(100)
                .Sample(TimeSpan.FromMilliseconds(50))
                .Subscribe(i => TestContext.WriteLine(i));

            await Task.Delay(1000);
        }
    }


    public static class ObservableTestExtensions
    {
        public static IObservable<TOut> TestSelect<TIn, TOut>(this IObservable<TIn> source, Func<TIn, TOut> selector) =>
            new SelectObservable<TIn, TOut>(source, selector);

        public static IObservable<TOut> TestSelectMany<TIn, TOut>(this IObservable<TIn> source,
            Func<TIn, IObservable<TOut>> selector) =>
            new SelectManyObservable<TIn, TOut>(source, selector);
    }

    public class SelectObservable<TIn, TOut> : IObservable<TOut>
    {
        private readonly IObservable<TIn> _input;
        private readonly Func<TIn, TOut> _selector;

        public SelectObservable(IObservable<TIn> input, Func<TIn, TOut> selector)
        {
            _input = input;
            _selector = selector;
        }

        public IDisposable Subscribe(IObserver<TOut> observer)
        {
            return _input.Subscribe(new SelectObserver(observer, _selector));
        }

        private class SelectObserver : IObserver<TIn>
        {
            private readonly IObserver<TOut> _input;
            private readonly Func<TIn, TOut> _selector;

            public SelectObserver(IObserver<TOut> input, Func<TIn, TOut> selector)
            {
                _input = input;
                _selector = selector;
            }

            public void OnCompleted()
            {
                _input.OnCompleted();
            }

            public void OnError(Exception error)
            {
                _input.OnError(error);
            }

            public void OnNext(TIn value)
            {
                _input.OnNext(_selector(value));
            }
        }
    }


    public class SelectManyObservable<TIn, TOut> : IObservable<TOut>
    {
        private readonly IObservable<TIn> _input;
        private readonly Func<TIn, IObservable<TOut>> _selector;

        public SelectManyObservable(IObservable<TIn> input, Func<TIn, IObservable<TOut>> selector)
        {
            _input = input;
            _selector = selector;
        }

        public IDisposable Subscribe(IObserver<TOut> observer)
        {
            var subscriptions = new CompositeDisposable();

            subscriptions.Add(_input.Subscribe(new SelectManyObserver(observer, _selector, subscriptions)));

            return subscriptions;
        }

        private class SelectManyObserver : IObserver<TIn>
        {
            private readonly IObserver<TOut> _input;
            private readonly Func<TIn, IObservable<TOut>> _selector;
            private readonly CompositeDisposable _subscriptions;

            public SelectManyObserver(IObserver<TOut> input, Func<TIn, IObservable<TOut>> selector,
                CompositeDisposable subscriptions)
            {
                _input = input;
                _selector = selector;
                _subscriptions = subscriptions;
            }

            public void OnCompleted()
            {
                _input.OnCompleted();
            }

            public void OnError(Exception error)
            {
                _input.OnError(error);
            }

            public void OnNext(TIn value)
            {
                var nexts = _selector(value);

                _subscriptions.Add(nexts.Subscribe(_input));
            }
        }
    }
}