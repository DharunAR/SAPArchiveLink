using System.ComponentModel.DataAnnotations;

namespace SAPArchiveLink
{
    public class SapSearchRequestModel : IValidatableObject
    {
        private string _pattern = string.Empty;

        [Required(ErrorMessage = "Content repository (contRep) is required.")]
        public required string ContRep { get; set; }

        [Required(ErrorMessage = "Document ID (docId) is required.")]
        public required string DocId { get; set; }       

        [Required(ErrorMessage = "Search pattern is required.")]
        public required string Pattern
        {
            get => _pattern;
            set => _pattern = Uri.UnescapeDataString(value);
        }

        [Required(ErrorMessage = "Component ID (compId) is required.")]
        public required string CompId { get; set; }

        [Required(ErrorMessage = "Primary version (pVersion) is required.")]
        public required string PVersion { get; set; }

        public bool CaseSensitive { get; set; } = false;

        public int FromOffset { get; set; } = 0;

        public int ToOffset { get; set; } = -1;

        public int NumResults { get; set; } = 1;
     
        public string AccessMode { get; set; }
     
        public string AuthId { get; set; }

        public string Expiration { get; set; }

        public string? SecKey { get; set; }

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
