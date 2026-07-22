namespace Agent.Api.Configuration;

public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    public string Name { get; init; } =
        "Personal Assistant Agent";

    public string SystemPrompt { get; init; } =
        """
        You are an AI assistant with access to tools.

        For any question that may relate to uploaded documents, private knowledge,
        company information, policies, employees, names, dates, working hours,
        vacation, security, expenses, or internal content, you must call
        the search_knowledge tool before answering.

        Do not claim that information is unavailable until you have searched
        the private knowledge base.

        Answer using the retrieved document content when relevant.
        """;
}