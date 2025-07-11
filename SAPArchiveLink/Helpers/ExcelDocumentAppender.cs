using ClosedXML.Excel;
using SAPArchiveLink.Interfaces;

namespace SAPArchiveLink
{
    public class ExcelDocumentAppender: IDocumentAppender
    {
        public async Task<Stream> AppendAsync(Stream original, Stream additional)
        {
            // Load original and additional workbooks into memory streams
            var originalMemory = new MemoryStream();
            await original.CopyToAsync(originalMemory);
            originalMemory.Position = 0;

            var additionalMemory = new MemoryStream();
            await additional.CopyToAsync(additionalMemory);
            additionalMemory.Position = 0;

            using var originalWorkbook = new XLWorkbook(originalMemory);
            using var additionalWorkbook = new XLWorkbook(additionalMemory);

            foreach (var sheetToAdd in additionalWorkbook.Worksheets)
            {
                var sheetName = sheetToAdd.Name;
                IXLWorksheet targetSheet;

                if (originalWorkbook.Worksheets.TryGetWorksheet(sheetName, out targetSheet))
                {
                    var lastRow = targetSheet.LastRowUsed()?.RowNumber() ?? 0;

                    foreach (var row in sheetToAdd.RowsUsed())
                    {
                        var newRow = targetSheet.Row(++lastRow);
                        row.CopyTo(newRow); // Copy all cells and formatting
                    }
                }
                else
                {
                    // Sheet doesn't exist, just copy it over
                    sheetToAdd.CopyTo(originalWorkbook, sheetName);
                }
            }

            var output = new MemoryStream();
            originalWorkbook.SaveAs(output);
            output.Position = 0;
            return output;
        }
    }
    
}
