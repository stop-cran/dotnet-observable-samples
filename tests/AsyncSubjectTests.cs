using System;
using System.Drawing;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Reactive.Samples
{
    public class AsyncSubjectTests
    {
        [Test]
        public void ShouldTakeLast()
        {
            var subject = new AsyncSubject<Color>();

            subject.OnNext(ColorHelper.GetRandomColor());
            subject.OnNext(ColorHelper.GetRandomColor());
            subject.OnNext(ColorHelper.GetRandomColor());

            using var subscription = subject.Subscribe(item =>
                Console.WriteLine($"Observed item {item}"));
            
            Console.WriteLine("Completed!");
            subject.OnCompleted();
        }

        [Test]
        public async Task ShouldTakeLastAwait()
        {
            var subject = new AsyncSubject<Color>();

            subject.OnNext(ColorHelper.GetRandomColor());
            subject.OnNext(ColorHelper.GetRandomColor());
            subject.OnNext(ColorHelper.GetRandomColor());

            async Task PrintLast()
            {
                var lastItem = await subject;
                Console.WriteLine($"Observed item {lastItem}");
            }

            var task = PrintLast();

            subject.OnCompleted();

            await task;
        }


        [Test]
        public async Task ShouldTakeLastAwaitFromTimer()
        {
            var lastItem = await Observable.Timer(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100))
                .Select(_ => ColorHelper.GetRandomColor())
                .Take(3)
                .RunAsync(default);

            Console.WriteLine($"Observed item {lastItem}");
        }
    }
}