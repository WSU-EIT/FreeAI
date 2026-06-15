# ChatWithAI

Multi-project .NET 9 solution that pairs the FreeCRM scaffold (renamed to the FreeAI namespace) with a standalone Azure OpenAI chat console. Seven projects are included:

| Project | Type | Role |
|---|---|---|
| `FreeAI` | ASP.NET Core Web (Blazor United) | Server host: authentication, REST API, SignalR hub, plugin loader |
| `FreeAI.Client` | Blazor WebAssembly | WASM UI: all admin and user-facing pages |
| `FreeAI.DataAccess` | Class library | Server-side data access over EF Core; auth, users, tenants, files, PDF, CSV, Graph API |
| `FreeAI.DataObjects` | Class library | Shared DTOs, caching helper, and startup-state flags |
| `FreeAI.EFModels` | Class library | EF Core `DbContext` and entity models for all core tables |
| `FreeAI.Plugins` | Class library | Roslyn-based runtime that loads, compiles, and executes `.cs`/`.plugin` files |
| `FreeAI.LocalTests` | Console `Exe` | Token-budgeted Azure OpenAI chat loop — the only project with unique AI logic |

## FreeAI.LocalTests — the unique project

`DataAccess.ChatWithAi()` demonstrates a token-budgeted chat round-trip against the Azure OpenAI REST API:

1. Loads `AzureOpenAiSettings` from `appsettings.json` and .NET user secrets (`AzureOpenAi:Endpoint`, `Deployment`, `ApiKey`, `ApiVersion`, `MaxContextTokens`, `ReplyMaxTokens`, `TokenizerEncoding`, `Temperature`).
2. Creates a `GptEncoding` (SharpToken BPE tokenizer, default `o200k_base`) and counts tokens across all messages before sending — using a 6-token-per-message overhead buffer to approximate the API's own counting.
3. Computes `promptBudget = MaxContextTokens − ReplyMaxTokens` and validates that `replyMaxTokens` is less than `maxContextTokens`, falling back to `maxContextTokens / 4` if not.
4. Posts the conversation history to `/openai/deployments/{deployment}/chat/completions?api-version={version}` using a raw `HttpClient` with the `api-key` header.
5. Pretty-prints the JSON response to stdout.

