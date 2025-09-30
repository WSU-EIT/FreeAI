// Program.cs
// Requires: dotnet add package SharpToken --version 2.0.4
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SharpToken;

namespace FreeAI.LocalTests
{
    internal class Program
    {
        // ======== Types ========
        private record Msg(string role, string content);

        // ======== Entry ========
        private static async Task Main()
        {
            Console.WriteLine("Hello, World!");

            // ---- config ---------------------------------------------------
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<Program>()
                .Build();

            var azureOpenAiEndpoint = config["AzureOpenAi:Endpoint"]?.TrimEnd('/');
            var azureOpenAiDeployment = config["AzureOpenAi:Deployment"];
            var embeddingsDeployment = config["AzureOpenAi:EmbeddingsDeployment"]; // optional
            var azureOpenAiApiKey = config["AzureOpenAi:ApiKey"];
            var apiVersion = config["AzureOpenAi:ApiVersion"] ?? "2024-06-01";

            // Model/context budgeting
            var maxContextTokens = int.TryParse(config["AzureOpenAi:MaxContextTokens"], out var mct) ? mct : 128_000;
            var replyMaxTokens = int.TryParse(config["AzureOpenAi:ReplyMaxTokens"], out var rmt) ? rmt : 1_000;
            var promptBudget = maxContextTokens - replyMaxTokens;

            Console.WriteLine($"Azure OpenAI Endpoint: {azureOpenAiEndpoint}");
            Console.WriteLine($"Azure OpenAI Deployment: {azureOpenAiDeployment}");
            Console.WriteLine($"Azure OpenAI ApiKey: {azureOpenAiApiKey?.Substring(0, 5)}...{azureOpenAiApiKey?.Substring(Math.Max(0, azureOpenAiApiKey.Length - 5))}");
            Console.WriteLine($"API Version: {apiVersion}");
            Console.WriteLine($"MaxContextTokens: {maxContextTokens}  |  ReplyMaxTokens: {replyMaxTokens}");
            Console.WriteLine();

            // ---- http client ----------------------------------------------
            using var http = new HttpClient { BaseAddress = new Uri(azureOpenAiEndpoint!) };
            http.DefaultRequestHeaders.Add("api-key", azureOpenAiApiKey);

            // ---- TOKENIZER -----------------------------------------------
            // GPT-4o family uses o200k_base. Change if you change models.
            var encoding = GptEncoding.GetEncoding("o200k_base");

            int CountTokens(string text) => encoding.Encode(text ?? string.Empty).Count;

            int CountTokensForMessages(IEnumerable<Msg> msgs)
            {
                const int perMessageOverhead = 6; // rough JSON/role overhead buffer
                var sum = 0;
                foreach (var m in msgs)
                    sum += perMessageOverhead + CountTokens(m.role) + CountTokens(m.content);
                return sum;
            }

            List<Msg> BuildMessagesWithinBudget(List<Msg> fullHistory, int budget, out bool trimmed)
            {
                trimmed = false;
                var msgs = new List<Msg>(fullHistory);
                while (CountTokensForMessages(msgs) > budget && msgs.Count > 2) {
                    // keep system at [0]; drop the oldest non-system at [1]
                    msgs.RemoveAt(1);
                    trimmed = true;
                }
                return msgs;
            }

            async Task<Msg?> SummarizeAsync(List<Msg> historyToSummarize)
            {
                if (historyToSummarize.Count == 0) return null;

                var summarizerPrompt = new List<Msg>
                {
                    new("system", "You are a concise meeting minutes generator. Summarize the conversation so far as factual bullet points that preserve instructions, names, and constraints."),
                    new("user", string.Join("\n\n---\n\n", historyToSummarize.Select(m => $"{m.role.ToUpper()}: {m.content}")))
                };

                var uri = $"/openai/deployments/{azureOpenAiDeployment}/chat/completions?api-version={apiVersion}";
                var payload = new {
                    messages = summarizerPrompt.Select(m => new { role = m.role, content = m.content }),
                    temperature = 0,
                    max_tokens = 400
                };
                var json = JsonSerializer.Serialize(payload);
                var res = await http.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"));
                var body = await ReadBodyAsync(res);
                Console.WriteLine($"[Summarize] Status: {(int)res.StatusCode} {res.ReasonPhrase}");
                Console.WriteLine(body);

                if (!res.IsSuccessStatusCode) return null;

                using var doc = JsonDocument.Parse(body);
                var summary = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                return new Msg("assistant", $"(Earlier conversation summary)\n{summary}");
            }

            // ---- 1) enumerate deployments (data plane usually 404) --------
            Console.WriteLine("=== 1) Deployments ===");
            try {
                var uri = $"/openai/deployments?api-version={apiVersion}";
                var res = await http.GetAsync(uri);
                Console.WriteLine($"Status: {(int)res.StatusCode} {res.ReasonPhrase}");
                Console.WriteLine(await ReadBodyAsync(res));
                PrintRateLimitHeaders(res);
            } catch (Exception ex) {
                Console.WriteLine($"Deployments call failed: {ex.Message}");
            }
            Console.WriteLine();

            // ---- 2) Chat Completion with history management ----------------
            Console.WriteLine("=== 2) Chat Completion (with history management) ===");

            var history = new List<Msg>
            {
                new("system", "You are a helpful assistant."),
                new("user",   "Say hello and tell me which model/deployment you are."),
                new("assistant", "Hello!"),
            };
            var newUserTurn = new Msg("user", "Now, please explain your token and rate limits in one paragraph.");
            history.Add(newUserTurn);

            var trimmedMsgs = BuildMessagesWithinBudget(history, promptBudget, out var didTrim);

            if (CountTokensForMessages(trimmedMsgs) > promptBudget) {
                var chunkToSummarize = history.Take(Math.Max(0, history.Count - 4)).ToList();
                var summary = await SummarizeAsync(chunkToSummarize);
                if (summary != null) {
                    var recent = history.Skip(Math.Max(0, history.Count - 4)).ToList();
                    history = new List<Msg> { history[0], summary };
                    history.AddRange(recent);
                    trimmedMsgs = BuildMessagesWithinBudget(history, promptBudget, out _);
                }
            }

            try {
                var uri = $"/openai/deployments/{azureOpenAiDeployment}/chat/completions?api-version={apiVersion}";
                var payload = new {
                    messages = trimmedMsgs.Select(m => new { role = m.role, content = m.content }),
                    temperature = 0,
                    max_tokens = replyMaxTokens
                };
                var requestTokenCount = CountTokensForMessages(trimmedMsgs);
                Console.WriteLine($"[TokenCounts] prompt≈{requestTokenCount}  reply<= {replyMaxTokens}  (budget={promptBudget})");

                var json = JsonSerializer.Serialize(payload);
                var res = await http.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"));
                Console.WriteLine($"Status: {(int)res.StatusCode} {res.ReasonPhrase}");
                Console.WriteLine(await ReadBodyAsync(res));
                PrintRateLimitHeaders(res);
            } catch (Exception ex) {
                Console.WriteLine($"Chat call failed: {ex.Message}");
            }
            Console.WriteLine();

            // ---- 3) Embeddings probe (only if you configure one) -----------
            Console.WriteLine("=== 3) Embeddings Probe ===");
            if (!string.IsNullOrWhiteSpace(embeddingsDeployment)) {
                try {
                    var uri = $"/openai/deployments/{embeddingsDeployment}/embeddings?api-version={apiVersion}";
                    var payload = new { input = new[] { "hello world" } };
                    var json = JsonSerializer.Serialize(payload);
                    var res = await http.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"));
                    Console.WriteLine($"[Embeddings:{embeddingsDeployment}] Status: {(int)res.StatusCode} {res.ReasonPhrase}");
                    Console.WriteLine(await ReadBodyAsync(res));
                    PrintRateLimitHeaders(res);
                } catch (Exception ex) {
                    Console.WriteLine($"Embeddings call failed: {ex.Message}");
                }
            } else {
                Console.WriteLine("No AzureOpenAi:EmbeddingsDeployment configured — skip embeddings.");
            }
            Console.WriteLine();

            // ---- 4) quick error cheat sheet --------------------------------
            Console.WriteLine("=== Hints ===");
            Console.WriteLine("• Characters aren’t enforced; tokens are. ~4 chars ≈ 1 token in English (measure with tokenizer).");
            Console.WriteLine("• 400 with 'maximum context length' means you exceeded the per-request context window.");
            Console.WriteLine("• x-ratelimit-remaining-requests / x-ratelimit-remaining-tokens tell you remaining minute-level capacity.");
            Console.WriteLine("• 401 = bad key; 403 = blocked; 404 = wrong deployment/path; 429 = rate limit.");
            Console.WriteLine();

            if (System.Diagnostics.Debugger.IsAttached) {
                Console.WriteLine("Done. Press any key to exit...");
                Console.ReadKey();
            }
        }

        // ======== Helpers (static) ========
        private static void PrintHeader(HttpResponseMessage res, string name)
        {
            if (res.Headers.TryGetValues(name, out var values))
                Console.WriteLine($"{name}: {string.Join(", ", values)}");
        }

        private static void PrintRateLimitHeaders(HttpResponseMessage res)
        {
            PrintHeader(res, "x-ratelimit-remaining-requests");
            PrintHeader(res, "x-ratelimit-remaining-tokens");
            PrintHeader(res, "retry-after-ms");
        }

        private static async Task<string> ReadBodyAsync(HttpResponseMessage res)
        {
            var json = await res.Content.ReadAsStringAsync();
            try {
                using var doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            } catch { return json; }
        }
    }
}
