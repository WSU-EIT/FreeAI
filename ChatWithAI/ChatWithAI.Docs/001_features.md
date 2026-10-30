# ChatWithAI -- Feature Inventory

> **Category:** Features
> **Purpose:** Complete catalog of what this project can do today.

---

## Core features

### Azure OpenAI integration (ChatWithAI.LocalTests)
- [x] Token-budgeted chat completions via Azure OpenAI REST API
- [x] SharpToken BPE tokenizer (o200k_base) for accurate token counting
- [x] Per-message 6-token overhead buffer to match API counting
- [x] `promptBudget = MaxContextTokens - ReplyMaxTokens` enforcement
- [x] Raw `HttpClient` POST to `/openai/deployments/{deployment}/chat/completions`
- [x] Configurable via appsettings + .NET user secrets

### FreeCRM scaffold (ChatWithAI.*)
- [x] Multi-tenant authentication (cookie, Google, Microsoft, Apple, Facebook, OpenID Connect)
- [x] User management (create, edit, deactivate, group membership)
- [x] Department and tenant administration
- [x] File storage (upload, download, delete)
- [x] Unlimited data fields (UDF labels)
- [x] Settings administration
- [x] Roslyn dynamic plugin system (load/compile/execute `.cs` and `.plugin` files at runtime)
- [x] SignalR real-time hub (`/chatwithaiHub`, optional Azure SignalR fallback)
- [x] QuestPDF report generation
- [x] Microsoft Graph API integration
- [x] LDAP/Active Directory authentication
- [x] EF Core multi-database (SQL Server, MySQL, PostgreSQL, SQLite, InMemory)

## Configuration

| Setting | Location | Description |
|---------|----------|-------------|
| `AzureOpenAi:Endpoint` | User secrets | Azure OpenAI resource endpoint |
| `AzureOpenAi:Deployment` | User secrets | Deployment name |
| `AzureOpenAi:ApiKey` | User secrets | API key |
| `AzureOpenAi:MaxContextTokens` | appsettings.json | Token budget for context window |
| `AzureOpenAi:ReplyMaxTokens` | appsettings.json | Max tokens for the model reply |
| `AzureOpenAi:Temperature` | appsettings.json | Sampling temperature |
| `DatabaseType` | appsettings.json | InMemory / SQLServer / MySQL / PostgreSQL / SQLite |
| `ConnectionStrings:AppData` | appsettings.json | Database connection string |
| `AzureSignalRurl` | appsettings.json | Azure SignalR endpoint (blank = local) |

## Known limitations

- The Azure OpenAI integration is currently only in the console project, not wired into the web UI
- Conversation history is hard-coded in `ChatWithAI.LocalTests` -- no persistent chat sessions