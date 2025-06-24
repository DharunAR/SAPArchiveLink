using System.ComponentModel.DataAnnotations;

namespace SAPArchiveLink
{
    public class SapDocumentRequest : IValidatableObject
    {
        [Required(ErrorMessage = "docId is required.")]
        public string DocId { get; set; }
        [Required(ErrorMessage = "contRep is required.")]
        public string ContRep { get; set; }
        [Required(ErrorMessage ="PVersion is required.")]
        public string PVersion { get; set; }
        public string CompId { get; set; }
        public string SecKey { get; set; }
        public string AccessMode { get; set; }
        public string AuthId { get; set; }
        public string Expiration { get; set; }
        public long FromOffset { get; set; }
        public long ToOffset { get; set; }
        public string ResultAs { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
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
