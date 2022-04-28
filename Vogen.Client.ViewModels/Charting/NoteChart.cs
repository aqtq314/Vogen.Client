using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Vogen.Client.ViewModels.Charting
{
    public class NoteChart : TimedEventChart<NoteItem, MidiClock>
    {
        public NoteChart(IEnumerable<NoteItem>? notes = null)
            : base(notes)
        {
        }
    }
}
