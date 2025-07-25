using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using NUnit.Framework;
using SAPArchiveLink;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class ExcelDocumentAppenderTests
    {
        [Test]
        public async Task AppendAsync_AppendsRows_WhenSheetExists()
        {
            // Arrange
            var originalStream = new MemoryStream();
            var additionalStream = new MemoryStream();

            // Create original workbook with one sheet and one row
            using (var wb = new XLWorkbook())
            {
                var ws = wb.AddWorksheet("Sheet1");
                ws.Cell(1, 1).Value = "Original";
                wb.SaveAs(originalStream);
            }
            originalStream.Position = 0;

            // Create additional workbook with same sheet name and one row
            using (var wb = new XLWorkbook())
            {
                var ws = wb.AddWorksheet("Sheet1");
                ws.Cell(1, 1).Value = "Additional";
                wb.SaveAs(additionalStream);
            }
            additionalStream.Position = 0;

            var appender = new ExcelDocumentAppender();

            // Act
            var resultStream = await appender.AppendAsync(originalStream, additionalStream);

            // Assert
            using (var resultWorkbook = new XLWorkbook(resultStream))
            {
                var sheet = resultWorkbook.Worksheet("Sheet1");
                Assert.That("Original",Is.EqualTo(sheet.Cell(1, 1).Value));
                Assert.That("Additional", Is.EqualTo(sheet.Cell(2, 1).Value));
            }
        }

        [Test]
        public async Task AppendAsync_CopiesSheet_WhenSheetDoesNotExist()
        {
            // Arrange
            var originalStream = new MemoryStream();
            var additionalStream = new MemoryStream();

            // Create original workbook with one sheet
            using (var wb = new XLWorkbook())
            {
                wb.AddWorksheet("Sheet1").Cell(1, 1).Value = "Original";
                wb.SaveAs(originalStream);
            }
            originalStream.Position = 0;

            // Create additional workbook with a different sheet
            using (var wb = new XLWorkbook())
            {
                wb.AddWorksheet("Sheet2").Cell(1, 1).Value = "Additional";
                wb.SaveAs(additionalStream);
            }
            additionalStream.Position = 0;

            var appender = new ExcelDocumentAppender();

            // Act
            var resultStream = await appender.AppendAsync(originalStream, additionalStream);

            // Assert
            using (var resultWorkbook = new XLWorkbook(resultStream))
            {
                Assert.That(resultWorkbook.Worksheet("Sheet1"), Is.Not.Null);
                Assert.That(resultWorkbook.Worksheet("Sheet2"), Is.Not.Null);
                Assert.That("Original", Is.EqualTo(resultWorkbook.Worksheet("Sheet1").Cell(1, 1).Value));
                Assert.That("Additional", Is.EqualTo(resultWorkbook.Worksheet("Sheet2").Cell(1, 1).Value));
            }
        }
    }
}
