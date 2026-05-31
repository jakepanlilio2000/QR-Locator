namespace LocatorApp.Services
{
    public interface IDialogService
    {
        string OpenCsvFileDialog();
        string SavePdfFileDialog(string defaultFileName);
        void ShowError(string message);
        void ShowMessage(string message);
    }
}