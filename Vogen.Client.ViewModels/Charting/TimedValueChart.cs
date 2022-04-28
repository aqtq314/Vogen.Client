using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class TimedValueChart<TTime, TValue> : TimedEventChart<TimedValueItem<TTime, TValue>, TTime>
        where TTime : struct, IComparable<TTime>
    {
        TValue _InitialValue;

        public TValue InitialValue
        {
            get => _InitialValue;
            set => SetAndNotify(ref _InitialValue, value);
        }

        public TimedValueChart(TValue initialValue, IEnumerable<TimedValueItem<TTime, TValue>>? items = null) : base(items)
        {
            _InitialValue = initialValue;
        }
    }
}
