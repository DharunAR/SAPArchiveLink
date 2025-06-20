namespace SAPArchiveLink
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public static class ModelValidator
    {
        /// <summary>  
        /// Validates any model using DataAnnotations and IValidatableObject (if implemented).  
        /// </summary>  
        /// <typeparam name="T">The type of model.</typeparam>  
        /// <param name="model">The model instance to validate.</param>  
        /// <returns>A list of validation results. Empty if valid.</returns>  
        public static List<ValidationResult> Validate<T>(T model, bool isValidationReq = true)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model), "The model instance cannot be null.");
            }

            var results = new List<ValidationResult>();
            var context = new ValidationContext(model, serviceProvider: null, items: new Dictionary<object, object?>
                {
                    { "IsValidationRequired", isValidationReq }
                });

            // This validates:  
            // - [Required], [StringLength], etc.  
            // - IValidatableObject.Validate() if implemented  
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);

            return results;
        }
    }
}
