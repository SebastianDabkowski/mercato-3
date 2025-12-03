namespace MercatoApp.Models;

/// <summary>
/// Represents the data type of a category attribute.
/// </summary>
public enum AttributeType
{
    /// <summary>
    /// Single-line text field.
    /// </summary>
    Text = 1,

    /// <summary>
    /// Multi-line text area.
    /// </summary>
    TextArea = 2,

    /// <summary>
    /// Numeric value (integer or decimal).
    /// </summary>
    Number = 3,

    /// <summary>
    /// Boolean value (yes/no, true/false).
    /// </summary>
    Boolean = 4,

    /// <summary>
    /// Date value.
    /// </summary>
    Date = 5,

    /// <summary>
    /// Single selection from a predefined list of options.
    /// </summary>
    SingleSelect = 6,

    /// <summary>
    /// Multiple selections from a predefined list of options.
    /// </summary>
    MultiSelect = 7
}
