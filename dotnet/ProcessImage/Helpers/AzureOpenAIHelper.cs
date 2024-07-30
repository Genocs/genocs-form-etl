using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System;
using System.Threading.Tasks;

namespace ProcessImageEx.Helpers;

public class AzureOpenAIHelper
{

    private static readonly double TEMPERATURE = 0.2;
    private static readonly double TOP_P = 0.95;
    private static readonly int MAX_TOKENS = 350;
    public static async Task<dynamic> RunAsync(string resourceURL)
    {
        string apiKey = Environment.GetEnvironmentVariable("AzureOpenAIKey");
        string endPoint = Environment.GetEnvironmentVariable("AzureOpenAIEndPoint");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("AzureOpenAIKey is not set");
        }

        if (string.IsNullOrWhiteSpace(endPoint))
        {
            throw new ArgumentException("AzureOpenAIEndPoint is not set");
        }

        AzureOpenAIClient azureClient = new AzureOpenAIClient(
                                                                new Uri(endPoint),
                                                                new AzureKeyCredential(apiKey));

        ChatClient chatClient = azureClient.GetChatClient("gpt4-with-vision");

        ChatCompletion completion = await chatClient.CompleteChatAsync(
            [
                // System messages represent instructions or other guidance about how the assistant should behave
                new SystemChatMessage("You are a helpful assistant that talks like a pirate."),
                // User messages represent user input, whether historical or the most recent input
                new UserChatMessage("Hi, can you help me?"),
                // Assistant messages in a request represent conversation history for responses
                new AssistantChatMessage("Arrr! Of course, me hearty! What can I do for ye?"),
                new UserChatMessage("What's the best way to train a parrot?"),
            ]);

        return completion.Content;


        /*
        var payload = new
        {
            enhancements = new
            {
                ocr = new { enabled = true },
                grounding = new { enabled = true }
            },
            messages = new object[]
            {
                new {
                    role = "system",
                    content = new object[] {
                        new {
                            type = "text",
                            text = "Assistant is an AI chatbot that helps turn a natural language into JSON format. Respond to the request with a JSON object."
                        }
                    }
                },
                new {
                    role = "user",
                    content = new object[] {
                        new {
                            type = "image_url",
                            image_url = new {
                                url = resourceURL // $"data:image/jpeg;base64,{encodedImage}"                                  
                            }
                        },
                        new {
                            type = "text",
                            text = "Extract the merchant relevant information as the denomination, address, business registration number along with the invoice issuing date and the total amount."
                        }
                    }
                }
            },
            temperature = TEMPERATURE,
            top_p = TOP_P,
            max_tokens = MAX_TOKENS,
            stream = false
        };

        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
            using StringContent content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(endPoint, content);

            if (response.IsSuccessStatusCode)
            {
                var responseData = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                return responseData;
            }
            else
            {
                return $"Error: {response.StatusCode}, {response.ReasonPhrase}";
            }
        }

        */
    }
}
