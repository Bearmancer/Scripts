namespace CSharpScripts.CLI;

#region AllowedValuesAttribute

[AttributeUsage(validOn: AttributeTargets.Property)]
public sealed class AllowedValuesAttribute(params string[] values)
    : ParameterValidationAttribute($"Must be one of: {Join(separator: ", ", value: values)}")
{
    private readonly FrozenSet<string> allowedValues = values.ToFrozenSet(
        comparer: StringComparer.OrdinalIgnoreCase
    );

    public override ValidationResult Validate(CommandParameterContext context)
    {
        if (context.Value is null)
            return ValidationResult.Success();

        string value = context.Value.ToString() ?? "";

        return allowedValues.Contains(item: value)
            ? ValidationResult.Success()
            : ValidationResult.Error($"Invalid value '{value}'. {ErrorMessage}");
    }
}

#endregion

#region NotEmptyAttribute

[AttributeUsage(validOn: AttributeTargets.Property)]
public sealed class NotEmptyAttribute()
    : ParameterValidationAttribute(errorMessage: "Value cannot be empty")
{
    public override ValidationResult Validate(CommandParameterContext context)
    {
        return context.Value switch
        {
            null => ValidationResult.Success(),
            string s when IsNullOrWhiteSpace(value: s) => ValidationResult.Error(
                message: ErrorMessage
            ),
            _ => ValidationResult.Success(),
        };
    }
}

#endregion
