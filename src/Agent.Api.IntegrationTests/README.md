# RAG acceptance tests

These tests use the production AddRagServices registration and real OpenAI embeddings,
PostgreSQL keyword search, and Qdrant. Each test gets its own DI scope.
The suite searches existing data; it does not seed or delete records.

## Prerequisites

- .NET 9 SDK.
- PostgreSQL with the application's schema and indexed keyword chunks.
- Qdrant with the matching document chunks and embedding model/vector dimensions.
- The previously calibrated vacation document stating 19 vacation days, indexed
  in both stores. Use the same corpus as calibration: other documents may change
  keyword matches and the CEO query's expected empty result.
- OpenAI:ApiKey in MCP user secrets/configuration, or OPENAI_API_KEY / OpenAI__ApiKey.

Settings are copied from Mcp.Tools.Server/appsettings.json. Environment variables
override settings, using double underscores for nesting, for example:
ConnectionStrings__AgentDatabase, Qdrant__Host, Qdrant__GrpcPort,
Qdrant__CollectionName, OpenAI__EmbeddingModel.
The fixture loads MCP JSON settings, MCP user secrets, then environment overrides. OPENAI_API_KEY takes final precedence; OpenAI__ApiKey is also supported.
Keep Rag:MinimumVectorScore at the calibrated 0.45.

## Run from the solution directory

Unit tests (no external services):
    dotnet test src/Agent.Api.Tests/Agent.Api.Tests.csproj

Acceptance tests (requires the prerequisites above):
    dotnet test src/Agent.Api.IntegrationTests/Agent.Api.IntegrationTests.csproj

Running dotnet test on the whole solution also runs acceptance tests.
Missing credentials or unavailable services fail the tests; they are not silently
skipped. Embedding requests use the configured OpenAI account.

## Assertions

- Direct vacation query: raw/relevant/merged results and content containing 19.
- Semantic paraphrase: vector results, zero keyword results, content containing 19.
- Unrelated CEO phone query: raw vectors exist, but no relevant vectors, keywords,
  merged results, or matches.
- All three assert the configured minimum score is 0.45.

## Expanded vacation query set

The suite now contains 10 cases: 3 direct vacation queries, the original calibrated
semantic query, 3 additional paraphrases, and 3 CEO phone queries.
New paraphrases require relevant vector retrieval and a standalone 19 in content,
but do not assume zero keyword matches. The CEO queries retain the no-results
expectation from the original calibrated case. New cases are evaluation targets:
run them against the same indexed vacation corpus before treating them as passing.
A failure can expose a retrieval limitation; do not change 0.45 just to pass it.

## Chat answer end-to-end tests (6 cases)

These call the running Agent API over HTTP, exercising real orchestration, MCP,
retrieval, model generation, and conversation persistence. Both /api/chat and
/api/chat/stream are covered. They do not launch servers or use mocks.

Start Agent.Api and Mcp.Tools.Server with their normal credentials and databases.
Use a test database/corpus containing the calibrated vacation document.
Set AGENT_E2E_BASE_URL to the Agent API origin, e.g. http://localhost:5000
(use your actual port), and RAG_E2E_DOCUMENT_NAME to the exact indexed vacation
document name returned in Sources. No OpenAI key is needed in the test process
for these HTTP tests; the running services need their configured credentials.

Run only chat E2E tests:
    dotnet test src/Agent.Api.IntegrationTests/Agent.Api.IntegrationTests.csproj --filter "Category=EndToEnd"

Run only direct RAG acceptance tests:
    dotnet test src/Agent.Api.IntegrationTests/Agent.Api.IntegrationTests.csproj --filter "Category=Integration"

Running this project without a filter runs both suites and requires both sets of
environment variables. Each E2E case creates a new conversation; records remain
in the configured database. Requests consume model/embedding usage.

The answer checks accept "19 days" or "nineteen days", require search_knowledge
and the named document source, and reject calculator use. The CEO case requires
an explicit missing-information phrase, empty sources, and no phone-like digit
sequence or email address. These are automated English response checks, not a
proof that every sentence is grounded; unexpected wording may need review.
SSE checks also require a conversation event, completion, content, and no error.

