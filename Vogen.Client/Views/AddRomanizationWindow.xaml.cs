using Doaz.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Vogen.Synth.Romanization;

namespace Vogen.Client.Views
{
    public partial class AddRomanizationWindow : Window
    {
        public static IMultiValueConverter AddRomanizationConverter => ValueConverter.CreateMulti(values =>
        {
            if (values.Length != 2) return null;
            if (values[0] == null) return null;
            if (values[1] == null) return null;
            var lyric = (string)values[0];
            var romScheme = (string)values[1];
            var romanizer = Romanizer.get(romScheme);

            //var chRegex = @"[\u3400-\u4DBF\u4E00-\u9FFF]";
            //return $"TEST OUTPUT\r\n{lyric}\r\nEND TEST OUTPUT";
            return Regex.Replace(
                lyric, @"[\u3400-\u4DBF\u4E00-\u9FFF]+",
                lineMatch =>
                {
                    var chs = lineMatch.Value.Select(c => c.ToString()).ToArray();
                    var roms = romanizer.Convert(chs, Array.ConvertAll(chs, c => ""))
                        .Select(roms => roms[0]);
                    return String.Join("", chs.Zip(roms, (ch, rom) => $"{ch}{rom}"));
                });
        });

        public AddRomanizationWindow()
        {
            InitializeComponent();

            Loaded += (sender, e) =>
            {
                inputTextBox.Focus();
                inputTextBox.SelectAll();
            };

            pasteButton.Click += (sender, e) =>
            {
                inputTextBox.Text = Clipboard.GetText();
            };

            copyResultButton.Click += (sender, e) =>
            {
                Clipboard.SetText(outputTextBox.Text);
                outputTextBox.SelectAll();
                outputTextBox.Focus();
            };
        }
    }
}
