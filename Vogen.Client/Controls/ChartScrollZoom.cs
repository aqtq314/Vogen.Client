using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Vogen.Client.Controls
{
    public class ChartScrollZoomH2 : ChartScrollZoomBase
    {
        static ChartScrollZoomH2()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ChartScrollZoomH2), new FrameworkPropertyMetadata(typeof(ChartScrollZoomH2)));
        }
    }

    public class ChartScrollZoomV2 : ChartScrollZoomBase
    {
        static ChartScrollZoomV2()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ChartScrollZoomV2), new FrameworkPropertyMetadata(typeof(ChartScrollZoomV2)));
        }
    }
}
