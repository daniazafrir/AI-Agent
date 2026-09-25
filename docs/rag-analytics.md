# RAG Analytics

KnowledgeContextBuilder records each returned match while formatting the actual
tool message. ToolExecutionResult carries the analytics to ChatDebugInfo in both
normal and streaming execution. Text formatting and the raw-JSON fallback remain
unchanged.

The panel shows the last search call only, not all calls in the request:

- Returned match entries, entries included in context, and omitted entries.
- Document identity, chunk index, content character count and inclusion reason.
- Full tool-message length in UTF-16 characters, including source labels or JSON;
  this is not a token count or the size of the entire conversation.
- Vector candidates removed by the relevance threshold (raw minus relevant).
  Identities of those candidates are not available. This does not measure later
  top-K selection, ranking exclusions or deduplication.

Nonempty content is included without extra truncation or token-budget filtering.
Empty content is omitted when other nonempty chunks exist. If all content is
empty, the existing raw JSON fallback contains the returned entries; these are
marked `raw_payload_fallback`, not falsely reported as omitted. No matches means
zero entries, although the tool's JSON status still occupies characters.

Inclusion describes the constructed tool context. It is not proof of provider
receipt or of which evidence the model used in its answer. No analytics are
persisted for historical conversations yet; that belongs to replay work.

## Validation

120 backend unit tests passed, including mixed content, empty fallback, zero
matches, PascalCase fields and duplicate entries. Angular template compilation
passed. Live acceptance remains pending:

1. Rebuild/restart Agent.Api and refresh Angular.
2. Ask a vacation question and inspect RAG Analytics under RAG Debug.
3. Compare included entries with the last round's tool message in Prompt Viewer.
4. Ask an unknown policy question: returned/included counts should be zero if
   the search has no matches. A general question should clear previous debug.
