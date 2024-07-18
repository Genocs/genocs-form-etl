using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ProcessImageEx.Helpers;

public class AzureOpenAIClient
{

    private static readonly double TEMPERATURE = 0.2;
    private static readonly double TOP_P = 0.95;
    private static readonly int MAX_TOKENS = 350;
    public static async Task<dynamic> Run(string resourceURL)
    {
        string openAIKey = Environment.GetEnvironmentVariable("AzureOpenAIKey");
        string openAIEndPoint = Environment.GetEnvironmentVariable("AzureOpenAIEndPoint");

        if (string.IsNullOrWhiteSpace(openAIKey))
        {
            throw new ArgumentException("AzureOpenAIKey is not set");
        }

        if (string.IsNullOrWhiteSpace(openAIEndPoint))
        {
            throw new ArgumentException("AzureOpenAIEndPoint is not set");
        }


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
            httpClient.DefaultRequestHeaders.Add("api-key", openAIKey);
            using StringContent content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(openAIEndPoint, content);

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
    }
}
