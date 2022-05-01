using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Vogen.Client.Converters;
using Vogen.Client.Utils;
using Vogen.Client.ViewModels;
using Vogen.Client.ViewModels.Charting;
using Vogen.Client.ViewModels.Utils;

namespace Vogen.Client.Controls
{
    public class PartPanel : Panel
    {
        Dictionary<PartItem, double> measuredChildren = new();

        public NonEquatable<TimedEventChart<TimedValueItem<MidiClock, double>, MidiClock>>? TempoChart
        {
            get => (NonEquatable<TimedEventChart<TimedValueItem<MidiClock, double>, MidiClock>>?)GetValue(TempoChartProperty);
            set => SetValue(TempoChartProperty, value);
        }

        public double TrackHeight
        {
            get => (double)GetValue(TrackHeightProperty);
            set => SetValue(TrackHeightProperty, value);
        }

        public static DependencyProperty TempoChartProperty { get; } =
            DependencyProperty.Register(nameof(TempoChart), typeof(NonEquatable<TimedEventChart<TimedValueItem<MidiClock, double>, MidiClock>>?), typeof(PartPanel),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public static DependencyProperty TrackHeightProperty { get; } =
            DependencyProperty.Register(nameof(TrackHeight), typeof(double), typeof(PartPanel),
                new FrameworkPropertyMetadata(20.0));

        protected override Size MeasureOverride(Size availableSize)
        {
            if (double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height))
                throw new ArgumentException($"Unable to handle measure availableSize: {availableSize}");

            var quarterWidth = MidiCharting.GetQuarterWidth(this);
            var trackHeight = TrackHeight;
            var hOffset = MidiCharting.GetHOffset(this);

            var minPulse = MidiClock.FloorFrom(ChartUnitConversion.PixelToMidiClock(quarterWidth, hOffset, 0));
            var maxPulse = MidiClock.CeilFrom(ChartUnitConversion.PixelToMidiClock(quarterWidth, hOffset, availableSize.Width));

            measuredChildren.Clear();

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var child = (PartItem)InternalChildren[i];
                if (child.Onset > maxPulse) continue;

                var x0 = ChartUnitConversion.MidiClockToPixel(quarterWidth, hOffset, child.Onset);

                var childMeasureSize = new Size(double.PositiveInfinity, trackHeight);
                child.Measure(childMeasureSize);    // child should return real desired width assuming infinite screen width
                if (x0 + child.DesiredSize.Width < 0) continue;

                measuredChildren.Add(child, x0);
            }

            foreach (PartItem child in InternalChildren)
                child.Visibility = measuredChildren.ContainsKey(child) ? Visibility.Visible : Visibility.Collapsed;

            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var trackHeight = TrackHeight;

            // Might cause child visibility issues if finalSize != availableSize
            foreach (PartItem child in InternalChildren)
            {
                if (measuredChildren.TryGetValue(child, out var x0))
                {
                    var childTrackIndex = child.TrackIndex;
                    var x1 = x0 + child.DesiredSize.Width;

                    child.HasLeftOverflow = x0 < 0;
                    child.HasRightOverflow = x1 >= finalSize.Width;

                    x0 = Math.Max(x0, 0);
                    x1 = Math.Max(Math.Min(x1, finalSize.Width), x0);
                    child.Arrange(new Rect(x0, childTrackIndex * trackHeight, x1 - x0, trackHeight));
                }
            }

            return finalSize;
        }
    }
}
