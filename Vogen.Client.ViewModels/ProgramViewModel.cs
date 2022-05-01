using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vogen.Client.ViewModels.Charting;
using Vogen.Client.ViewModels.Utils;
using Vogen.Synth;

namespace Vogen.Client.ViewModels
{
    public class ProgramViewModel : ViewModelBase
    {
        Composition _ActiveComp;

        public Composition ActiveComp
        {
            get => _ActiveComp;
            set => SetAndNotify(ref _ActiveComp, value);
        }

        public ProgramViewModel()
        {
            _ActiveComp = new Composition();
        }

        public void New()
        {
            ActiveComp = new Composition();
        }

        public void LoadComp(Composition comp)
        {
            ActiveComp = comp;
        }

        //public void ImportFromVog(VogPackage.VogPackage vog)
        //{
        //    var chart = vog.Chart;

        //    var initTimeSig = TimeSignature.TryParse(chart.TimeSig0).GetOrDefault(new TimeSignature(4, 4));
        //    var initTempo = chart.Bpm0;
        //    var noteTracks = chart.Utts.Select(utt =>
        //    {
        //        var notes = new List<Note>();
        //        for (var i = 0; i < utt.Notes.Length; i++)
        //        {
        //            var note = utt.Notes[i];
        //            notes.Add(new Note(note.Pitch, note.Lyric, note.Rom, note.On));

        //            if (i + 1 < utt.Notes.Length)
        //            {
        //                var nextNote = utt.Notes[i + 1];
        //                if (note.Off < nextNote.On)
        //                    // Spacing between notes
        //                    notes.Add(new Note(note.Off));
        //            }
        //            else
        //            {
        //                // End of track note
        //                notes.Add(new Note(note.Off));
        //            }
        //        }

        //        return new Track(utt.Name, utt.SingerId, utt.RomScheme, notes.OrderBy(note => note.On));
        //    });

        //    ActiveChart = new Composition(initTimeSig, initTempo, noteTracks);
        //}
    }
}
