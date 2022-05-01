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
}
