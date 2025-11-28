using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Validation;

/// <summary>
/// Validation attribute that ensures a boolean value is true.
/// Useful for terms acceptance checkboxes.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MustBeTrueAttribute : ValidationAttribute
{
    public MustBeTrueAttribute() : base("The field must be checked.")
    {
    }

    public override bool IsValid(object? value)
    {
        return value is bool boolValue && boolValue;
    }
}
