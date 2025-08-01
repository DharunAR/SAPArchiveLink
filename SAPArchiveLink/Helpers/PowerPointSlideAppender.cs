using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using SAPArchiveLink.Interfaces;

namespace SAPArchiveLink
{
    public class PowerPointSlideAppender : IDocumentAppender
    {
        public async Task<Stream> AppendAsync(Stream existingStream, Stream newContentStream)
        {
            var outputStream = new MemoryStream();
            await existingStream.CopyToAsync(outputStream);
            outputStream.Position = 0;

            // Read new content into memory
            var newDocMemory = new MemoryStream();
            await newContentStream.CopyToAsync(newDocMemory);
            newDocMemory.Position = 0;

            using (var mainPresentation = PresentationDocument.Open(outputStream, true))
            using (var newPresentation = PresentationDocument.Open(newDocMemory, false))
            {
                var mainPresPart = mainPresentation.PresentationPart;
                var newPresPart = newPresentation.PresentationPart;

                var mainPres = mainPresPart.Presentation;
                var newPres = newPresPart.Presentation;

                var slideIdList = mainPres.SlideIdList ?? mainPres.AppendChild(new SlideIdList());
                uint maxSlideId = slideIdList.ChildElements
                                             .OfType<SlideId>()
                                             .Select(s => s.Id.Value)
                                             .DefaultIfEmpty(256U)
                                             .Max();

                foreach (var slidePart in newPresPart.SlideParts)
                {
                    var importedPart = mainPresPart.AddPart(slidePart);
                    maxSlideId++;

                    var relId = mainPresPart.GetIdOfPart(importedPart);
                    slideIdList.Append(new SlideId() { Id = maxSlideId, RelationshipId = relId });
                }

                mainPres.Save();
            }

            outputStream.Position = 0;
            return outputStream;
        }
    }
}
