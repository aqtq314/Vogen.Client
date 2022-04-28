using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class F0AnchorChart : TimedEventChart<F0AnchorItem, MidiClock>
    {
        public F0AnchorChart(IEnumerable<F0AnchorItem>? f0Anchors = null)
            : base(f0Anchors)
        {
        }

        protected override void UpdateChartStates()
        {
            base.UpdateChartStates();

            // TODO: interpolate between anchors for actual F0 Curve
        }
    }
}
