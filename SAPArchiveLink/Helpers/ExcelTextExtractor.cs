using ClosedXML.Excel;
using System.Text;

namespace SAPArchiveLink
{
    public class ExcelTextExtractor : ITextExtractor
    {
        public string ExtractText(Stream stream)
        {
            var sb = new StringBuilder();
            using var workbook = new XLWorkbook(stream);
            foreach (var worksheet in workbook.Worksheets)
            {
                sb.AppendLine($"Sheet: {worksheet.Name}");
                foreach (var row in worksheet.RowsUsed())
                {
                    foreach (var cell in row.Cells())
                    {
                        sb.Append(cell.Value.ToString()).Append("\t");
                    }
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }
    }
}
