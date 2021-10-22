using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Reactive.Samples
{
    public class Tests
    {
        private event Action<int> test = _ => { };

        [SetUp]
        public void Setup()
        {
            x = 0;
        }

        private int x;

        [Test]
        public void ShouldSynchronize()
        {
            var observable = Observable.FromEvent<int>(d => test += d, d => test -= d);

            Thread CreateThread(int i) => new(() =>
            {
                Thread.Sleep(100);
                test(i);
            });

            using var _ = observable.Synchronize().Subscribe(i =>
            {
                TestContext.Out.WriteLine($"Begin processing {i}...");
                Interlocked.Exchange(ref x, i).ShouldBe(0);
                Thread.Sleep(200);
                Interlocked.Exchange(ref x, 0).ShouldBe(i);
                TestContext.Out.WriteLine($"End processing {i}...");
            });

            var threads = Enumerable.Range(1, 10).Select(CreateThread).ToList();

            foreach (var thread in threads)
                thread.Start();
            
            foreach (var thread in threads)
                thread.Join();
        }
    }
}