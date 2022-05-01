using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class TrackList : CollectionViewModelBase<Track>
    {
        protected override void ClearItems()
        {
            foreach (var item in Items)
                item.Index = -1;

            base.ClearItems();
        }

        protected override void InsertItem(int index, Track item)
        {
            base.InsertItem(index, item);

            for (var i = index; i < Count; i++)
                Items[i].Index = i;
        }

        protected override void RemoveItem(int index)
        {
            Items[index].Index = -1;
            base.RemoveItem(index);

            for (var i = index; i < Count; i++)
                Items[i].Index = i;
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            base.MoveItem(oldIndex, newIndex);

            var lowerIndex = Math.Min(oldIndex, newIndex);
            var upperIndex = Math.Max(oldIndex, newIndex);
            for (var i = lowerIndex; i <= upperIndex; i++)
                Items[i].Index = i;
        }

        public TrackList(IEnumerable<Track>? tracks = null)
            : base(tracks ?? Enumerable.Empty<Track>())
        {
            for (var i = 0; i < Count; i++)
                Items[i].Index = i;
        }
    }
}
