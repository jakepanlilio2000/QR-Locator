using LocatorApp.Services;
using LocatorApp.ViewModels;
using System;
using System.Windows;

namespace LocatorApp
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ILoggerService logger = new LoggerService();
            IDialogService dialogService = new DialogService();
            IGeneratorService generatorService = new GeneratorService(logger);

            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel(dialogService, generatorService, logger)
            };

            mainWindow.Show();
        }
    }
}