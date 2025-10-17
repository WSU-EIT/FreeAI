// DataAccess.cs
using SharpToken;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FreeAI.LocalTests
{
    public static class DataAccess
    {
        // Simple message model for chat payloads
        public sealed class Msg
        {
            public string role { get; }
            public string content { get; }

            public Msg(string role, string content)
            {
                this.role = role;
                this.content = content;
            }
        }

        public sealed class AzureOpenAiSettings
        {
            public required string Endpoint { get; init; }                 // e.g. "https://<resource>.openai.azure.com"
            public required string Deployment { get; init; }               // chat/completions deployment name
            public string? EmbeddingsDeployment { get; init; }             // optional
            public required string ApiKey { get; init; }                   // Azure OpenAI API key
            public string ApiVersion { get; init; } = "2024-06-01";        // default API version

            // Budgeting + tokenizer (defaults aligned with original Program.cs sample)
            public int MaxContextTokens { get; init; } = 128_000;          // model context window for prompts+response
            public int ReplyMaxTokens { get; init; } = 1_000;              // cap for assistant reply (parity with original)
            public string TokenizerEncoding { get; init; } = "o200k_base"; // SharpToken encoding to use

            // Optional: default temperature for requests
            public double Temperature { get; init; } = 0.0;
        }

        public static async Task ChatWithAi(string message, AzureOpenAiSettings settings)
        {
            // ---- config ---------------------------------------------------
            var azureOpenAiEndpoint = settings.Endpoint.TrimEnd('/');
            var azureOpenAiDeployment = settings.Deployment;
            var embeddingsDeployment = settings.EmbeddingsDeployment; // may be null
            var azureOpenAiApiKey = settings.ApiKey;
            var apiVersion = string.IsNullOrWhiteSpace(settings.ApiVersion)
                                        ? "2024-06-01"
                                        : settings.ApiVersion;

            var maxContextTokens = settings.MaxContextTokens;
            var replyMaxTokens = settings.ReplyMaxTokens;

            // basic guardrails
            if (replyMaxTokens >= maxContextTokens)
                replyMaxTokens = Math.Max(16, maxContextTokens / 4);

            // Model/context budgeting
            var promptBudget = Math.Max(0, maxContextTokens - replyMaxTokens);

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
            // Use configurable tokenizer (default o200k_base for GPT-4o family)
            var encoding = GptEncoding.GetEncoding(settings.TokenizerEncoding);

            int CountTokens(string text) => encoding.Encode(text ?? string.Empty).Count;

            int CountTokensForMessages(IEnumerable<Msg> msgs)
            {
                const int perMessageOverhead = 6; // rough JSON/role overhead buffer
                var sum = 0;
                foreach (var m in msgs)
                    sum += perMessageOverhead + CountTokens(m.role) + CountTokens(m.content);
                return sum;
            }

      



            // ---- 2) Chat Completion with history management ----------------
            Console.WriteLine("=== 2) Chat Completion (with history management) ===");

            var history = new List<Msg>
            {
                new("system", "You are a helpful assistant who is an expert in c# blazor."),
                new("user",   "Say hello and tell me which model/deployment you are."),
                new("assistant", "Hello!"),
            };
            var newUserTurn = new Msg("user", "Now give me a basic program.cs for a simple hello world web application. be as minimal as you can.");
            history.Add(newUserTurn);

            var allMessages = history.ToList();           

            try {
                var uri = $"/openai/deployments/{azureOpenAiDeployment}/chat/completions?api-version={apiVersion}";
                var payload = new {
                    messages = allMessages.Select(m => new { role = m.role, content = m.content }),
                    temperature = settings.Temperature,
                    max_tokens = replyMaxTokens
                };
                var requestTokenCount = CountTokensForMessages(allMessages);
                Console.WriteLine($"[TokenCounts] prompt≈{requestTokenCount}  reply<= {replyMaxTokens}  (budget={promptBudget})");

                var json = JsonSerializer.Serialize(payload);
                var res = await http.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"));
                Console.WriteLine($"Status: {(int)res.StatusCode} {res.ReasonPhrase}");
                Console.WriteLine(await ReadBodyAsync(res));
            } catch (Exception ex) {
                Console.WriteLine($"Chat call failed: {ex.Message}");
            }
            Console.WriteLine();

      
            if (System.Diagnostics.Debugger.IsAttached) {
                Console.WriteLine("Done. Press any key to exit...");
                Console.ReadKey();
            }
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
