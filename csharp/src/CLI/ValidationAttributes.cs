namespace CSharpScripts.CLI;

[AttributeUsage(validOn: AttributeTargets.Property)]
internal sealed class AllowedValuesAttribute(params string[] values)
	: ParameterValidationAttribute($"Must be one of: {Join(separator: ", ", value: values)}")
{
	private readonly FrozenSet<string> AllowedValues = values.ToFrozenSet(
		comparer: StringComparer.OrdinalIgnoreCase
	);

	public IReadOnlyList<string> Values => [.. AllowedValues];

	public override ValidationResult Validate(CommandParameterContext context)
	{
		if (context.Value is null)
			return ValidationResult.Success();

		var value = context.Value.ToString() ?? "";

		return AllowedValues.Contains(item: value)
			? ValidationResult.Success()
			: ValidationResult.Error($"Invalid value '{value}'. {ErrorMessage}");
	}
}
