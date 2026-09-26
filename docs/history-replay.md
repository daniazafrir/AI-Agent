# History and replay

Each newly stored assistant message can contain a versioned ConversationTrace
in the nullable ConversationMessages.TraceJson JSONB column. The answer and its
trace are inserted together. Conversation deletion also removes the trace.
Older messages have no trace and remain readable.

## Database upgrade

Before starting the updated API, apply the migration from the repository root:

```powershell
dotnet ef database update --project src/Agent.Api --startup-project src/Agent.Api
```

In Visual Studio Package Manager Console, select Agent.Api as the default project
and run `Update-Database` instead if using the installed EF tooling there.
The migration adds one nullable JSONB column; it does not backfill old traces.
Do not run the updated API against the old schema.

## Saved data

- Prompt snapshots only when EnablePromptViewer was enabled at execution time.
- Completed tool calls: arguments, exact normalized context text and search debug
  (including timing and analytics), in execution order.
- Final sources, used tool names and latest search debug.
- Server elapsed time and first text time, excluding history loading and saving.
- Completed/cancelled/failed status; interrupted output uses the existing marker.

If a tool errors or cancellation occurs before it returns, it has no completed
tool-call result. Its attempted name may still appear in used tools. These are
partial traces, not a full event log. Nonstreaming failures currently have no
saved answer/trace. Total server timing differs from browser network timing.

Trace data contains full tool context and, when enabled, system/user prompts.
It is retained with the conversation and served by the existing conversation
API access controls. Disabling prompt capture affects future snapshots only.
Trace metadata is not replayed into model conversation input.

## UI acceptance

1. After migration, rebuild/restart Agent.Api and refresh Angular.
2. Ask a new knowledge question; wait until it finishes.
3. Refresh the page, open that conversation, and click 'אבחון תשובה שמורה'
   underneath the answer.
4. Inspect tool arguments/results, sources, per-call debug, server durations and
   Prompt Viewer. This reads saved data and never calls the model/tools again.
5. Ask another question in the same conversation and verify each answer opens
   its own trace. Stop a new answer and verify its partial trace after reload.
6. Older conversations still display text, with no fabricated trace button.

Backend unit coverage exercises per-message trace storage/retrieval, legacy
messages, cancellation, deletion, and migration SQL generation. Angular template
compilation passed. Applying the migration and live refresh/replay acceptance
remain to be verified against the user's running PostgreSQL/API.
