using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Vogen.Client.Converters;

namespace Vogen.Client.Controls
{
    public class TempoEventPanel : Panel
    {
        readonly Dictionary<MidiEventItem, double> measuredChildren = new();

        protected override Size MeasureOverride(Size availableSize)
        {
            if (double.IsInfinity(availableSize.Width))
                availableSize.Width = 0;

            var quarterWidth = MidiCharting.GetQuarterWidth(this);
            var hOffset = MidiCharting.GetHOffset(this);

            var minTime = MidiClock.FloorFrom(ChartUnitConversion.PixelToMidiClock(quarterWidth, hOffset, 0));
            var maxTime = MidiClock.CeilFrom(ChartUnitConversion.PixelToMidiClock(quarterWidth, hOffset, availableSize.Width));

            double maxDesiredHeight = 0;
            measuredChildren.Clear();

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var child = (MidiEventItem)InternalChildren[i];
                if (child.Onset < minTime) continue;
                if (child.Onset > maxTime) continue;

                var x0 = ChartUnitConversion.MidiClockToPixel(quarterWidth, hOffset, child.Onset);

                var childMeasureSize = new Size(double.PositiveInfinity, availableSize.Height);
                child.Measure(childMeasureSize);
                maxDesiredHeight = Math.Max(maxDesiredHeight, child.DesiredSize.Height);

                measuredChildren.Add(child, x0);
            }

            foreach (MidiEventItem child in InternalChildren)
                child.Visibility = measuredChildren.ContainsKey(child) ? Visibility.Visible : Visibility.Collapsed;

            return new Size(availableSize.Width, maxDesiredHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            // Might cause child visibility issues if finalSize != availableSize
            foreach (MidiEventItem child in InternalChildren)
                if (measuredChildren.TryGetValue(child, out var x0))
                    child.Arrange(new Rect(x0, 0, child.DesiredSize.Width, finalSize.Height));

            return finalSize;
        }
    }
}
