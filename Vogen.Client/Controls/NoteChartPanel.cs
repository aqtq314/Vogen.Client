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
    public class NoteChartPanel : Panel
    {
        Dictionary<NoteItem, Rect> measuredChildren = new Dictionary<NoteItem, Rect>();

        protected override Size MeasureOverride(Size availableSize)
        {
            if (double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height))
                throw new ArgumentException($"{nameof(NoteChartPanel)} measure availableSize ({availableSize}) cannot contain infinity");

            var actualWidth = ActualWidth;
            var actualHeight = ActualHeight;
            var quarterWidth = NoteChartEditor.GetQuarterWidth(this);
            var keyHeight = NoteChartEditor.GetKeyHeight(this);
            var minKey = NoteChartEditor.GetMinKey(this);
            var maxKey = NoteChartEditor.GetMaxKey(this);
            var hOffset = NoteChartEditor.GetHOffset(this);
            var vOffset = NoteChartEditor.GetVOffset(this);

            var minPulse = (long)ChartUnitConversion.PixelToPulse(quarterWidth, hOffset, 0);
            var maxPulse = (long)ChartUnitConversion.PixelToPulse(quarterWidth, hOffset, actualWidth).Ceil();
            var botPitch = Math.Max(minKey, (int)ChartUnitConversion.PixelToPitch(keyHeight, actualHeight, vOffset, actualHeight));
            var topPitch = Math.Min(maxKey, (int)ChartUnitConversion.PixelToPitch(keyHeight, actualHeight, vOffset, 0).Ceil());

            measuredChildren.Clear();

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var note = (NoteItem)InternalChildren[i];
                var noteOff = i + 1 < InternalChildren.Count ? ((NoteItem)InternalChildren[i + 1]).Onset : note.Onset;
                if (noteOff < minPulse) continue;
                if (note.Onset > maxPulse) continue;

                var prevPitch = i == 0 ? Note.RestPitch : ((NoteItem)InternalChildren[i - 1]).Pitch;
                var arrangePrevPitch = prevPitch != Note.RestPitch ? prevPitch : note.Pitch;
                var arrangeCurrPitch = note.Pitch != Note.RestPitch ? note.Pitch : prevPitch;
                if (arrangeCurrPitch == Note.RestPitch) continue;
                if (Math.Max(arrangePrevPitch, arrangeCurrPitch) < botPitch) continue;
                if (Math.Min(arrangePrevPitch, arrangeCurrPitch) > topPitch) continue;

                note.InternalDeltaPitch = arrangeCurrPitch - arrangePrevPitch;

                var x0 = ChartUnitConversion.PulseToPixel(quarterWidth, hOffset, note.Onset);
                var x1 = ChartUnitConversion.PulseToPixel(quarterWidth, hOffset, noteOff);
                var yMid = ChartUnitConversion.PitchToPixel(keyHeight, actualHeight, vOffset, note.Pitch);

                var noteRect = new Rect(x0, yMid - keyHeight / 2, x1 - x0, keyHeight);
                note.Measure(noteRect.Size);
                measuredChildren.Add(note, noteRect);
            }

            foreach (NoteItem note in InternalChildren)
                note.Visibility = measuredChildren.ContainsKey(note) ? Visibility.Visible : Visibility.Collapsed;

            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (NoteItem note in InternalChildren)
                if (measuredChildren.TryGetValue(note, out var noteRect))
                    note.Arrange(noteRect);

            return finalSize;
        }

        public NoteChartPanel()
        {
        }
    }
}
