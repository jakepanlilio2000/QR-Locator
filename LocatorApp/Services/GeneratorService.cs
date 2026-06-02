using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using Newtonsoft.Json;
using QRCoder;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using LocatorApp.Models;

namespace LocatorApp.Services
{
    public class GeneratorService : IGeneratorService
    {
        private readonly ILoggerService _logger;
        private readonly string _tempFolder;

        public GeneratorService(ILoggerService logger)
        {
            _logger = logger;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _tempFolder = Path.Combine(appData, "JakePanlilioLocatorApp", "Temp");
            Directory.CreateDirectory(_tempFolder);
        }

        public async Task<string> GeneratePdfAsync(string csvPath, string outputPdfPath, IProgress<int> progress, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureFileIsNotLocked(outputPdfPath);

                    _logger.LogInfo($"Starting generation for {csvPath}");
                    var locators = ParseCsv(csvPath);
                    int total = locators.Count;

                    if (total == 0) throw new Exception("CSV is empty or invalid format.");

                    using (PdfDocument document = new PdfDocument())
                    {
                        document.Info.Title = "Scanner Locator Tags";

                        int maxCols = 2;
                        int maxRows = 3;

                        double pageMarginTop = XUnit.FromMillimeter(12);
                        double pageMarginLeft = XUnit.FromMillimeter(12);
                        double cellW = XUnit.FromMillimeter(95);
                        double cellH = XUnit.FromMillimeter(110);
                        double boxMargin = XUnit.FromMillimeter(2);

                        int col = 0;
                        int row = 0;

                        PdfPage page = document.AddPage();
                        page.Size = PdfSharp.PageSize.Legal;
                        XGraphics gfx = XGraphics.FromPdfPage(page);

                        XFont titleFont = new XFont("Arial", 9, XFontStyle.Regular);
                        XFont labelFont = new XFont("Arial", 9, XFontStyle.Bold);
                        XFont tableFont = new XFont("Arial", 8, XFontStyle.Regular);
                        XFont footerFont = new XFont("Arial", 8, XFontStyle.Italic);

                        XPen outerPen = new XPen(XColors.Black, 0.5);
                        XPen tablePen = new XPen(XColors.Black, 0.5);

                        for (int i = 0; i < total; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var loc = locators[i];

                            double tx = pageMarginLeft + (col * cellW);
                            double ty = pageMarginTop + (row * cellH);

                            gfx.DrawRectangle(outerPen, tx + boxMargin, ty + boxMargin, cellW - (boxMargin * 2), cellH - (boxMargin * 2));

                            gfx.DrawString("SCANNER LOCATOR TAG", titleFont, XBrushes.Black,
                                new XRect(tx, ty + XUnit.FromMillimeter(6), cellW, 0),
                                XStringFormats.TopCenter);

                            double maxTextWidth = XUnit.FromMillimeter(38).Point;
                            double currentFontSize = 55;
                            XFont dynamicNumberFont = new XFont("Arial", currentFontSize, XFontStyle.Bold);

                            while (gfx.MeasureString(loc.SlotNo, dynamicNumberFont).Width > maxTextWidth && currentFontSize > 12)
                            {
                                currentFontSize -= 1; 
                                dynamicNumberFont = new XFont("Arial", currentFontSize, XFontStyle.Bold);
                            }

                            gfx.DrawString(loc.SlotNo, dynamicNumberFont, XBrushes.Black,
                                new XRect(tx + XUnit.FromMillimeter(5), ty + XUnit.FromMillimeter(18), XUnit.FromMillimeter(38), XUnit.FromMillimeter(20)),
                                XStringFormats.TopLeft);

                            gfx.DrawString("SCANNER SLOT NO:", labelFont, XBrushes.Black,
                                new XRect(tx + XUnit.FromMillimeter(5), ty + XUnit.FromMillimeter(48), XUnit.FromMillimeter(40), XUnit.FromMillimeter(5)),
                                XStringFormats.TopLeft);

                            string qrPayload = GenerateJsonPayload(loc);
                            using (Bitmap qrBitmap = GenerateQrCode(qrPayload))
                            {
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    qrBitmap.Save(ms, ImageFormat.Png);
                                    using (XImage xImg = XImage.FromStream(ms))
                                    {
                                        double qrSize = XUnit.FromMillimeter(46);
                                        gfx.DrawImage(xImg, tx + XUnit.FromMillimeter(44), ty + XUnit.FromMillimeter(14), qrSize, qrSize);
                                    }
                                }
                            }

                            // Table Layout
                            double tableY = ty + XUnit.FromMillimeter(68);
                            double tX = tx + XUnit.FromMillimeter(5);
                            double rowH = XUnit.FromMillimeter(7.5);
                            double col1W = XUnit.FromMillimeter(45);
                            double col2W = XUnit.FromMillimeter(40);

                            DrawTableCell(gfx, tablePen, tableFont, "FIXED LOCATOR NO:", loc.Bay, tX, tableY, col1W, col2W, rowH);
                            DrawTableCell(gfx, tablePen, tableFont, "TOTAL BOXES COUNTED:", "", tX, tableY + (rowH * 1), col1W, col2W, rowH);
                            DrawTableCell(gfx, tablePen, tableFont, "TOTAL BOXES SCANNED:", "", tX, tableY + (rowH * 2), col1W, col2W, rowH);
                            DrawTableCell(gfx, tablePen, tableFont, "SCANNED BY:", "", tX, tableY + (rowH * 3), col1W, col2W, rowH);
                            DrawTableCell(gfx, tablePen, tableFont, "SIGN-OFF TL:", "", tX, tableY + (rowH * 4), col1W, col2W, rowH);

                            col++;
                            if (col >= maxCols)
                            {
                                col = 0;
                                row++;
                                if (row >= maxRows)
                                {

                                    if (i < total - 1)
                                    {
                                        page = document.AddPage();
                                        page.Size = PdfSharp.PageSize.Legal;
                                        gfx = XGraphics.FromPdfPage(page);
                                        row = 0;
                                    }
                                }
                            }

                            progress?.Report((i + 1) * 100 / total);
                        }


                        document.Save(outputPdfPath);
                        _logger.LogInfo($"Successfully generated PDF at {outputPdfPath}");
                        return outputPdfPath;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("PDF Generation failed.", ex);
                    throw;
                }
            }, cancellationToken);
        }

        private void EnsureFileIsNotLocked(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.None))
                    {
                    }
                }
                catch (IOException)
                {
                    throw new Exception($"The file '{Path.GetFileName(path)}' is currently open in another program (like a PDF viewer). Please close it and try again.");
                }
            }
        }

        private void DrawTableCell(XGraphics gfx, XPen pen, XFont font, string label, string value, double x, double y, double w1, double w2, double h)
        {
            gfx.DrawRectangle(pen, x, y, w1, h);
            gfx.DrawString(label, font, XBrushes.Black, new XRect(x + 2, y, w1, h), XStringFormats.CenterLeft);

            gfx.DrawRectangle(pen, x + w1, y, w2, h);
            gfx.DrawString(value, font, XBrushes.Black, new XRect(x + w1 + 2, y, w2, h), XStringFormats.CenterLeft);
        }

        private List<LocatorData> ParseCsv(string path)
        {
            var results = new List<LocatorData>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null
            };

            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    results.Add(new LocatorData
                    {
                        SlotNo = csv.GetField(0) ?? "",
                        Name = csv.GetField(1) ?? "",
                        Aisle = csv.GetField(2) ?? "",
                        Bay = csv.GetField(4) ?? "",
                        BayName = csv.GetField(5) ?? "",
                        Item = csv.GetField(6) ?? ""
                    });
                }
            }
            return results;
        }

        private string GenerateJsonPayload(LocatorData data)
        {
            var payload = new LocatorQrPayload
            {
                Info = new LocatorInfo
                {
                    SlotNo = data.SlotNo,
                    Name = data.Name,
                    Aisle = data.Aisle,
                    Bay = data.Bay,
                    BayName = data.BayName,
                    Item = data.Item,
                    RecNo = null,
                    LocStatus = "Open",
                    Username = null
                }
            };
            return JsonConvert.SerializeObject(payload);
        }

        private Bitmap GenerateQrCode(string payload)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.L))
                {
                    using (QRCode qrCode = new QRCode(qrCodeData))
                    {
                        return qrCode.GetGraphic(6);
                    }
                }
            }
        }
    }
}