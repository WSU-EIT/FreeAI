# ChatWithAI -- Overview

> **Category:** Overview
> **Purpose:** What this project is, why it exists, and how to get started.

---

## What it is

ChatWithAI pairs the full FreeCRM multi-tenant Blazor scaffold (renamed to the `FreeAI` namespace) with `FreeAI.LocalTests` -- a standalone console project that demonstrates token-budgeted chat completions against the Azure OpenAI REST API. The scaffold provides authentication, multi-tenant user management, file storage, a SignalR hub, and a Roslyn plugin system; the console project is the unique AI integration that shows how to budget tokens via SharpToken (BPE tokenizer), build a conversation history, and call the Azure OpenAI chat completions endpoint with a raw `HttpClient`.

## Why it exists

WSU-EIT needed a reference implementation showing how to integrate Azure OpenAI into the standard FreeCRM application framework. ChatWithAI demonstrates the pattern: scaffold provides the web host, token-budgeted chat completions are wired alongside it without disrupting the existing auth and data layers.

## Who it is for

- Developers building LLM-backed features on top of the FreeCRM framework
- WSU-EIT engineers evaluating Azure OpenAI integration patterns
- Teams who want to see how token budgeting works with the SharpToken BPE tokenizer

## Quick start

```bash
cd ChatWithAI/FreeAI
dotnet run
```

For the Azure OpenAI console demo:

```bash
cd ChatWithAI/FreeAI.LocalTests
dotnet user-secrets set "AzureOpenAi:ApiKey" "<key>"
dotnet user-secrets set "AzureOpenAi:Endpoint" "https://<resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAi:Deployment" "<deployment-name>"
dotnet run
```

Navigate to `http://localhost:5100` for the web app.

## Related projects

- [FreeCRM](https://github.com/WSU-EIT/FreeCRM) -- the source framework this is based on
- [FreeManager](../FreeManager/README.md) -- code-generation platform for FreeCRM apps
- [FreeA11yChecker](../FreeA11yChecker/README.md) -- accessibility auditing

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*