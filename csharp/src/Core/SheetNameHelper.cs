using System.Text;

namespace CSharpScripts.Core;

internal static class SheetNameHelper
{
	public static string Sanitize(string name)
	{
		var sb = new StringBuilder(name.Length + 4);
		foreach (var c in name)
		{
			switch (c)
			{
				case ':':
					sb.Append(" -");
					break;
				case '/':
				case '\\':
					sb.Append('-');
					break;
				case '[':
					sb.Append('(');
					break;
				case ']':
					sb.Append(')');
					break;
				case '?':
				case '*':
					break;
				default:
					sb.Append(c);
					break;
			}
		}
		return sb.ToString();
	}

	public static string EscapeForFormula(string name) =>
		name.Contains('\'') || name.Contains(' ') || name.Contains('-')
			? $"'{name.Replace("'", "''")}'"
			: name;
}


