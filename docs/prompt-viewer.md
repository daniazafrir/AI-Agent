# Prompt Viewer (Sprint 3)

The debug panel opens a viewer for the current request's model-input snapshots.
Select a round to inspect ordered SDK-serialized messages, tool calls, tool
results (including the actual normalized RAG context), tool schemas, tool
choice, model name, and capture time. The final answer remains in the chat.

Snapshots are taken at the completion-service call boundary. They describe
prepared inputs, not HTTP headers or proof that OpenAI received the request.
They include full prompt/document content; transport credentials and API keys
are not captured. This is a developer feature, not a public diagnostics endpoint.

## Enable

Development enables the viewer by default. Other environments disable it.
Set `OpenAI__EnablePromptViewer=true` to explicitly enable it in a trusted
development deployment, or `false` to disable it in any environment.
Restart Agent.Api after changing this setting.

## Lifetime

Streaming sends one `prompt` event per round. Non-streaming responses return
`prompts`. Disabled mode emits no prompt events and returns an empty list.
The UI keeps snapshots during the current request, including after a stop or
error, until another request starts, a saved conversation is opened, a new
conversation is created, or the page refreshes. No snapshots are persisted.
Historical replay is still part of Sprint 7.

## Verify

1. Start Agent.Api in Development and open the Angular app.
2. Ask a general question: open Prompt Viewer and inspect the system/user roles.
3. Ask about vacation days: inspect the next round's assistant tool call and
   matching tool result, containing the text actually passed to the model.
4. Check the debug panel's query, tool, ranked chunks, engines and RRF scores.
5. Start another request or open a saved conversation: stale snapshots clear.
6. Set `OpenAI__EnablePromptViewer=false` and restart: no snapshots are exposed.

Unit tests cover immutable SDK snapshots, call/result linkage, round ordering,
and enabled/disabled behavior in both streaming and non-streaming paths.
