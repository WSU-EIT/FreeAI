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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
