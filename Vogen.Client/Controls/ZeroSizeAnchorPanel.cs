using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Vogen.Client.Controls
{
    public class ZeroSizeAnchorPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            var childAvailableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
            foreach (UIElement child in InternalChildren)
                child.Measure(childAvailableSize);

            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement child in InternalChildren)
            {
                var childDesiredSize = child.DesiredSize;

                if (child is FrameworkElement feChild)
                {
                    var childX = feChild.HorizontalAlignment switch
                    {
                        HorizontalAlignment.Center => -childDesiredSize.Width / 2,
                        HorizontalAlignment.Right => -childDesiredSize.Width,
                        _ => 0
                    };
                    var childY = feChild.VerticalAlignment switch
                    {
                        VerticalAlignment.Center => -childDesiredSize.Height / 2,
                        VerticalAlignment.Bottom => -childDesiredSize.Height,
                        _ => 0
                    };
                    child.Arrange(new Rect(new Point(childX, childY), childDesiredSize));
                }
                else
                {
                    child.Arrange(new Rect(childDesiredSize));
                }
            }

            return finalSize;
        }
    }
}
