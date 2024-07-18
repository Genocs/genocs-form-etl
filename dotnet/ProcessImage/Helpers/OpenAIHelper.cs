using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessImageEx.Helpers;

public class OpenAIHelper
{

    private static readonly double TEMPERATURE = 0.2;
    private static readonly double TOP_P = 0.95;
    private static readonly int MAX_TOKENS = 350;
    public static async Task<dynamic> Run(string resourceURL)
    {
        string apiKey = Environment.GetEnvironmentVariable("OpenAIKey");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("AzureOpenAIKey is not set");
        }

        ChatClient client = new("gpt-4o", apiKey);

        List<ChatMessage> messages = [
            new UserChatMessage(
                        ChatMessageContentPart.CreateTextMessageContentPart("Please describe the following image."),
                        ChatMessageContentPart.CreateImageMessageContentPart(new Uri(resourceURL), "high"))
        ];

        ChatCompletion completion = await client.CompleteChatAsync(messages);

        return completion?.Content[0]?.Text ?? "Unable to complete";
    }
}
