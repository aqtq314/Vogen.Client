using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Vogen.Client.Controls;
using Vogen.Client.ViewModel;
using Vogen.Client.Views;

namespace Vogen.Client
{
    public partial class MainWindow : MainWindowBase
    {
        public MainWindow()
        {
            InitializeComponent();
            noteChartEditPanel.Focus();

            newButton.Click += (sender, e) => New();
            openButton.Click += (sender, e) => Open();
            saveButton.Click += (sender, e) => Save();
            saveAsButton.Click += (sender, e) => SaveAs();

            importButton.Click += (sender, e) => Import();

            playButton.Click += (sender, e) => ProgramModel.Play();
            stopButton.Click += (sender, e) => ProgramModel.Stop();

            clearAllSynthButton.Click += (sender, e) => ProgramModel.ClearAllSynth();
            synthButton.Click += (sender, e) => ProgramModel.Synth(Dispatcher);
        }
    }
}
