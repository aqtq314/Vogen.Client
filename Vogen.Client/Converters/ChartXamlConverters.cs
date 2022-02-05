using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Vogen.Client.Utils;

namespace Vogen.Client.Converters
{
    public static class ChartXamlConverters
    {
        public static IMultiValueConverter HScrollSpan { get; } = ValueConverter.CreateMulti((object[] vs, object p) => // unit in midi pulses
        {
            if (vs.Length != 2) throw new ArgumentException($"{nameof(ChartXamlConverters)}.{nameof(HScrollSpan)}");
            var log2QuarterWidth = Convert.ToDouble(vs[0]);
            var chartWidth = Convert.ToDouble(vs[1]);
            var scale = p is null ? 1 : Convert.ToDouble(p);
            return scale * ChartUnitConversion.PixelToPulse(Math.Pow(2, log2QuarterWidth), 0, chartWidth);
        });

        public static IMultiValueConverter VScrollSpan { get; } = ValueConverter.CreateMulti((object[] vs, object p) => // unit in key indices
        {
            if (vs.Length != 2) throw new ArgumentException($"{nameof(ChartXamlConverters)}.{nameof(VScrollSpan)}");
            var log2KeyHeight = Convert.ToDouble(vs[0]);
            var chartHeight = Convert.ToDouble(vs[1]);
            var scale = p is null ? 1 : Convert.ToDouble(p);
            return scale * ChartUnitConversion.PixelToPitch(Math.Pow(2.0, log2KeyHeight), 0, 0, -chartHeight);
        });

        public static IValueConverter Negative { get; } = ValueConverter.Create(
            (double value) => -value,
            (double value) => -value);

        public static IValueConverter Log2 { get; } = ValueConverter.Create(
            (double linearValue, object offset) => Math.Log(linearValue) / MathExtension.Log2 + Convert.ToDouble(offset ?? 0.0),
            (double log2Value, object offset) => Math.Pow(2, log2Value - Convert.ToDouble(offset ?? 0.0)));

        public static IValueConverter Exp2 { get; } = ValueConverter.Create(
            (double log2Value, object offset) => Math.Pow(2, log2Value + Convert.ToDouble(offset ?? 0.0)),
            (double linearValue, object offset) => Math.Log(linearValue) / MathExtension.Log2 - Convert.ToDouble(offset ?? 0.0));
    }
}
