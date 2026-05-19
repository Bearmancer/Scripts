using System.Text;

namespace CSharpScripts.Core;

internal static class SheetNameHelper
{
	public static string Sanitize(string name)
	{
		StringBuilder sb = new(name.Length + 4);
		foreach (var c in name)
		{
			switch (c)
			{
				case ':':
					sb.Append(value: " -");
					break;
				case '/':
				case '\\':
					sb.Append(value: '-');
					break;
				case '[':
					sb.Append(value: '(');
					break;
				case ']':
					sb.Append(value: ')');
					break;
				case '?':
				case '*':
					break;
				default:
					sb.Append(value: c);
					break;
			}
		}
		return sb.ToString();
	}

	public static string EscapeForFormula(string name) =>
		name.Contains(value: '\'') || name.Contains(value: ' ') || name.Contains(value: '-')
			? $"'{name.Replace(oldValue: "'", newValue: "''")}'"
			: name;
}
