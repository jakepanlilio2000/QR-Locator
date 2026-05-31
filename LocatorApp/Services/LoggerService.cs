using System;
using System.IO;

namespace LocatorApp.Services
{
    public class LoggerService : ILoggerService
    {
        private readonly string _logFilePath;

        public LoggerService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string logFolder = Path.Combine(appData, "JakePanlilioLocatorApp", "Logs");
            Directory.CreateDirectory(logFolder);
            _logFilePath = Path.Combine(logFolder, $"AppLog_{DateTime.Now:yyyyMMdd}.txt");
        }

        public void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public void LogError(string message, Exception ex = null)
        {
            WriteLog("ERROR", $"{message} | Exception: {ex?.Message}");
        }

        private void WriteLog(string level, string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, logEntry);
            }
            catch
            {
            }
        }
    }
}