using Mcp.Tools.Server.Features.Rag;
using System.Collections.Concurrent;

namespace Mcp.Tools.Server.Features.Rag;

public sealed class InMemoryRagStore : IRagStore
{
    private readonly ConcurrentDictionary<Guid,DocumentInfo> _documents = new();
    private readonly ConcurrentDictionary<Guid,IReadOnlyList<DocumentChunk>> _chunks = new();

    public Task SaveAsync(DocumentInfo document,IReadOnlyList<DocumentChunk> chunks,CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _documents[document.Id]=document;
        _chunks[document.Id]=chunks;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DocumentInfo>> ListDocumentsAsync(CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<DocumentInfo> result=_documents.Values.OrderByDescending(x=>x.CreatedAtUtc).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<DocumentChunk>> GetAllChunksAsync(CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<DocumentChunk> result=_chunks.Values.SelectMany(x=>x).ToList();
        return Task.FromResult(result);
    }

    public Task<bool> DeleteDocumentAsync(Guid documentId,CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _chunks.TryRemove(documentId,out _);
        return Task.FromResult(_documents.TryRemove(documentId,out _));
    }
}
