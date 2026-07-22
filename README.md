# Agent Personal Assistant — Step 03: Tool Calling

This step turns the chat application into a basic agent. The model can decide to call local tools, the API executes each requested tool, sends the result back to the model, and repeats until a final answer is produced.

## Included tools

- `get_current_time` — current time in an IANA time zone.
- `calculate` — safe arithmetic parser supporting `+`, `-`, `*`, `/`, and parentheses.
- `search_knowledge` — mock RAG search. It deliberately reports that no documents are indexed yet.
- `create_calendar_event_preview` — creates a preview only; no calendar data is saved.

## Flow

```text
User -> Agent.Api -> OpenAI
                    |
                    +-> tool call requested
                        |
                        +-> local ToolExecutor
                            |
                            +-> result returned to OpenAI
                                |
                                +-> final answer
```

The API limits execution to five tool-calling rounds to prevent an accidental infinite loop.

## Requirements

- .NET 8 SDK
- OpenAI API key

## Configure the API key

From `src/Agent.Api`:

```bash
dotnet user-secrets set "OpenAI:ApiKey" "YOUR_KEY"
```

Alternatively, set the environment variable:

```bash
OPENAI_API_KEY=YOUR_KEY
```

Do not commit a real API key to `appsettings.json`.

## Run

```bash
dotnet restore
dotnet run --project src/Agent.Api
```

Open Swagger at the URL printed by ASP.NET, usually:

```text
http://localhost:5000/swagger
```

## Request example

```json
{
  "message": "What is (542 * 83) / 2?",
  "conversationId": null
}
```

Expected response shape:

```json
{
  "conversationId": "...",
  "answer": "...",
  "usedTools": ["calculate"],
  "createdAt": "..."
}
```

Other prompts to test:

```text
What time is it in Asia/Jerusalem?
Search our internal documents for the vacation policy.
Prepare a 45-minute calendar-event preview for tomorrow at 10:00 titled Architecture review.
```

## Important limitation

Step 03 has no chat-history persistence, real RAG, MCP, or Google Calendar integration. The knowledge and calendar tools are intentional placeholders that demonstrate tool calling safely.

## Next step

Step 04 will extract the tools into a separate MCP server and make `Agent.Api` an MCP client.
