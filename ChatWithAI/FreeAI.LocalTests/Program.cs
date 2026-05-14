// Program.cs
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

namespace FreeAI.LocalTests
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

        private static int ParseInt(string? value, int fallback)
            => int.TryParse(value, out var n) ? n : fallback;

        private static double ParseDouble(string? value, double fallback)
            => double.TryParse(value, out var x) ? x : fallback;
    }
}
