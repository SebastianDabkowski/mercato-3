using System.ComponentModel.DataAnnotations;
using MercatoApp.Models;

namespace MercatoApp.Validation;

/// <summary>
/// Validates that the sum of shipped and cancelled quantities does not exceed the total quantity.
/// Used for order item fulfillment validation.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ValidateOrderItemQuantitiesAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is OrderItem item)
        {
            var totalFulfilled = item.QuantityShipped + item.QuantityCancelled;
            
            if (totalFulfilled > item.Quantity)
            {
                return new ValidationResult(
                    $"The sum of shipped ({item.QuantityShipped}) and cancelled ({item.QuantityCancelled}) quantities cannot exceed the total quantity ({item.Quantity}).",
                    new[] { nameof(item.QuantityShipped), nameof(item.QuantityCancelled) });
            }
        }

        return ValidationResult.Success;
    }
}
