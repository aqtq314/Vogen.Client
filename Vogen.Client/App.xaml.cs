using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Vogen.Client.ViewModel;
using Vogen.Client.Views;

namespace Vogen.Client
{
    public partial class App : Application
    {
        private void OnApplicationDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var ex = e.Exception;
            var msgboxResult =
                MessageBox.Show(
                    $"The application has encountered an unhandled exception. " +
                    $"Press OK to save a copy of your work before the app terminates.\r\n\r\n" +
                    $"{ex.Message}\r\n\r\n{ex.StackTrace}", MainWindowBase.AppName,
                    MessageBoxButton.OKCancel, MessageBoxImage.Error);

            if (msgboxResult == MessageBoxResult.OK)
            {
                var mainWindow = MainWindow as MainWindow;
                mainWindow?.SaveACopy();
            }
        }
    }
}
