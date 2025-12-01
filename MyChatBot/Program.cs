using System.Text;
using System.Text.Json;

namespace MyChatBot
{
    public class Program
    {
        private const string OllamaUrl = "http://localhost:11434/api/chat";
        private const string ModelName = "llama3.1";

        private static readonly string SystemPrompt =
            "You are a sarcastic and funny software developer who pretends to be lazy.\r\n" +
            "You always answer in a short response (1–3 sentences max).\r\n" +
            "Your sarcasm should be playful, not rude or offensive.\r\n" +
            "Even though you act lazy, you still provide correct and useful answers.\r\n";

        private static readonly List<Dictionary<string, string>> ChatHistory =
            new List<Dictionary<string, string>>();

        static async Task Main(string[] args)
        {
            InitializeSystemPrompt();

            Console.WriteLine("=== Local AI Chatbot (Ollama) ===");
            Console.WriteLine("Type 'exit' to quit.");

            using var httpClient = new HttpClient();

            while (true)
            {
                string userInput = ReadUserInput();
                if (userInput == "exit") break;

                AddUserMessage(userInput);

                string assistantReply = await SendChatRequestAsync(httpClient);

                AddAssistantMessage(assistantReply);
            }
        }

        // ---------------- Helper Methods ----------------

        private static void InitializeSystemPrompt()
        {
            ChatHistory.Add(new Dictionary<string, string>
            {
                ["role"] = "system",
                ["content"] = SystemPrompt
            });
        }

        private static string ReadUserInput()
        {
            Console.Write("\nYou: ");
            return Console.ReadLine()?.Trim() ?? "";
        }

        private static void AddUserMessage(string content)
        {
            ChatHistory.Add(new Dictionary<string, string>
            {
                ["role"] = "user",
                ["content"] = content
            });
        }

        private static void AddAssistantMessage(string content)
        {
            ChatHistory.Add(new Dictionary<string, string>
            {
                ["role"] = "assistant",
                ["content"] = content
            });
        }

        private static async Task<string> SendChatRequestAsync(HttpClient httpClient)
        {
            var requestBody = new
            {
                model = ModelName,
                messages = ChatHistory,
                stream = true
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, OllamaUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8, "application/json")
            };

            using var response = await httpClient.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead);

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            Console.WriteLine("\nAI: ");

            StringBuilder assistantResponse = new StringBuilder();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var jsonDoc = JsonDocument.Parse(line);

                    if (jsonDoc.RootElement.TryGetProperty("message", out var msg)
                        && msg.TryGetProperty("content", out var token))
                    {
                        string chunk = token.GetString();
                        Console.Write(chunk);
                        assistantResponse.Append(chunk);
                    }
                }
                catch
                {
                    // Ignore malformed chunks
                }
            }

            Console.WriteLine();
            return assistantResponse.ToString();
        }
    }
}
