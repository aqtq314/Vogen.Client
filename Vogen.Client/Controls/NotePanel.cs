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

namespace Vogen.Client.Controls
{
    public class NotePanel : Panel
    {
        Dictionary<NoteItem, Rect> measuredChildren = new();

        protected override Size MeasureOverride(Size availableSize)
        {
            if (double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height))
                throw new ArgumentException($"Unable to handle measure availableSize: {availableSize}");

            var actualWidth = ActualWidth;
            var actualHeight = ActualHeight;
            var quarterWidth = MidiCharting.GetQuarterWidth(this);
            var keyHeight = MidiCharting.GetKeyHeight(this);
            var minKey = MidiCharting.GetMinKey(this);
            var maxKey = MidiCharting.GetMaxKey(this);
            var hOffset = MidiCharting.GetHOffset(this);
            var vOffset = MidiCharting.GetVOffset(this);

            var minPulse = MidiClock.FloorFrom(ChartUnitConversion.PixelToMidiClock(quarterWidth, hOffset, 0));
            var maxPulse = MidiClock.CeilFrom(ChartUnitConversion.PixelToMidiClock(quarterWidth, hOffset, actualWidth));
            var botPitch = Math.Max(minKey, (int)ChartUnitConversion.PixelToPitch(keyHeight, actualHeight, vOffset, actualHeight));
            var topPitch = Math.Min(maxKey, (int)ChartUnitConversion.PixelToPitch(keyHeight, actualHeight, vOffset, 0).Ceil());

            measuredChildren.Clear();

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var child = (NoteItem)InternalChildren[i];
                var childOff = i + 1 < InternalChildren.Count ? ((NoteItem)InternalChildren[i + 1]).Onset : child.Onset;
                if (childOff < minPulse) continue;
                if (child.Onset > maxPulse) continue;

                var prevPitch = i == 0 ? 0 : ((NoteItem)InternalChildren[i - 1]).Pitch;
                var arrangePrevPitch = prevPitch != 0 ? prevPitch : child.Pitch;
                var arrangeCurrPitch = child.Pitch != 0 ? child.Pitch : prevPitch;
                if (arrangeCurrPitch == 0) continue;
                if (Math.Max(arrangePrevPitch, arrangeCurrPitch) < botPitch) continue;
                if (Math.Min(arrangePrevPitch, arrangeCurrPitch) > topPitch) continue;

                child.InternalDeltaPitch = arrangeCurrPitch - arrangePrevPitch;

                var x0 = ChartUnitConversion.MidiClockToPixel(quarterWidth, hOffset, child.Onset);
                var x1 = ChartUnitConversion.MidiClockToPixel(quarterWidth, hOffset, childOff);
                var yMid = ChartUnitConversion.PitchToPixel(keyHeight, actualHeight, vOffset, child.Pitch);

                var childRect = new Rect(x0, yMid - keyHeight / 2, x1 - x0, keyHeight);
                child.Measure(childRect.Size);
                measuredChildren.Add(child, childRect);
            }

            foreach (NoteItem note in InternalChildren)
                note.Visibility = measuredChildren.ContainsKey(note) ? Visibility.Visible : Visibility.Collapsed;

            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            // Might cause child visibility issues if finalSize != availableSize
            foreach (NoteItem child in InternalChildren)
            {
                if (measuredChildren.TryGetValue(child, out var childRect))
                {
                    child.HasLeftOverflow = childRect.Left < 0;
                    child.HasRightOverflow = childRect.Right >= finalSize.Width;

                    var x0 = Math.Max(childRect.Left, 0);
                    var x1 = Math.Max(Math.Min(childRect.Right, finalSize.Width), x0);
                    child.Arrange(new Rect(x0, childRect.Y, x1 - x0, childRect.Height));
                }
            }

            return finalSize;
        }

        public NotePanel()
        {
        }
    }
}
