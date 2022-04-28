using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class PhonemeChart : TimedEventChart<TimedValueItem<TimeSpan, string?>, TimeSpan>
    {
        public PhonemeChart(IEnumerable<TimedValueItem<TimeSpan, string?>>? phonemes = null)
            : base(phonemes)
        { }
    }
}
