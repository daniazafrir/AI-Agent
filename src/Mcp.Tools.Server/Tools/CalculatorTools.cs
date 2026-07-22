using System.ComponentModel;
using System.Data;
using ModelContextProtocol.Server;

namespace Mcp.Tools.Server.Tools;

[McpServerToolType]
public sealed class CalculatorTools
{
    [McpServerTool(Name = "calculate")]
    [Description(
        "Evaluates a basic arithmetic expression containing numbers, " +
        "parentheses, addition, subtraction, multiplication, and division.")]
    public static object Calculate(
        [Description("Arithmetic expression, for example: (542 * 83) / 2")]
        string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ArgumentException(
                "Expression is required.",
                nameof(expression));
        }

        if (!IsAllowedExpression(expression))
        {
            throw new ArgumentException(
                "The expression contains unsupported characters.",
                nameof(expression));
        }

        var table = new DataTable
        {
            Locale = System.Globalization.CultureInfo.InvariantCulture
        };

        var result = table.Compute(expression, null);

        return new
        {
            success = true,
            expression,
            result = Convert.ToString(
                result,
                System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    private static bool IsAllowedExpression(string expression)
    {
        return expression.All(character =>
            char.IsDigit(character)
            || char.IsWhiteSpace(character)
            || character is '.'
                or '+'
                or '-'
                or '*'
                or '/'
                or '('
                or ')');
    }
}