### Saved local settings
testsettings.json now supplies http://localhost:5001 and
Employee_Handbook_Sample.pdf. Environment variables can override either value.
The HTTP helper appends /api/chat or /api/chat/stream.
Visual Studio can read the saved settings without environment variables.
RAG tests also read the existing Mcp.Tools.Server user secrets.

## Conversation tests (4 additional EndToEnd cases)

Both JSON and SSE now test:
- Vacation question, then "And when does that entitlement begin?", then CEO phone.
- A unique marker remembered within one conversation, absent from a new one.
- Persisted history contains exactly the expected user/assistant turns in order.

The one-year eligibility expectation was verified against indexed handbook chunk 0.
Every scenario uses fresh conversation IDs. Test conversations remain in the database.

Run these four cases:
    dotnet test src/Agent.Api.IntegrationTests/Agent.Api.IntegrationTests.csproj --filter "FullyQualifiedName~Conversation_"

The project now contains 20 cases: 10 RAG and 10 EndToEnd.

## Two-document regression suite (10 additional cases)

Both JSON and SSE are checked for five independent questions:
meals (45 USD per person per day), submission (14 calendar days after the trip),
payment (10 business days after finance approval), undefined mileage rate,
and vacation (19 days after one year).

Both Employee_Handbook_Sample.pdf and Business_Travel_Reimbursement_Test.txt
must be indexed. Override the names with RAG_E2E_DOCUMENT_NAME and
RAG_E2E_TRAVEL_DOCUMENT_NAME if necessary.

Every case requires search_knowledge and a source in the appropriate document.
The test also fetches the cited chunks and checks that they contain the expected
facts. Extra retrieved sources are allowed. The mileage case allows a citation
to the explicit missing-information clause, but rejects numeric/currency answers.
These English text-pattern checks are regression checks, not full semantic grading.

Run just this suite:
    dotnet test src/Agent.Api.IntegrationTests/Agent.Api.IntegrationTests.csproj --filter "Scenario=MultiDocument"

This adds 10 cases, bringing the project to 30 integration cases (10 direct RAG,
20 HTTP EndToEnd). Fresh conversation IDs are used; test conversations remain
in the database and real model/embedding calls incur normal usage.

## Document lifecycle (1 additional EndToEnd case)

Run only the lifecycle case:
    dotnet test src/Agent.Api.IntegrationTests/Agent.Api.IntegrationTests.csproj --filter "Scenario=DocumentLifecycle"

This case uploads a uniquely named temporary TXT policy, uploads identical content
again, deletes it, and uploads new content under the same file name. It verifies:
- One document ID, one persisted chunk and one Qdrant point after duplicate upload.
- Document/chunk absence through PostgreSQL-backed API endpoints and zero Qdrant
  points after deletion.
- New content and a new ID after explicit delete-and-upload replacement.
- Fresh chat answers cite the correct version, and the old value disappears.
It does not assert that uploading changed content automatically replaces a file.

Requires the running API/MCP/embedding services and Qdrant REST access.
RAG_E2E_QDRANT_URL defaults to http://localhost:6333 and
RAG_E2E_QDRANT_COLLECTION to agent_documents in testsettings.json.
Both must refer to the same Qdrant collection used by the running MCP.
The current local test assumes Qdrant REST does not require authentication.

Only documents with this run's unique Lifecycle_<guid>.txt name are deleted.
Cleanup runs in finally; a terminated test process can leave a temporary document.
Chat records remain. Do not run against production. This suite now has 31 cases.

## Performance baseline

Run the latency guardrail with:
    dotnet test src/Agent.Api.IntegrationTests/Agent.Api.IntegrationTests.csproj --filter "Scenario=Performance"

It measures one normal JSON response and one SSE response for the vacation query,
prints both elapsed times, and fails only when either request exceeds 120 seconds.
This is an operational stuck-request check, not a hardware benchmark.
