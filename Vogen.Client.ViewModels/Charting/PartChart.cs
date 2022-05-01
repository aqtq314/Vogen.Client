using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class PartChart : TimedEventChart<PartBase, MidiClock>
    {
        public PartChart(IEnumerable<PartBase>? parts = null)
            : base(parts)
        {
        }
    }
}
