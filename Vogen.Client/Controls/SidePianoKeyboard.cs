using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Vogen.Client.Converters;
using Vogen.Client.Utils;

namespace Vogen.Client.Controls
{
    public class SidePianoKeyboard : FrameworkElement
    {
        static Brush? whiteKeyFill = Brushes.White;
        static Pen? whiteKeyPen = new Pen(Brushes.Black, 0.6).Frozen();
        static Brush? blackKeyFill = Brushes.Black;
        static Pen? blackKeyPen = null;

        static double defaultKeyHeight = 12;
        static double[] keyOffsetLookup = new double[] { -8, 0, -4, 0, 0, -9, 0, -6, 0, -3, 0, 0 };
        static double[] keyHeightLookup = new double[] { 20, 12, 20, 12, 20, 21, 12, 21, 12, 21, 12, 21 };

        public double BlackKeyLengthRatio
        {
            get => (double)GetValue(BlackKeyLengthRatioProperty);
            set => SetValue(BlackKeyLengthRatioProperty, value);
        }
        public static DependencyProperty BlackKeyLengthRatioProperty { get; } =
            DependencyProperty.Register(nameof(BlackKeyLengthRatio), typeof(double), typeof(SidePianoKeyboard),
                new FrameworkPropertyMetadata(0.6, FrameworkPropertyMetadataOptions.AffectsRender));

        protected override void OnRender(DrawingContext dc)
        {
            var actualWidth = ActualWidth;
            var actualHeight = ActualHeight;
            var keyHeight = NoteChartEditor.GetKeyHeight(this);
            var minKey = NoteChartEditor.GetMinKey(this);
            var maxKey = NoteChartEditor.GetMaxKey(this);
            var vOffset = NoteChartEditor.GetVOffset(this);

            var whiteKeyWidth = actualWidth;
            var blackKeyWidth = (whiteKeyWidth * BlackKeyLengthRatio).Clamp(0.0, whiteKeyWidth);
            var cornerRadius = Math.Min(2.0, Math.Min(keyHeight / 2, blackKeyWidth / 2));

            var botPitch = (int)Math.Max(minKey, ChartUnitConversion.PixelToPitch(keyHeight, actualHeight, vOffset, actualHeight));
            var topPitch = (int)Math.Min(maxKey, ChartUnitConversion.PixelToPitch(keyHeight, actualHeight, vOffset, 0.0)).Ceil();

            using var _ = dc.UsingClip(new RectangleGeometry(new Rect(new Size(actualWidth, actualHeight))));

            // background
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(new Size(actualWidth, actualHeight)));

            // white keys
            for (var pitch = botPitch; pitch <= topPitch; pitch++)
                if (!Midi.isBlackKey(pitch))
                {
                    var keyOffset = keyOffsetLookup[pitch % 12] / defaultKeyHeight;
                    var y = ChartUnitConversion.PitchToPixel(keyHeight, actualHeight, vOffset, pitch + 0.5 - keyOffset);
                    var height = keyHeightLookup[pitch % 12] / defaultKeyHeight * keyHeight;
                    var x = whiteKeyPen is null ? 0.0 : whiteKeyPen.Thickness / 2;
                    var width = Math.Max(0, whiteKeyWidth - x * 2);
                    dc.DrawRoundedRectangle(whiteKeyFill, whiteKeyPen, new Rect(x, y, width, height), cornerRadius, cornerRadius);
                }

            // black keys
            for (var pitch = botPitch; pitch <= topPitch; pitch++)
                if (Midi.isBlackKey(pitch))
                {
                    var y = ChartUnitConversion.PitchToPixel(keyHeight, actualHeight, vOffset, pitch + 0.5);
                    var height = keyHeight;
                    var x = blackKeyPen is null ? 0.0 : blackKeyPen.Thickness / 2;
                    var width = Math.Max(0, blackKeyWidth - x * 2);
                    dc.DrawRoundedRectangle(blackKeyFill, blackKeyPen, new Rect(0.0, y, width, height), cornerRadius, cornerRadius);
                }

            // text labels
            for (var pitch = botPitch; pitch <= topPitch; pitch++)
                if (pitch % 12 == 0)
                {
                    var ft = this.MakeFormattedText($"C{pitch / 12 - 1}");
                    var x = whiteKeyWidth - 2.0 - ft.Width;
                    var y = ChartUnitConversion.PitchToPixel(keyHeight, actualHeight, vOffset, pitch + 0.5) + (keyHeight - ft.Height) / 2;
                    dc.DrawText(ft, new Point(x, y));
                }
        }
    }
}
