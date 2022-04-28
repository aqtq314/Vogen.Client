using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class TimedEventItem<TTime> : ViewModelBase, IComparable, IComparable<TimedEventItem<TTime>>
        where TTime : struct, IComparable<TTime>
    {
        TTime _Time;

        public TTime Time
        {
            get => _Time;
            set => SetAndNotify(ref _Time, value);
        }

        public TimedEventItem(TTime time)
        {
            _Time = time;
        }

        public int CompareTo(TimedEventItem<TTime>? other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            return _Time.CompareTo(other._Time);
        }

        public int CompareTo(object? obj)
        {
            if (obj is not TimedEventItem<TTime>)
                throw new ArgumentException(null, nameof(obj));

            return CompareTo(obj as TimedEventItem<TTime>);
        }
    }
}
