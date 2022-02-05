using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.Utils
{
    public static class MathExtension
    {
        public static double Lerp(this double amount, double a, double b) => amount * (b - a) + a;
        public static double Unlerp(this double value, double a, double b) => (value - a) / (b - a);

        public static double Floor(this double value) => Math.Floor(value);
        public static double Ceil(this double value) => Math.Ceiling(value);

        public static int CeilDiv(this int dividend, int divisor)
        {
            var ratio = dividend / divisor;
            return (dividend ^ divisor) >= 0 && (dividend % divisor) != 0 ? ratio + 1 : ratio;
        }
        public static long CeilDiv(this long dividend, long divisor)
        {
            var ratio = dividend / divisor;
            return (dividend ^ divisor) >= 0 && (dividend % divisor) != 0 ? ratio + 1 : ratio;
        }

        public static double Clamp(this double value, double minValue, double maxValue) => Math.Max(minValue, Math.Min(maxValue, value));
        public static int Clamp(this int value, int minValue, int maxValue) => Math.Max(minValue, Math.Min(maxValue, value));
        public static long Clamp(this long value, long minValue, long maxValue) => Math.Max(minValue, Math.Min(maxValue, value));

        public static bool IsBetween(this double value, double minValue, double maxValue) => value >= minValue && value < maxValue;
        public static bool IsBetween(this int value, int minValue, int maxValue) => value >= minValue && value < maxValue;
        public static bool IsBetween(this long value, long minValue, long maxValue) => value >= minValue && value < maxValue;

        public static bool IsBetweenInc(this double value, double minValue, double maxValue) => value >= minValue && value <= maxValue;
        public static bool IsBetweenInc(this int value, int minValue, int maxValue) => value >= minValue && value <= maxValue;
        public static bool IsBetweenInc(this long value, long minValue, long maxValue) => value >= minValue && value <= maxValue;
    }
}
