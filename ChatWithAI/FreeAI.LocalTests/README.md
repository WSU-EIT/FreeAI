# FreeAI.LocalTests

Console executable — the only genuinely unique project in the ChatWithAI solution.

`DataAccess.ChatWithAi.cs` implements a token-budgeted Azure OpenAI chat loop using raw `HttpClient` calls against the Azure OpenAI REST API (`/openai/deployments/{deployment}/chat/completions`). It counts prompt tokens with SharpToken (`GptEncoding`) before each request, enforces a `MaxContextTokens` budget minus `ReplyMaxTokens` to keep requests within the model's context window, and pretty-prints the JSON response to stdout. `Program.cs` loads connection settings from `appsettings.json` and user secrets (`AzureOpenAi:Endpoint`, `Deployment`, `ApiKey`, `ApiVersion`, `MaxContextTokens`, `ReplyMaxTokens`, `TokenizerEncoding`, `Temperature`), constructs an `AzureOpenAiSettings` record, and calls `DataAccess.ChatWithAi()` with a hard-coded starter conversation asking for a minimal Blazor Hello World.

## Key public classes

| Class | Purpose |
|---|---|
| `DataAccess` | Static class; owns `ChatWithAi()` and all supporting token-counting helpers |
| `DataAccess.AzureOpenAiSettings` | Strongly-typed settings record (endpoint, key, deployment, token budget, tokenizer) |
| `DataAccess.Msg` | Lightweight `role`/`content` chat message model |

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `SharpToken` | GPT tokenizer (BPE encoding) for prompt token counting |
| `Microsoft.Extensions.Configuration.UserSecrets` | Loads API keys from .NET user secrets store |

## Build info

| Field | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net9.0` |
| Output type | `Exe` |

Part of the ChatWithAI solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A console app that demonstrates one thing well: a **token-safe Azure OpenAI chat call**. `Program.cs` loads settings from .NET user secrets; `DataAccess.ChatWithAi()` counts the conversation's tokens with SharpToken, computes a budget so the reply still fits, POSTs the messages to the Azure OpenAI chat endpoint with the `api-key` header, and pretty-prints the reply.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Azure OpenAI REST API | The chat round-trip | [DataAccess.ChatWithAi.cs#L110-L123](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.LocalTests/DataAccess.ChatWithAi.cs#L110-L123) |
| SharpToken (`GptEncoding`) | Token counting before send | [DataAccess.ChatWithAi.cs#L77-L90](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.LocalTests/DataAccess.ChatWithAi.cs#L77-L90) |
| `AzureOpenAiSettings` record | Endpoint, key, deployment, token budget | [DataAccess.ChatWithAi.cs#L28-L43](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.LocalTests/DataAccess.ChatWithAi.cs#L28-L43) |
| .NET user secrets | Keep the API key out of the repo | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.LocalTests/Program.cs) |

**Why does this exist?**
As a clean, copyable reference for *"call Azure OpenAI correctly from C#"* — the one project in this solution with genuinely unique logic, deliberately kept small enough to read in one sitting.

**What does it accomplish that other tools don't?**
- **Counts tokens before sending** (most samples don't), guaranteeing the prompt leaves room for the reply.
- **A guardrail**: if the reply cap is mis-set larger than the whole context, it auto-shrinks to a quarter of the window instead of failing.
- **Raw REST** with `HttpClient` — no SDK to learn or version-pin; the exact request is visible.

**Terminology & "can I see it?"**
- **Token** — the unit an LLM bills/thinks in (~¾ word).
- **Per-message overhead** — a small fixed token cost (6 here) added per message to approximate the API's own counting.
- **`max_tokens`** — the cap on the *reply* length, sent in the request body.

**The hard part, drawn** — budget first, then send:

```
  Program.cs ── load settings (user secrets) ──▶ AzureOpenAiSettings
        │
        ▼  DataAccess.ChatWithAi(history, settings)
  SharpToken counts tokens ─▶ budget = MaxContextTokens − ReplyMaxTokens
        (guardrail: ReplyMaxTokens ≥ context? → context ÷ 4)
        │
        ▼  POST /openai/deployments/{deployment}/chat/completions   (api-key, max_tokens=ReplyMax)
  Azure OpenAI ──▶ JSON reply ──▶ pretty-printed to stdout
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
