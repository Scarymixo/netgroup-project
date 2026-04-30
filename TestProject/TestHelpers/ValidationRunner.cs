using System.ComponentModel.DataAnnotations;

namespace TestProject.TestHelpers;

public static class ValidationRunner
{
    public static (bool IsValid, IList<ValidationResult> Results) Validate(object instance)
    {
        var ctx = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        var ok = Validator.TryValidateObject(instance, ctx, results, validateAllProperties: true);
        return (ok, results);
    }
}
