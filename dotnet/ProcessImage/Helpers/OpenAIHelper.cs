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
    private static readonly string OPEN_AI_MODEL = "gpt-4o";

    public static async Task<string> RunAsync(string resourceURL)
    {
        string apiKey = Environment.GetEnvironmentVariable("OpenAIKey");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("AzureOpenAIKey is not set");
        }

        ChatClient client = new(OPEN_AI_MODEL, apiKey);

        List<ChatMessage> messages = [
            new UserChatMessage(
                        ChatMessageContentPart.CreateTextMessageContentPart("You are an assistant to help identify if the image provided is a Tax-Free form or a receipt. The forms are issued by private companies like: 'Global Blue', 'Planet', 'Tax Refund'. The Tax-free form is a standard A4 sheet or a thermal receipt. Please respond concisely, starting by reporting the country of origin, the issuing company. Please replay only with a JSON like the following: { is_taxfree_form : true, is_receipt: true, country: 'USA', vro: 'tax operator' }")),
            new UserChatMessage(
                        ChatMessageContentPart.CreateTextMessageContentPart("Is the image a TaxFree form or receipt?"),
                        ChatMessageContentPart.CreateImageMessageContentPart(new Uri(resourceURL), "high"))
        ];

        ChatCompletion completion = await client.CompleteChatAsync(messages);

        return completion?.Content[0]?.Text ?? "Unable to complete";
    }
}
