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
    public class EventChartPanel : Panel
    {
        Dictionary<EventItem, (double x, double width)> measuredChildren = new Dictionary<EventItem, (double, double)>();

        protected override Size MeasureOverride(Size availableSize)
        {
            if (double.IsInfinity(availableSize.Width))
                throw new ArgumentException($"Unable to handle measure availableSize: {availableSize}");

            var actualWidth = ActualWidth;
            var actualHeight = ActualHeight;
            var quarterWidth = NoteChartEditor.GetQuarterWidth(this);
            var hOffset = NoteChartEditor.GetHOffset(this);

            var minPulse = (long)ChartUnitConversion.PixelToPulse(quarterWidth, hOffset, 0);
            var maxPulse = (long)ChartUnitConversion.PixelToPulse(quarterWidth, hOffset, actualWidth).Ceil();

            double maxDesiredHeight = 0;
            measuredChildren.Clear();

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var child = (EventItem)InternalChildren[i];
                var childOff = i + 1 < InternalChildren.Count ? ((EventItem)InternalChildren[i + 1]).Onset : child.Onset;
                if (childOff < minPulse) continue;
                if (child.Onset > maxPulse) continue;

                var x0 = ChartUnitConversion.PulseToPixel(quarterWidth, hOffset, child.Onset);
                var x1 = ChartUnitConversion.PulseToPixel(quarterWidth, hOffset, childOff);

                var childMeasureSize = new Size(x1 - x0, availableSize.Height);
                child.Measure(childMeasureSize);
                maxDesiredHeight = Math.Max(maxDesiredHeight, child.DesiredSize.Height);

                measuredChildren.Add(child, (x0, x1 - x0));
            }

            foreach (EventItem child in InternalChildren)
                child.Visibility = measuredChildren.ContainsKey(child) ? Visibility.Visible : Visibility.Collapsed;

            return new Size(availableSize.Width, maxDesiredHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            // Might cause child visibility issues if finalSize != availableSize
            foreach (EventItem child in InternalChildren)
                if (measuredChildren.TryGetValue(child, out var xs))
                {
                    var (x, width) = xs;
                    child.Arrange(new Rect(x, 0, width, finalSize.Height));
                }

            return finalSize;
        }
    }
}
