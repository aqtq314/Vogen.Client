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
        Task<Task>? updateTask = null;

        public App()
        {
            DispatcherUnhandledException += OnApplicationDispatcherUnhandledException;
        }

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
                var mainWindow = MainWindow as MainWindowV01;
                mainWindow?.SaveACopy();
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            updateTask = AutoUpdater.checkUpdates();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            if (updateTask?.IsCompleted == true)
            {
                var update = updateTask.Result;
                update.Start();
                update.Wait();
            }
        }
    }
}
