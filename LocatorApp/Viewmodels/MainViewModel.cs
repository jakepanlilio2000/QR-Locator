using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocatorApp.Services;

namespace LocatorApp.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private readonly IGeneratorService _generatorService;
        private readonly ILoggerService _logger;
        private CancellationTokenSource _cancellationTokenSource;

        private string _selectedFilePath;
        public string SelectedFilePath
        {
            get => _selectedFilePath;
            set
            {
                if (SetProperty(ref _selectedFilePath, value))
                {
                    GenerateCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    BrowseCommand.NotifyCanExecuteChanged();
                    GenerateCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private int _progressValue;
        public int ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        private string _progressText = "Ready";
        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        private bool _isPdfReady;
        public bool IsPdfReady
        {
            get => _isPdfReady;
            set => SetProperty(ref _isPdfReady, value);
        }

        private string _generatedPdfPath;

        public IRelayCommand BrowseCommand { get; }
        public IAsyncRelayCommand GenerateCommand { get; }
        public IRelayCommand OpenPdfCommand { get; }

        public MainViewModel(IDialogService dialogService, IGeneratorService generatorService, ILoggerService logger)
        {
            _dialogService = dialogService;
            _generatorService = generatorService;
            _logger = logger;

            BrowseCommand = new RelayCommand(Browse, CanBrowse);
            GenerateCommand = new AsyncRelayCommand(GenerateAsync, CanGenerate);
            OpenPdfCommand = new RelayCommand(OpenPdf);
        }

        private void Browse()
        {
            string file = _dialogService.OpenCsvFileDialog();
            if (!string.IsNullOrEmpty(file))
            {
                SelectedFilePath = file;
                IsPdfReady = false;
                ProgressValue = 0;
                ProgressText = "File Selected. Ready to Generate.";
            }
        }
        private bool CanBrowse() => !IsBusy;

        private async Task GenerateAsync()
        {
            if (string.IsNullOrEmpty(SelectedFilePath)) return;

            string defaultFileName = Path.GetFileNameWithoutExtension(SelectedFilePath) + "_Generated.pdf";
            string savePath = _dialogService.SavePdfFileDialog(defaultFileName);
            if (string.IsNullOrEmpty(savePath)) return;

            IsBusy = true;
            IsPdfReady = false;
            ProgressValue = 0;
            ProgressText = "Preparing locators...";

            _cancellationTokenSource = new CancellationTokenSource();

            var progress = new Progress<int>(percent =>
            {
                ProgressValue = percent;
                ProgressText = $"Processing locators... {percent}%";
            });

            try
            {
                _generatedPdfPath = await _generatorService.GeneratePdfAsync(SelectedFilePath, savePath, progress, _cancellationTokenSource.Token);

                ProgressText = "Generation Complete!";
                IsPdfReady = true;
                _dialogService.ShowMessage("PDF Generated Successfully.");
            }
            catch (OperationCanceledException)
            {
                ProgressText = "Generation Cancelled.";
            }
            catch (Exception ex)
            {
                ProgressText = "Error during generation.";
                _logger.LogError("Error in GenerateAsync", ex);
                _dialogService.ShowError(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }
        private bool CanGenerate() => !IsBusy && !string.IsNullOrEmpty(SelectedFilePath);

        private void OpenPdf()
        {
            if (string.IsNullOrEmpty(_generatedPdfPath)) return;

            try
            {
                Process.Start(new ProcessStartInfo(_generatedPdfPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger.LogError("Could not open PDF file.", ex);
                _dialogService.ShowError("Could not open the PDF file. Ensure you have a PDF viewer installed.");
            }
        }
    }
}