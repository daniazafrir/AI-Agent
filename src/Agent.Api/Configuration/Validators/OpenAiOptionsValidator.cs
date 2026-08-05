using Microsoft.Extensions.Options;

namespace Agent.Api.Configuration.Validators;

public sealed class OpenAiOptionsValidator
    : IValidateOptions<OpenAiOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        OpenAiOptions options)
    {
        var apiKey =
            options.ApiKey ??
            Environment.GetEnvironmentVariable(
                "OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return ValidateOptionsResult.Fail(
                "OpenAI API key is missing.");
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            return ValidateOptionsResult.Fail(
                "OpenAI model is missing.");
        }

        return ValidateOptionsResult.Success;
    }
}