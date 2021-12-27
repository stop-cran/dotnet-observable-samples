using System;
using System.Drawing;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Reactive.Samples
{
    public class SubjectTests
    {
        [Test]
        public void ShouldTakeLast()
        {
            var subject = new ReplaySubject<Color>();
            subject.OnNext(ColorHelper.GetRandomColor());
            subject.OnNext(ColorHelper.GetRandomColor());

            using var subscription = subject.Subscribe(item =>
                Console.WriteLine($"Observed item {item}"));
            
            subject.OnNext(ColorHelper.GetRandomColor());

            Console.WriteLine("Completed!");
            subject.OnCompleted();
        }

        [Test]
        public void ShouldTakeLastAwait()
        {
            var subject = new Subject<Color>();
            subject.OnNext(ColorHelper.GetRandomColor());
            subject.OnNext(ColorHelper.GetRandomColor());

            using var subscription = subject.Subscribe(item =>
                Console.WriteLine($"Observed item {item}"));
            
            subject.OnNext(ColorHelper.GetRandomColor());

            Console.WriteLine("Completed!");
            subject.OnCompleted();
        }


        [Test]
        public async Task ShouldReplay()
        {
            var observable = Observable.Timer(TimeSpan.FromMilliseconds(30), TimeSpan.FromMilliseconds(30))
                .Select(_ => ColorHelper.GetRandomColor())
                .Replay();
            using var publishSubscription = observable.Connect();

            await Task.Delay(100);
                
            using var subscription = observable.Subscribe(item =>
                Console.WriteLine($"Observed item {item}"));

            Console.WriteLine($"Waiting...");
            await Task.Delay(100);
        }
    }
}