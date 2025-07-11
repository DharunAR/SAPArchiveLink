using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SAPArchiveLink
{
    public class AppendSapDocCompModel
    {

        [Required(ErrorMessage = "contRep is required.")]
        public required string ContRep { get; set; }

        [Required(ErrorMessage = "docId is required.")]
        public required string DocId { get; set; }

        [Required(ErrorMessage = "CompId is required.")]
        public string CompId { get; set; }

        [Required(ErrorMessage = "pVersion is required.")]
        public required string PVersion { get; set; }    

        public string AccessMode { get; set; }

        [FromQuery(Name = "authId")]
        public string AuthId { get; set; }

        public string Expiration { get; set; }

        public string? SecKey { get; set; }

        public string? ScanPerformed { get; set; }

        public Stream StreamData { get; set; }

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
