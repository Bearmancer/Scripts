namespace CSharpScripts.CLI;

[AttributeUsage(AttributeTargets.Property)]
public sealed class AllowedValuesAttribute(params string[] values)
    : ParameterValidationAttribute($"Must be one of: {Join(", ", values)}")
{
    private readonly FrozenSet<string> allowedValues = values.ToFrozenSet(
        StringComparer.OrdinalIgnoreCase
    );

    public override ValidationResult Validate(CommandParameterContext context)
    {
        if (context.Value is null)
            return ValidationResult.Success(); // Let [Required] handle null checks

        var value = context.Value.ToString() ?? "";

        return allowedValues.Contains(value)
            ? ValidationResult.Success()
            : ValidationResult.Error($"Invalid value '{value}'. {ErrorMessage}");
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class NotEmptyAttribute() : ParameterValidationAttribute("Value cannot be empty")
{
    public override ValidationResult Validate(CommandParameterContext context)
    {
        return context.Value switch
        {
            null => ValidationResult.Success(),
            string s when IsNullOrWhiteSpace(s) => ValidationResult.Error(ErrorMessage),
            _ => ValidationResult.Success(),
        };
    }
}
