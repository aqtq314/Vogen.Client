using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public static class TimedValueItem
    {
        public static TimedValueItem<TTime, TValue> Create<TTime, TValue>(TTime time, TValue value)
            where TTime : struct, IComparable<TTime> =>
            new TimedValueItem<TTime, TValue>(time, value);
    }

    public class TimedValueItem<TTime, TValue> : TimedEventItem<TTime>
        where TTime : struct, IComparable<TTime>
    {
        TValue _Value;

        public TValue Value
        {
            get => _Value;
            set => SetAndNotify(ref _Value, value);
        }

        public TimedValueItem(TTime time, TValue value) : base(time)
        {
            _Value = value;
        }
    }
}
