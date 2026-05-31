using System;
using System.Threading;
using System.Threading.Tasks;

namespace LocatorApp.Services
{
    public interface IGeneratorService
    {
        Task<string> GeneratePdfAsync(string csvPath, string outputPdfPath, IProgress<int> progress, CancellationToken cancellationToken);
    }
}