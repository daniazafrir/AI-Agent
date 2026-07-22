using Mcp.Tools.Server.Features.Rag;
using System.Collections.Concurrent;

namespace Mcp.Tools.Server.Features.Rag;

public sealed class InMemoryRagDocumentStore : IRagDocumentStore
{
    private readonly ConcurrentDictionary<Guid, DocumentInfo> _documents = new();

    public Task SaveAsync(
        DocumentInfo document,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _documents[document.Id] = document;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DocumentInfo>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<DocumentInfo> result = _documents.Values
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<bool> DeleteAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_documents.TryRemove(documentId, out _));
    }
}
