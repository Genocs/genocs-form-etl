using OpenAI.Chat;
using System.ClientModel;

namespace Genocs.DocumentImporter.Helpers;

public class OpenAIHelper
{
    private static readonly float TEMPERATURE = 0.2f;
    private static readonly float TOP_P = 0.95f;
    private static readonly int MAX_TOKENS = 1024;
    private static readonly string OPEN_AI_MODEL = "gpt-4o";

    public static async Task<string> RunAsync(string resourceURL, CancellationToken cancellationToken = default)
    {
        string apiKey = Environment.GetEnvironmentVariable("OpenAIKey")
            ?? throw new ArgumentException("OpenAIKey is not set");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("OpenAIKey is not set");
        }

        ChatClient client = new(OPEN_AI_MODEL, new ApiKeyCredential(apiKey));

        ChatCompletionOptions options = new()
        {
            Temperature = TEMPERATURE,
            TopP = TOP_P,
            MaxOutputTokenCount = MAX_TOKENS
        };

        List<ChatMessage> messages = [
            new UserChatMessage(
                        ChatMessageContentPart.CreateTextPart("You are an assistant to help identify if the image provided is a Tax-Free form or a receipt. The forms are issued by private companies like: 'Global Blue', 'Planet', 'Tax Refund'. The Tax-free form is a standard A4 sheet or a thermal receipt. Please respond concisely, starting by reporting the country of origin, the issuing company. Please replay only with a JSON like the following: { is_taxfree_form : true, is_receipt: true, country: 'USA', vro: 'tax operator' }")),
            new UserChatMessage(
                        ChatMessageContentPart.CreateTextPart("Is the image a TaxFree form or receipt?"),
                        ChatMessageContentPart.CreateImagePart(new Uri(resourceURL), "high"))
        ];

        ChatCompletion completion = await client.CompleteChatAsync(messages, options: options, cancellationToken: cancellationToken);

        return completion?.Content[0]?.Text ?? "Unable to complete";
    }
}
