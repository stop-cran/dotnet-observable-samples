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
    public static class ColorHelper
    {
        private static IReadOnlyList<Color> _colors = typeof(Color)
            .GetProperties(BindingFlags.Static | BindingFlags.Public)
            .Select(p => (Color)p.GetMethod!.Invoke(null, Array.Empty<object>())!).ToList();

        private static Random _random = new Random();

        public static Color GetRandomColor()
        {
            var randomColor = _colors[_random.Next(_colors.Count)];

            Console.WriteLine("Generated item: " + randomColor);

            return randomColor;
        }
    }
}