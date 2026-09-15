using System.ComponentModel;
using System.Data;
using System.Globalization;
using ModelContextProtocol.Server;

namespace Mcp.Tools.Server.Tools;

[McpServerToolType]
public sealed class CalculatorTools
{
    [McpServerTool(Name = "calculate")]
    [Description(
        "Evaluates only basic arithmetic expressions containing numbers, " +
        "parentheses, addition, subtraction, multiplication, and division. " +
        "Use this tool only for explicit mathematical calculations. " +
        "Do not use it for phone numbers, IDs, dates, document lookup, " +
        "contact information, or knowledge base questions.")]
    public static object Calculate(
        [Description(
            "A mathematical arithmetic expression only, " +
            "for example: (542 * 83) / 2")]
        string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return CreateError(
                expression,
                "Expression is required.");
        }

        expression =
            expression.Trim();

        if (!IsAllowedExpression(expression))
        {
            return CreateError(
                expression,
                "Only numbers and the operators +, -, *, /, and parentheses are supported.");
        }

        try
        {
            var table =
                new DataTable
                {
                    Locale =
                        CultureInfo.InvariantCulture
                };

            var result =
                table.Compute(
                    expression,
                    null);

            return new
            {
                success = true,
                expression,
                result = Convert.ToString(
                    result,
                    CultureInfo.InvariantCulture)
            };
        }
        catch (Exception exception)
            when (exception is
                SyntaxErrorException
                or EvaluateException
                or DivideByZeroException)
        {
            return CreateError(
                expression,
                "The arithmetic expression is invalid.");
        }
    }

    private static bool IsAllowedExpression(
        string expression)
    {
        return expression.All(
            character =>
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

    private static object CreateError(
        string? expression,
        string error)
    {
        return new
        {
            success = false,
            expression,
            error
        };
    }
}