using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Vogen.Client.Controls
{
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(EventItem))]
    public class EventChartEditor : ItemsControl
    {
        protected override bool IsItemItsOwnContainerOverride(object item) => item is EventItem;
        protected override DependencyObject GetContainerForItemOverride() => new EventItem();
    }
}
