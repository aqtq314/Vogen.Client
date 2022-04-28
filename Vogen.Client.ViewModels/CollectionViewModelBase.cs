using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels
{
    public class CollectionViewModelBase<TItem> : ObservableCollection<TItem>
    {
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

        protected bool SetAndNotify<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
                return false;

            field = newValue;
            OnPropertyChanged(propertyName);
            return true;
        }

        public CollectionViewModelBase(IEnumerable<TItem>? items = null) : base(items ?? Enumerable.Empty<TItem>())
        {
        }
    }
}
