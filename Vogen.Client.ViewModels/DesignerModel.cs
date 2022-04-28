using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vogen.Client.ViewModels.Charting;
using Vogen.Client.ViewModels.Utils;
using Vogen.Synth;

namespace Vogen.Client.ViewModels
{
    public static class DesignerModel
    {
        public static ProgramViewModel Program { get; private set; }

        static float[] GetSampleDryVocal()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(@"Vogen.Client.ViewModels.2100-016-030.m4a") ??
                throw new KeyNotFoundException();

            var audioBytes = stream.ReadAllBytes();
            return AudioSamplesModule.decode(audioBytes);
        }

        static DesignerModel()
        {
            //using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(@"Vogen.Client.ViewModels.testComp.vog") ??
            //    throw new KeyNotFoundException();
            //var vog = VogPackage.read(stream, ".");

            Program = new ProgramViewModel();
            //Program.ImportFromVog(vog);

            Composition comp = new Composition(
                new TimeSignature(4, 4), 120,
                Util.Array.ofParams(
                    TimedValueItem.Create(0, new TimeSignature(6, 8)),
                    TimedValueItem.Create(2, new TimeSignature(1, 4)),
                    TimedValueItem.Create(6, new TimeSignature(9, 16)),
                    TimedValueItem.Create(8, new TimeSignature(3, 4))),
                Util.Array.ofParams(
                    TimedValueItem.Create(new MidiClock(0), 72.0),
                    TimedValueItem.Create(new MidiClock(3840), 96.0),
                    TimedValueItem.Create(new MidiClock(7680), 120.0),
                    TimedValueItem.Create(new MidiClock(11520), 144.0)),
                Util.Array.ofParams(
                    new Track("Track 1", parts: Util.Array.ofParams(
                        new DryVocalPart(new MidiClock(1920), GetSampleDryVocal()))),
                    new Track("Track 2", parts: Util.Array.ofParams(
                        new NotePart(new MidiClock(1440), "Notes", "cmn",
                            notes: Util.Array.ofParams(
                                new NoteItem(new MidiClock(0), "我", "ngou", 62),
                                NoteItem.CreateSlur(new MidiClock(120), 64),
                                new NoteItem(new MidiClock(240), "自", "zy", 62),
                                new NoteItem(new MidiClock(480), "关山", "kue se", 64),
                                NoteItem.CreateCont(new MidiClock(960), 71),
                                NoteItem.CreateSlur(new MidiClock(1080), 74),
                                NoteItem.CreateSlur(new MidiClock(1320), 71),
                                new NoteItem(new MidiClock(1440), "点酒", "tie tseu", 71),
                                NoteItem.CreateSlur(new MidiClock(1680), 74),
                                NoteItem.CreateSlur(new MidiClock(1800), 71),
                                NoteItem.CreateCont(new MidiClock(1920), 69),
                                NoteItem.CreateRest(new MidiClock(2280)),
                                new NoteItem(new MidiClock(2400), "千秋", "tshie tsheu", 69),
                                NoteItem.CreateSlur(new MidiClock(2520), 71),
                                NoteItem.CreateCont(new MidiClock(2640), 69),
                                new NoteItem(new MidiClock(2880), "皆", "cia", 69),
                                NoteItem.CreateSlur(new MidiClock(3000), 64),
                                new NoteItem(new MidiClock(3120), "入", "zeq", 64),
                                new NoteItem(new MidiClock(3360), "喉", "gheu", 64),
                                NoteItem.CreateSlur(new MidiClock(3600), 67),
                                NoteItem.CreateRest(new MidiClock(3840)))))),
                    new Track("Track 3", parts: Util.Array.ofParams(
                        new NotePart(new MidiClock(1920), "Notes", "cmn",
                            notes: Util.Array.ofParams(
                                new NoteItem(new MidiClock(0), "关山", "kue se", 59),
                                NoteItem.CreateCont(new MidiClock(480), 64),
                                NoteItem.CreateSlur(new MidiClock(600), 67),
                                NoteItem.CreateSlur(new MidiClock(840), 64),
                                new NoteItem(new MidiClock(960), "点酒", "tie tseu", 66),
                                NoteItem.CreateSlur(new MidiClock(1200), 69),
                                NoteItem.CreateSlur(new MidiClock(1320), 66),
                                NoteItem.CreateCont(new MidiClock(1440), 64),
                                NoteItem.CreateRest(new MidiClock(1920)))))),
                    new Track("Track 4", parts: Util.Array.ofParams(
                        new NotePart(new MidiClock(960), "Notes", "cmn",
                            notes: Util.Array.ofParams(
                                new NoteItem(new MidiClock(960), "关山", "kue se", 55),
                                NoteItem.CreateCont(new MidiClock(1440), 60),
                                new NoteItem(new MidiClock(1920), "点酒", "tie tseu", 57),
                                NoteItem.CreateCont(new MidiClock(2400), 60),
                                NoteItem.CreateRest(new MidiClock(2880)))),
                        new NotePart(new MidiClock(1920), "Notes", "cmn",
                            notes: Util.Array.ofParams(
                                new NoteItem(new MidiClock(0), "du", "du", 48),
                                new NoteItem(new MidiClock(960), "du", "du", 50),
                                NoteItem.CreateRest(new MidiClock(1920)),
                                NoteItem.CreateRest(new MidiClock(2880))))))));

            Program.LoadComp(comp);
        }
    }
}
