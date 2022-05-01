using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Vogen.Client.Controls
{
    public class ChartScrollZoomHV01 : ChartScrollZoomKitBase
    {
        static ChartScrollZoomHV01()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ChartScrollZoomHV01), new FrameworkPropertyMetadata(typeof(ChartScrollZoomHV01)));
        }
    }
}
