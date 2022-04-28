using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Charting
{
    public class AudioGraphemeChart : TimedEventChart<AudioGraphemeItem, TimeSpan>
    {
        public AudioGraphemeChart(IEnumerable<AudioGraphemeItem>? graphemes = null)
            : base(graphemes)
        { }
    }
}
