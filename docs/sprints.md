# Sprint tracking

Updated 2026-10-04. Tracks the original Debug / Prompt Viewer roadmap by feature.
Manual evidence below comes from user-shared results and confirmations; this
update did not rerun the application or tests.

| Sprint | Scope | Status / evidence |
| --- | --- | --- |
| 1 | Backend debug data | Implemented. Shared traces show query, counters, chunk identity, search engine and corrected RRF match metadata. |
| 2 | Angular debug panel | Implemented. User shared rendered counters and search details. |
| 3 | Prompt Viewer | Implemented and manually exercised: Hebrew search, English retry and actual tool context visible. User production build succeeded. See prompt-viewer.md. |
| 4 | Chunk Viewer | Implemented. User compared selected chunks against tool context. Full keyboard/mobile/error-retry checklist and automated component tests are not explicitly confirmed. See chunk-viewer.md. |
| 5 | Performance | Implemented. User shared live embedding/vector/keyword/ranking bars and total search time. Parallel stage durations refer to the latest search. |
| 6 | RAG Analytics | Implemented and manually checked: two returned/included chunks, zero omissions, character counts and matching context shown in shared traces. See rag-analytics.md. |
| 7 | History and Replay | Implemented; saved-answer diagnostic traces demonstrated against the running database. Full reload/cancellation/legacy checklist is not individually confirmed. See history-replay.md. |
| 8 | Knowledge administration | Implemented; user demonstrated successful reindex metadata (model, 1536 dimensions, timestamp) and subsequent vacation retrieval. Legacy chunk parameters correctly remain unknown. New-upload and failure-metadata checks remain checklist items. See knowledge-administration.md. |

## Validation baseline

- Latest recorded backend unit run: 138 passed, including holiday failure tests.
- User reported tests/build working after the holiday fixes; no new detailed
  integration report was collected during this documentation update.
- Earlier production build succeeded with unused-import and bundle-budget
  warnings. Their resolution has not been verified here.
- Angular component tests exist, but a successful runner result is not recorded.
  Earlier sandbox runs encountered spawn EPERM; that does not establish a blocker
  on the user's development machine.

## Supporting work

Weather and follow-ups, integration preflight, CI checks, Hebrew knowledge
fallback, and Jewish holiday lookup with current-date context were added.
Holiday year/region follow-ups, including quoted questions, were manually
verified. Holiday failure handling prevents a model-generated date after tool
failure. User reported an unavailable-server response and successful recovery,
and confirmed commits.

## Next task: finish frontend validation

1. Run `npm test -- --watch=false` from `src/agent-ui` and record the outcome.
   Attempted on 2026-10-04: blocked before test execution by esbuild spawn EPERM.
   Test TypeScript compilation passed. Added the missing jsdom dependency and
   HTTP testing providers/initial list responses to four screen component tests.
2. CI now runs Angular tests before the production build. Its first successful
   run is still pending; fix any runtime failures exposed by that run.
3. Complete the focused manual checks still listed in sprints 4, 7 and 8.

All eight feature areas are implemented; remaining validation does not require
reimplementation. Choose further product work after these checks. Non-atomic
reindexing remains a documented limitation, not a completed fix.
