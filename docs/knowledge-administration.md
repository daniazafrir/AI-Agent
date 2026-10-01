# Knowledge administration

The document list now exposes the existing POST /api/documents/{id}/reindex
endpoint and successful indexing metadata. Reindex uses the stored chunks in
index order, preserving their IDs/content in PostgreSQL. It does not re-extract
the original PDF or change chunk boundaries.

After success, MCP reports the embedding model bound to its client and vector
dimensions. API persists those with the completion time. New uploads also record
the actual chunking parameters (800 characters, 150 overlap, paragraphs-v1).
For legacy documents, old chunk size/overlap remain unknown even after reindex.
An older MCP server may report no model/dimensions; the UI displays 'לא תועד'.

The existing reindex operation deletes document vectors before indexing. It is
not atomic: search may temporarily miss the document, and failures can leave
partial/missing vectors. Persisted chunks survive, so retry can recover. A failed
operation does not update the last successful indexing metadata. UI prevents
overlapping delete/reindex clicks in this page; it is not a distributed lock.

## Upgrade and acceptance

From the repository root, before starting the updated services:

```powershell
dotnet ef database update --context Agent.Api.Infrastructure.Persistence.AgentDbContext --project src/Agent.Api --startup-project src/Agent.Api
```

Rebuild/restart MCP and Agent.Api, and refresh Angular.

1. Open a legacy document's indexing details: unknown metadata must be shown.
2. Click reindex. Check progress, disabled action buttons and completion message.
3. Refresh: model, dimensions and last indexing time should persist. Existing
   chunking parameters remain unknown for legacy documents.
4. Ask the vacation question and check sources/chunk text remain accessible.
5. A new upload should record chunking size/overlap. An unavailable MCP should
   produce an error without falsely updating the last successful timestamp.

Backend tests cover ordered persisted chunks, successful metadata persistence,
failure without metadata update, and retention of SQL chunks. Angular template
compilation passed. Database migration and live service acceptance are pending.
