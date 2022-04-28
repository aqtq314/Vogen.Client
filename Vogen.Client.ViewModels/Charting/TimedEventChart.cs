using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vogen.Client.ViewModels.Utils;

namespace Vogen.Client.ViewModels.Charting
{
    public class TimedEventChart<TItem, TTime> : CollectionViewModelBase<TItem>
        where TItem : TimedEventItem<TTime>
        where TTime : struct, IComparable<TTime>
    {
        bool hasPendingItemsChanges;

        ImmutableArray<TTime> _ErrorList;

        public ImmutableArray<TTime> ErrorList
        {
            get => _ErrorList;
            set => SetAndNotify(ref _ErrorList, value);
        }

        public NonEquatable<TimedEventChart<TItem, TTime>> ItemsBindingHook => new(this);
        public event EventHandler? ItemsChanged;

        public TimedEventChart(IEnumerable<TItem>? items = null)
            : base(items ?? Enumerable.Empty<TItem>())
        {
            _ErrorList = ImmutableArray<TTime>.Empty;

            foreach (var item in this)
                OnItemAdded(item);

            OnItemsChanged();
        }

        protected virtual void OnItemRemoving(TItem item)
        {
            item.PropertyChanged -= OnChildPropertyChanged;
        }

        protected virtual void OnItemAdded(TItem item)
        {
            item.PropertyChanged += OnChildPropertyChanged;
        }

        protected override void ClearItems()
        {
            foreach (var item in this)
                OnItemRemoving(item);

            base.ClearItems();
        }

        protected override void InsertItem(int index, TItem item)
        {
            base.InsertItem(index, item);
            OnItemAdded(item);
        }

        protected override void RemoveItem(int index)
        {
            OnItemRemoving(this[index]);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, TItem item)
        {
            OnItemRemoving(this[index]);
            base.SetItem(index, item);
            OnItemAdded(item);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!hasPendingItemsChanges)
                throw new InvalidOperationException($@"{GetType()}: Collection's {nameof(ItemsChangingNotifier)} not open");

            base.OnCollectionChanged(e);
        }

        private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!hasPendingItemsChanges)
                throw new InvalidOperationException($@"{GetType()}: Collection's {nameof(ItemsChangingNotifier)} not open");

            OnChildPropertyChanged((TItem)(sender ?? throw new ArgumentNullException(nameof(sender))), e);
        }

        protected virtual void OnChildPropertyChanged(TItem child, PropertyChangedEventArgs e)
        {
        }

        protected virtual void OnItemsChanged()
        {
            SortByTime();
            UpdateChartStates();

            ItemsChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(nameof(ItemsBindingHook));
        }

        public IDisposable ItemsChangingNotifier()
        {
            if (hasPendingItemsChanges)
                throw new InvalidOperationException($"{nameof(ItemsChangingNotifier)}: Reentrancy not allowed");

            hasPendingItemsChanges = true;

            return Disposable.CreateOnce(() =>
            {
                try { OnItemsChanged(); }
                finally { hasPendingItemsChanges = false; }
            });
        }

        protected virtual bool SortByTime()
        {
            var orderChanged = false;

            for (int i = 1; i < Count; i++)
            {
                int j = i - 1;
                for (; j >= 0 && this[j].Time.CompareTo(this[i].Time) > 0; j--) ;
                if (i != j + 1)
                {
                    Move(i, j + 1);
                    orderChanged = true;
                }
            }

            return orderChanged;
        }

        protected virtual IEnumerable<TTime> CheckErrors()
        {
            // check for simultaneous events
            for (int i = 0; i < Count - 1; i++)
                if (this[i].Time.CompareTo(this[i + 1].Time) == 0)
                    yield return this[i].Time;
        }

        protected virtual void UpdateChartStates()
        {
            ErrorList = ImmutableArray.CreateRange(CheckErrors());
        }

        public int FindInsertIndexByTime(TTime time)
        {
            int FindInsertIndex(int minIndex, int maxIndex)
            {
                if (maxIndex - minIndex <= 1)
                    return maxIndex;

                var midIndex = (minIndex + maxIndex) / 2;
                if (time.CompareTo(this[midIndex].Time) < 0)
                    return FindInsertIndex(minIndex, midIndex);
                else
                    return FindInsertIndex(midIndex, maxIndex);
            }

            return FindInsertIndex(-1, Count);
        }
    }
}
