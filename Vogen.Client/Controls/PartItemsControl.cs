using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Vogen.Client.Controls
{
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(PartItem))]
    internal class PartItemsControl : ItemsControl
    {
        protected override bool IsItemItsOwnContainerOverride(object item) => item is PartItem;
        protected override DependencyObject GetContainerForItemOverride() => new PartItem();
    }
}
