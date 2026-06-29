# FreeExamples.TestClient

> Console application that exercises the `FreeExamples.Client` NuGet client against the API key-protected endpoints — verifies authentication, error handling, and retry behavior.

**Target:** .NET 10 · **Type:** Console Application

---

## What This Does

A test harness that runs a suite of API calls against the FreeExamples server to verify:

- **Authorized access** — valid API key produces successful responses
- **Unauthorized access** — invalid/missing keys produce proper error responses
- **Error handling** — typed exception hierarchy works correctly
- **Fire-and-forget** — `TryPostDataAsync` never throws

---

## Usage

```bash
# 1. Start the FreeExamples server
dotnet run --project FreeExamples/FreeExamples

# 2. Open the API Key Demo page in the browser and generate a key

# 3. Configure the test client
cd FreeExamples/FreeExamples.TestClient
dotnet user-secrets set "FreeExamples:Endpoint" "https://localhost:7271"
dotnet user-secrets set "FreeExamples:ApiKey" "your-generated-api-key"

# 4. Run the tests
dotnet run
```

---

## Configuration

Set via `appsettings.json` or user secrets:

| Key | Description |
|-----|-------------|
| `FreeExamples:Endpoint` | Base URL of the FreeExamples server |
| `FreeExamples:ApiKey` | API key generated from the API Key Demo page |

---

## Pattern Source

Based on `FreeGLBA.TestClientWithNugetPackage` — same test suite structure.

---

Part of the FreeTools solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** A console harness that runs a suite of calls against the FreeExamples server using the `FreeExamples.Client` NuGet client — verifying that a valid API key works, an invalid one fails cleanly (typed exceptions), and the fire-and-forget overload never throws.

**What tech & where?** [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeTools/FreeExamples/FreeExamples.TestClient/Program.cs) (consumes the NuGet client; config via user secrets).

**Why does this exist?** To confirm the client library and the server's API-key middleware actually work end-to-end before relying on them.

**What does it beat?** It exercises the **auth + error + retry** paths against a real server — the integration proof for the API-key pattern. (Mirrors `FreeGLBA.TestClientWithNugetPackage`.)

**Terminology:** **Harness** — a small program that drives another and checks results.

**The hard part, drawn:**
```
  TestClient ─▶ FreeExamples.Client ─Bearer key─▶ server API-key middleware
        valid key ─▶ success   ·   invalid ─▶ typed exception   ·   TryPost… ─▶ never throws
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
