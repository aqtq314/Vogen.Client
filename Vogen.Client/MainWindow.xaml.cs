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

namespace Vogen.Client
{
    public partial class MainWindow : Window
    {
        public ProgramModel ProgramModel => (ProgramModel)base.DataContext;

        public MainWindow()
        {
            InitializeComponent();
            noteChartEditPanel.Focus();
        }

        private void OnClickPlayButton(object sender, RoutedEventArgs e)
        {
            ProgramModel.Play();
        }

        private void OnClickStopButton(object sender, RoutedEventArgs e)
        {
            ProgramModel.Stop();
        }

        private void OnClickClearAllSynthButton(object sender, RoutedEventArgs e)
        {
            ProgramModel.ClearAllSynth();
        }

        private void OnClickSynthButton(object sender, RoutedEventArgs e)
        {
            ProgramModel.Synth(Dispatcher);
        }
    }
}
