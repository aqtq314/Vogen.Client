using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Vogen.Client.Converters;
using Vogen.Client.Utils;

namespace Vogen.Client.Controls
{
    public class MidiEventPanel : Panel
    {
        Dictionary<MidiEventItem, XRange> measuredChildren = new();

        protected override Size MeasureOverride(Size availableSize)
        {
            if (double.IsInfinity(availableSize.Width))
                throw new ArgumentException($"Unable to handle measure availableSize: {availableSize}");

            var quarterWidth = MidiCharting.GetQuarterWidth(this);
            var hOffset = MidiCharting.GetHOffset(this);

            var minPulse = MidiClock.FloorFrom(ChartUnitConversion.PixelToMidiClock(quarterWidth, hOffset, 0));
            var maxPulse = MidiClock.CeilFrom(ChartUnitConversion.PixelToMidiClock(quarterWidth, hOffset, availableSize.Width));

            double maxDesiredHeight = 0;
            measuredChildren.Clear();

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var child = (MidiEventItem)InternalChildren[i];
                var childOff = i + 1 < InternalChildren.Count ? ((MidiEventItem)InternalChildren[i + 1]).Onset : child.Onset;
                if (childOff < minPulse) continue;
                if (child.Onset > maxPulse) continue;

                var x0 = ChartUnitConversion.MidiClockToPixel(quarterWidth, hOffset, child.Onset);
                var x1 = ChartUnitConversion.MidiClockToPixel(quarterWidth, hOffset, childOff);

                var childMeasureSize = new Size(x1 - x0, availableSize.Height);
                child.Measure(childMeasureSize);
                maxDesiredHeight = Math.Max(maxDesiredHeight, child.DesiredSize.Height);

                measuredChildren.Add(child, new XRange(x0, x1));
            }

            foreach (MidiEventItem child in InternalChildren)
                child.Visibility = measuredChildren.ContainsKey(child) ? Visibility.Visible : Visibility.Collapsed;

            return new Size(availableSize.Width, maxDesiredHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            // Might cause child visibility issues if finalSize != availableSize
            foreach (MidiEventItem child in InternalChildren)
            {
                if (measuredChildren.TryGetValue(child, out var xRange))
                {
                    child.HasLeftOverflow = xRange.X0 < 0;
                    child.HasRightOverflow = xRange.X1 >= finalSize.Width;

                    var x0 = Math.Max(xRange.X0, 0);
                    var x1 = Math.Max(Math.Min(xRange.X1, finalSize.Width), x0);
                    child.Arrange(new Rect(x0, 0, x1 - x0, finalSize.Height));
                }
            }

            return finalSize;
        }
    }
}
