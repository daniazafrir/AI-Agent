namespace Agent.Api.Tools;

public sealed class KnowledgeSearchUnavailableException : HttpRequestException
{
    public const string UserMessage =
        "The knowledge search is temporarily unavailable. Please try again later.";

    public KnowledgeSearchUnavailableException(Exception? inner = null)
        : base(UserMessage, inner) { }
}
