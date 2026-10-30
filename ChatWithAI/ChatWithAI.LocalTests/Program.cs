// Program.cs: the console entry point and the Azure OpenAI chat helper (DataAccess).
// Requires: 
//   dotnet add package SharpToken --version 2.0.4
//   dotnet add package Microsoft.Extensions.Configuration
//   dotnet add package Microsoft.Extensions.Configuration.Binder
//   dotnet add package Microsoft.Extensions.Configuration.Json
//   dotnet add package Microsoft.Extensions.Configuration.UserSecrets

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SharpToken;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ChatWithAI.LocalTests
{
    internal class Program
    {
        private static async Task Main()
        {
            Console.WriteLine("Hello, World!");

            // Build configuration (appsettings.json + user secrets)
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<Program>()
                .Build();

            // Required settings
            var endpoint = config["AzureOpenAi:Endpoint"]?.TrimEnd('/')
                             ?? throw new ArgumentNullException("AzureOpenAi:Endpoint");
            var deployment = config["AzureOpenAi:Deployment"]
                             ?? throw new ArgumentNullException("AzureOpenAi:Deployment");
            var apiKey = config["AzureOpenAi:ApiKey"]
                             ?? throw new ArgumentNullException("AzureOpenAi:ApiKey");

            // Optional settings
            var embeddingsDeployment = config["AzureOpenAi:EmbeddingsDeployment"]; // may be null
            var apiVersion = config["AzureOpenAi:ApiVersion"] ?? "2024-06-01";

            // Optional tuning knobs (safe parsing with defaults that match your original sample)
            var maxContextTokens = ParseInt(config["AzureOpenAi:MaxContextTokens"], 128_000);
            var replyMaxTokens = ParseInt(config["AzureOpenAi:ReplyMaxTokens"], 1_000); // original sample used 1000
            var tokenizer = config["AzureOpenAi:TokenizerEncoding"] ?? "o200k_base";
            var temperature = ParseDouble(config["AzureOpenAi:Temperature"], 0.0);

            var settings = new DataAccess.AzureOpenAiSettings {
                Endpoint = endpoint,
                Deployment = deployment,
                EmbeddingsDeployment = embeddingsDeployment,
                ApiKey = apiKey,
                ApiVersion = apiVersion,

                MaxContextTokens = maxContextTokens,
                ReplyMaxTokens = replyMaxTokens,   // default parity with original sample
                TokenizerEncoding = tokenizer,
                Temperature = temperature,
            };

            await DataAccess.ChatWithAi("Hello, AI!", settings);
        }

        private static double ParseDouble(string? value, double fallback)
            => double.TryParse(value, out var x) ? x : fallback;

        private static int ParseInt(string? value, int fallback)
            => int.TryParse(value, out var n) ? n : fallback;
    }

    public static class DataAccess
    {
        // Simple message model for chat payloads
        public sealed class Msg
        {
            public Msg(string role, string content)
            {
                this.role = role;
                this.content = content;
            }
            public string content { get; }
            public string role { get; }
        }

        public sealed class AzureOpenAiSettings
        {
            public required string ApiKey { get; init; }                   // Azure OpenAI API key
            public string ApiVersion { get; init; } = "2024-06-01";        // default API version
            public required string Deployment { get; init; }               // chat/completions deployment name
            public string? EmbeddingsDeployment { get; init; }             // optional
            public required string Endpoint { get; init; }                 // e.g. "https://<resource>.openai.azure.com"

            // Budgeting + tokenizer (defaults aligned with original Program.cs sample)
            public int MaxContextTokens { get; init; } = 128_000;          // model context window for prompts+response
            public int ReplyMaxTokens { get; init; } = 1_000;              // cap for assistant reply (parity with original)

            // Optional: default temperature for requests
            public double Temperature { get; init; } = 0.0;
            public string TokenizerEncoding { get; init; } = "o200k_base"; // SharpToken encoding to use
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
            // Never print any part of the key; report only whether one is configured.
            Console.WriteLine($"Azure OpenAI ApiKey: {(string.IsNullOrWhiteSpace(azureOpenAiApiKey) ? "NOT configured" : "configured")}");
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
