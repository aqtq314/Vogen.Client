using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Vogen.Client.Converters;
using Vogen.Client.ViewModels.Charting;

namespace Vogen.Client.Controls
{
    public class TimeSigEventPanel : Panel
    {
        readonly Dictionary<MeasureEventItem, double> measuredChildren = new();

        public TimeSignatureChart TimeSignatureChart
        {
            get => (TimeSignatureChart)GetValue(TimeSignatureChartProperty);
            set => SetValue(TimeSignatureChartProperty, value);
        }

        public static DependencyProperty TimeSignatureChartProperty { get; } =
            DependencyProperty.Register(nameof(TimeSignatureChart), typeof(TimeSignatureChart), typeof(TimeSigEventPanel),
                new FrameworkPropertyMetadata(new TimeSignatureChart(new TimeSignature(4, 4))));

        protected override Size MeasureOverride(Size availableSize)
        {
            if (double.IsInfinity(availableSize.Width))
                availableSize.Width = 0;

            var quarterWidth = MidiCharting.GetQuarterWidth(this);
            var hOffset = MidiCharting.GetHOffset(this);
            var timeSignatureChart = TimeSignatureChart;

            var minTime = MidiClock.FloorFrom(ChartUnitConversion.PixelToMidiClock(quarterWidth, hOffset, 0));
            var maxTime = MidiClock.CeilFrom(ChartUnitConversion.PixelToMidiClock(quarterWidth, hOffset, availableSize.Width));

            double maxDesiredHeight = 0;
            measuredChildren.Clear();

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var child = (MeasureEventItem)InternalChildren[i];
                var childTime = timeSignatureChart.TimeCodeToMidiTime(child.MeasureIndex, 0, 0);
                if (childTime < minTime) continue;
                if (childTime > maxTime) continue;

                var x0 = ChartUnitConversion.MidiClockToPixel(quarterWidth, hOffset, childTime);

                var childMeasureSize = new Size(double.PositiveInfinity, availableSize.Height);
                child.Measure(childMeasureSize);
                maxDesiredHeight = Math.Max(maxDesiredHeight, child.DesiredSize.Height);

                measuredChildren.Add(child, x0);
            }

            foreach (MeasureEventItem child in InternalChildren)
                child.Visibility = measuredChildren.ContainsKey(child) ? Visibility.Visible : Visibility.Collapsed;

            return new Size(availableSize.Width, maxDesiredHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (MeasureEventItem child in InternalChildren)
                if (measuredChildren.TryGetValue(child, out var x0))
                    child.Arrange(new Rect(x0, 0, child.DesiredSize.Width, finalSize.Height));

            return finalSize;
        }
    }
}
