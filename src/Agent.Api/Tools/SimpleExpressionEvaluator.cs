using System.Globalization;

namespace Agent.Api.Tools;

internal sealed class SimpleExpressionEvaluator
{
    private string _expression = string.Empty;
    private int _position;

    public decimal Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ArgumentException("Expression is required.", nameof(expression));
        }

        if (expression.Length > 200)
        {
            throw new ArgumentException("Expression is too long.", nameof(expression));
        }

        _expression = expression;
        _position = 0;

        var value = ParseExpression();
        SkipWhitespace();

        if (_position != _expression.Length)
        {
            throw new FormatException($"Unexpected character at position {_position}.");
        }

        return value;
    }

    private decimal ParseExpression()
    {
        var value = ParseTerm();

        while (true)
        {
            SkipWhitespace();
            if (Match('+'))
            {
                value += ParseTerm();
            }
            else if (Match('-'))
            {
                value -= ParseTerm();
            }
            else
            {
                return value;
            }
        }
    }

    private decimal ParseTerm()
    {
        var value = ParseFactor();

        while (true)
        {
            SkipWhitespace();
            if (Match('*'))
            {
                value *= ParseFactor();
            }
            else if (Match('/'))
            {
                var divisor = ParseFactor();
                if (divisor == 0)
                {
                    throw new DivideByZeroException("Cannot divide by zero.");
                }

                value /= divisor;
            }
            else
            {
                return value;
            }
        }
    }

    private decimal ParseFactor()
    {
        SkipWhitespace();

        if (Match('+'))
        {
            return ParseFactor();
        }

        if (Match('-'))
        {
            return -ParseFactor();
        }

        if (Match('('))
        {
            var value = ParseExpression();
            SkipWhitespace();
            if (!Match(')'))
            {
                throw new FormatException("Missing closing parenthesis.");
            }

            return value;
        }

        return ParseNumber();
    }

    private decimal ParseNumber()
    {
        SkipWhitespace();
        var start = _position;
        var hasDecimalSeparator = false;

        while (_position < _expression.Length)
        {
            var current = _expression[_position];
            if (char.IsDigit(current))
            {
                _position++;
                continue;
            }

            if (current == '.' && !hasDecimalSeparator)
            {
                hasDecimalSeparator = true;
                _position++;
                continue;
            }

            break;
        }

        if (start == _position)
        {
            throw new FormatException($"A number was expected at position {_position}.");
        }

        var token = _expression[start.._position];
        return decimal.Parse(token, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private void SkipWhitespace()
    {
        while (_position < _expression.Length && char.IsWhiteSpace(_expression[_position]))
        {
            _position++;
        }
    }

    private bool Match(char expected)
    {
        if (_position >= _expression.Length || _expression[_position] != expected)
        {
            return false;
        }

        _position++;
        return true;
    }
}