The hard-coded starter conversation asks the model (identified as a C# Blazor expert) for a minimal Hello World `Program.cs`.

## The six scaffold projects

The remaining six projects are the FreeCRM framework renamed to the `FreeAI` namespace. No app-specific logic has been added; the extension points (`Program.App.cs`, `DataAccess.App.cs`, `DataController.App.cs`, `Index.App.razor`, etc.) are present but empty stubs.

- **FreeAI** — ASP.NET Core host. Wires cookie/OAuth authentication (Google, Microsoft, Apple, Facebook, OpenId), loads plugins from a `Plugins/` folder via Roslyn, maps a SignalR hub at `/freeaiHub` (with optional Azure SignalR Service fallback), registers `IDataAccess`, `IConfigurationHelper`, and `ICustomAuthentication` in DI, and registers QuestPDF Community license.
- **FreeAI.Client** — Blazor WASM app. Full multi-tenant UI covering login/logout, user profile, settings administration (tenants, users, groups, departments, files, languages, UDF labels), plugin testing, and a home page with a per-tenant logo stub.
- **FreeAI.DataAccess** — Connects to SQL Server, MySQL, PostgreSQL, SQLite, or in-memory via EF Core. Runs custom SQL migration scripts on startup. Covers local/LDAP/AD/OAuth authentication, file storage, encryption, JWT helpers, PDF (QuestPDF), CSV, NuGet feed queries, and Microsoft Graph API calls.
- **FreeAI.DataObjects** — All request/response DTOs shared between client and server. Includes `GlobalSettings` (static startup flags) and `Caching` (in-process `MemoryCache` wrapper).
- **FreeAI.EFModels** — `EFDataModel` DbContext targeting all four providers. Tables: `Tenants`, `Users`, `UserGroups`, `UserInGroups`, `Departments`, `DepartmentGroups`, `FileStorages`, `Settings`, `UDFLabels`, `PluginCaches`.
- **FreeAI.Plugins** — Loads `.cs`/`.plugin` source files at startup, compiles each via Roslyn, extracts metadata via `Properties()`, and exposes `ExecuteDynamicCSharpCode<T>`. Plugins with `ContainsSensitiveData = true` are AES-encrypted before being sent to the browser.

## Solution structure

```
ChatWithAI/
├── FreeAI/                  # Server host (Microsoft.NET.Sdk.Web, net9.0)
├── FreeAI.Client/           # WASM client (Microsoft.NET.Sdk.BlazorWebAssembly, net9.0)
├── FreeAI.DataAccess/       # Data access layer (Microsoft.NET.Sdk, net9.0)
├── FreeAI.DataObjects/      # Shared DTOs (Microsoft.NET.Sdk, net9.0)
├── FreeAI.EFModels/         # EF Core models (Microsoft.NET.Sdk, net9.0)
├── FreeAI.LocalTests/       # Azure OpenAI console (Microsoft.NET.Sdk, net9.0, Exe)
└── FreeAI.Plugins/          # Plugin runtime (Microsoft.NET.Sdk, net9.0)
```

## Prerequisites

- .NET 9 SDK
- SQL Server, MySQL, PostgreSQL, or SQLite (or use the in-memory provider for development)
- For `FreeAI.LocalTests`: an Azure OpenAI resource with a chat completions deployment; store secrets via `dotnet user-secrets set "AzureOpenAi:ApiKey" "<key>"`

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
ChatWithAI is two things bolted together: (1) the **FreeCRM platform scaffold** — a ready-made multi-tenant web app (login, users, tenants, files, plugins, real-time updates) renamed into the `FreeAI` namespace — and (2) **`FreeAI.LocalTests`**, the only piece with real AI logic: a *token-budgeted* chat round-trip against Azure OpenAI. "Token-budgeted" means it counts how many **tokens** (the chunks an AI model bills and thinks in) your conversation will cost *before* sending, and reserves room for the reply so the request never overflows the model's context window. The six scaffold projects' app-specific hooks are present but empty — so this solution is a *starting point*: a production-grade platform plus a proven, safe AI call, ready to be joined together.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Azure OpenAI REST API (raw `HttpClient`) | The actual chat call | [DataAccess.ChatWithAi.cs#L110-L123](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.LocalTests/DataAccess.ChatWithAi.cs#L110-L123) |
| SharpToken (GPT tokenizer) | Count tokens *before* sending | [DataAccess.ChatWithAi.cs#L77-L90](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.LocalTests/DataAccess.ChatWithAi.cs#L77-L90) |
| Token budgeting | Reserve reply room; never exceed context | [DataAccess.ChatWithAi.cs#L56-L64](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.LocalTests/DataAccess.ChatWithAi.cs#L56-L64) |
| Blazor United (Server + WASM) | The platform UI/host | [FreeAI/Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI/Program.cs) |
| Roslyn plugin runtime | Compile drop-in C# at runtime | [FreeAI.Plugins/Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.Plugins/Plugins.cs) |
| EF Core (5 providers) | Data layer | [EFModels/EFDataModel.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.EFModels/EFModels/EFDataModel.cs) |

**Why does this exist?**
To pair WSU's reusable app platform with a *minimal, correct* example of talking to Azure OpenAI safely — so a team can start from a working multi-tenant app **and** a token-safe AI call, instead of wiring both from scratch.

**What does it accomplish that other tools don't?**
- **Token budgeting up front.** Most quick-start samples just POST the messages and hope. This counts tokens with the model's own tokenizer family and guarantees prompt + reply fit the window, with a fallback if the reply cap is set too high.
- **Raw REST, no SDK lock-in.** It calls the Azure OpenAI endpoint directly with `HttpClient`, so you can see (and change) exactly what's sent.
- **A real platform underneath.** The AI demo sits on a full multi-tenant scaffold (auth, plugins, 5 databases), not a toy.

**Terminology & "can I see it?"**
- **Token** — the unit an LLM reads and bills in (roughly ¾ of a word).
- **Context window** — the most tokens a model can consider at once (`MaxContextTokens`, default 128,000).
- **Tokenizer / BPE** — the algorithm (SharpToken's `o200k_base`) that splits text into tokens so they can be counted.
- **Deployment** — your named instance of a model inside Azure OpenAI.
- *See it:* the entire chat logic is one readable file — [DataAccess.ChatWithAi.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.LocalTests/DataAccess.ChatWithAi.cs).

**The hard part, drawn** — making a conversation fit the model's window before sending:

```
  you ─▶ Program.cs loads settings (endpoint/key/deployment + token budget) from user secrets
              │
              ▼  build conversation:  [system] [user] [assistant] [user]
  ┌──────────────────── THE HARD PART: fit the context window ─────────────────────┐
  │ SharpToken counts the tokens of every message  (+6 overhead per message)        │
  │ promptBudget = MaxContextTokens − ReplyMaxTokens                                │
  │ guardrail: if ReplyMaxTokens ≥ MaxContextTokens → ReplyMaxTokens = context ÷ 4   │
  └───────────────────────────────────┬────────────────────────────────────────────┘
              │ now the prompt is guaranteed to leave room for the reply
              ▼
   POST /openai/deployments/{deployment}/chat/completions?api-version=…
         header: api-key    body: { messages, temperature, max_tokens = ReplyMaxTokens }
              │
              ▼   Azure OpenAI
         JSON reply ──▶ pretty-printed to the console
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
