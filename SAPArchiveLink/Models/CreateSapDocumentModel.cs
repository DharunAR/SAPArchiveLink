using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace SAPArchiveLink
{
    public class CreateSapDocumentModel: IValidatableObject
    {
        // Mandatory parameters (from URL)
        [Required(ErrorMessage = "docId is required.")]
        public required string DocId { get; set; }

        [Required(ErrorMessage = "contRep is required.")]
        public required string ContRep { get; set; }

        public string CompId { get; set; }

        [Required(ErrorMessage = "pVersion is required.")]
        public required string PVersion { get; set; }

        // Header/Body (usually handled in controller separately)
        [Required(ErrorMessage = "Content-Length is required.")]
        public required string ContentLength { get; set; }

        // Soft-Mandatory (conditionally required if secKey is present)
        public string AccessMode { get; set; }
        public string AuthId { get; set; }
        public string Expiration { get; set; }

        // Optional
        public string SecKey { get; set; }           // URL/optional
        public string ContentType { get; set; }      // body
        public string Charset { get; set; }          // body
        public string Version { get; set; }          // body
        public string DocProt { get; set; }          // server setting
        public string ScanPerformed { get; set; }    // URL
        public Stream Stream { get; set; }
        //For multipart components
        public List<SapDocumentComponentModel> Components { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {

            if (validationContext.Items.TryGetValue("IsValidationRequired", out var flag) && flag is bool IsValidationRequired && IsValidationRequired
                && string.IsNullOrWhiteSpace(CompId))
            {
                yield return new ValidationResult("CompId is required.", new[] { nameof(CompId) });
            }

            if (!string.IsNullOrWhiteSpace(SecKey))
            {
                if (string.IsNullOrWhiteSpace(AccessMode))
                    yield return new ValidationResult("AccessMode is required when SecKey is provided.", new[] { nameof(AccessMode) });

                if (string.IsNullOrWhiteSpace(AuthId))
                    yield return new ValidationResult("AuthId is required when SecKey is provided.", new[] { nameof(AuthId) });

                if (string.IsNullOrWhiteSpace(Expiration))
                    yield return new ValidationResult("Expiration is required when SecKey is provided.", new[] { nameof(Expiration) });
            }
        }
    }
}
