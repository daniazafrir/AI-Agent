# Search performance

The existing HybridSearchService stopwatches now travel through
HybridSearchResult, RagSearchResult, search_knowledge JSON and ChatDebugInfo.
The normal response and streaming completion share the same debug extraction.
Angular normalizes camelCase and PascalCase fields.

Four nullable millisecond measurements are shown as independent bars scaled
to the longest stage: embeddingTimeMs, vectorSearchTimeMs,
keywordSearchTimeMs and rankingTimeMs. Null means not supplied (for example
an older MCP server); zero means the measured duration rounded down to zero.

Vector and keyword search run concurrently. Their durations must not be summed
to infer total elapsed time. SearchTimeMs remains the hybrid search wall time,
including filtering and logging overhead, but excludes later RagService work,
network transport and model generation. All values describe the LAST search
call; a Hebrew-to-English retry replaces the preceding call's debug values.

## Acceptance

Rebuild/restart MCP and Agent.Api, then refresh Angular. Ask a new knowledge
question. RAG Debug should show four stage durations and the total hybrid
search time. Compare these to the MCP log for the same query. Ranking can
legitimately show 0 ms. An older payload must show an em dash for missing
stages, never a fabricated zero. General questions without retrieval should
not display timings from an earlier request.

Unit coverage includes MCP timing serialization (including null and zero)
and extraction into streaming completion debug. Angular template compilation
is checked separately; live acceptance is still required.
