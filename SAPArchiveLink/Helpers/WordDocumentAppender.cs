using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using SAPArchiveLink.Interfaces;

namespace SAPArchiveLink
{
    public class WordDocumentAppender: IDocumentAppender
    {
        /// <summary>
        /// Appends the content of a new Word document stream to an existing Word document stream.
        /// </summary>
        /// <param name="existingStream"></param>
        /// <param name="newContentStream"></param>
        /// <returns></returns>
        public async Task<Stream> AppendAsync(Stream existingStream, Stream newContentStream)
        {
            // Copy existing document stream into a memory stream
            var outputStream = new MemoryStream();
            await existingStream.CopyToAsync(outputStream);
            outputStream.Position = 0;

            using (var mainDoc = WordprocessingDocument.Open(outputStream, true))
            {
                var mainBody = mainDoc.MainDocumentPart.Document.Body;

                // Read the new document stream into memory to prevent locking issues
                var newDocMemory = new MemoryStream();
                await newContentStream.CopyToAsync(newDocMemory);
                newDocMemory.Position = 0;

                using (var newDoc = WordprocessingDocument.Open(newDocMemory, false))
                {
                    var newBody = newDoc.MainDocumentPart.Document.Body;

                    // Append all elements from new body to main body
                    foreach (var element in newBody.Elements<OpenXmlElement>())
                    {
                        mainBody.Append(element.CloneNode(true));
                    }

                    mainDoc.MainDocumentPart.Document.Save();
                }
            }

            outputStream.Position = 0;
            return outputStream;
        }
    }
}
