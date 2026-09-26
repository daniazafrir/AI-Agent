# Sprint tracking

Based on the earlier Debug / Prompt Viewer roadmap. Other versions of the
conversation used different sprint numbering; this file tracks that roadmap
by feature. Updated 2026-09-24.

| Sprint | Scope | Status / remaining work |
| --- | --- | --- |
| 1 | Backend debug data | Search counters/query/tool/scores are available; full per-stage performance payload belongs to sprint 5. |
| 2 | Angular debug panel | Counters, query, tool, ranked chunks, search engines and RRF scores implemented. |
| 3 | Prompt Viewer | Current-request snapshots by model round implemented; user confirmed browser trace showing Hebrew search, English retry and source context, and reported passing tests. Local production build remains blocked by spawn EPERM. See prompt-viewer.md. |
| 4 | Chunk Viewer | Added literal highlighting/search, automatic numeric search from each answer, match count, source metadata, responsive layout, loading/error/retry and subscription cleanup. App template compilation and seven direct highlighting checks passed; browser acceptance and Angular test runner remain pending (spawn EPERM). See chunk-viewer.md. |
| 5 | Performance | Embedding/vector/keyword/ranking durations now flow from MCP to API debug and Angular bars for the latest search. Missing timings stay null; parallel searches are explicitly labeled. Backend tests and Angular compilation checked; live browser acceptance pending after restarting MCP and API. Request timing history already exists. |
| 6 | RAG Analytics | Returned versus context-included entries now tracked at tool-message construction, with omission reasons and character counts. Vector threshold exclusions shown as a count only. Backend tests and Angular compilation passed; live acceptance pending. See rag-analytics.md. |
| 7 | History and Replay | Versioned per-answer trace persistence and read-only replay implemented, including completed tools, context, debug, sources, conditional prompts and server timings. Migration and live acceptance pending. Partial/failure limitations documented in history-replay.md. |
| 8 | Knowledge administration | Upload/list/delete exist; re-index and indexing configuration metadata remain. |

Supporting work completed separately: weather tool, weather follow-ups,
acceptance tests, integration preflight runner, CI unit tests and Angular build.
These additions do not close the outstanding roadmap items.
