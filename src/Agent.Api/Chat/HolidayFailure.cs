using System.Text.Json;
using Agent.Api.Chat.Models;
using OpenAI.Chat;

namespace Agent.Api.Chat;

internal static class HolidayFailure
{
    internal static void Observe(AgentContext context, string name, string payload)
    {
        if (name != "get_jewish_holidays") return;
        try
        {
            using var json = JsonDocument.Parse(payload);
            var root = json.RootElement;
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True &&
                (!root.TryGetProperty("isError", out var error) || error.ValueKind != JsonValueKind.True)) return;
        }
        catch (JsonException) { }
        var question = string.Join(" ", context.Messages.OfType<UserChatMessage>().LastOrDefault()?.Content.Select(x => x.Text) ?? []);
        context.HolidayFailureMessage = question.Any(c => c >= '\u0590' && c <= '\u05ff')
            ? "לא ניתן לאמת כרגע את תאריך החג עקב תקלה בשירות. אפשר לנסות שוב כשהשירות יחזור לפעול."
            : "The holiday date could not be verified because the service is unavailable. Please try again when the service is restored.";
    }
}
