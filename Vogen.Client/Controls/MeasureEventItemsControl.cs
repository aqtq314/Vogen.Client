using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Vogen.Client.Controls
{
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(MeasureEventItem))]
    public class MeasureEventItemsControl : ItemsControl
    {
        protected override bool IsItemItsOwnContainerOverride(object item) => item is MeasureEventItem;
        protected override DependencyObject GetContainerForItemOverride() => new MeasureEventItem();
    }
}
