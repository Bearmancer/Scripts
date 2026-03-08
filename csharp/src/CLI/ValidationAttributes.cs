namespace CSharpScripts.CLI;

[AttributeUsage(AttributeTargets.Property)]
internal sealed class AllowedValuesAttribute(params string[] values)
	: ParameterValidationAttribute($"Must be one of: {Join(", ", values)}")
{
	private readonly FrozenSet<string> allowedValues = values.ToFrozenSet(
		StringComparer.OrdinalIgnoreCase
	);

	public IReadOnlyList<string> Values => [.. allowedValues];

	public override ValidationResult Validate(CommandParameterContext context)
	{
		if (context.Value is null)
			return ValidationResult.Success();

		var value = context.Value.ToString() ?? "";

		return allowedValues.Contains(value)
			? ValidationResult.Success()
			: ValidationResult.Error($"Invalid value '{value}'. {ErrorMessage}");
	}
}
