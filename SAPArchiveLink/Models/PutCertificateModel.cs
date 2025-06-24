using System.ComponentModel.DataAnnotations;

namespace SAPArchiveLink
{
    public class PutCertificateModel : IValidatableObject
    {
        // Mandatory parameters (from URL)
        [Required(ErrorMessage = "authId is required.")]
        public required string AuthId { get; set; }

        [Required(ErrorMessage = "contRep is required.")]
        public required string ContRep { get; set; } 

        [Required(ErrorMessage = "pVersion is required.")]
        public required string PVersion { get; set; }

        public string Permissions { get; set; }

        public Stream Stream { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return null;
        }
    }
}
