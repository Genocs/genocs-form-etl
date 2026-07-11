using Azure;
using Azure.AI.ContentUnderstanding;
using Azure.Identity;
using Microsoft.Extensions.Logging;


namespace Genocs.DocumentImporter.Helpers;

public class AzureContentUnderstandingHelper
{
    // API_VERSION - the API version to use.
    const string apiVersion = "2025-11-01";


    public static async Task<AnalysisResult> Run(string resourceUrl, ILogger log, CancellationToken cancellationToken = default)
    {
        string endPoint = Environment.GetEnvironmentVariable("DocumentUndestandingEndPoint")
            ?? throw new ArgumentException("DocumentUndestandingEndPoint is not set");

        string apiKey = Environment.GetEnvironmentVariable("DocumentUndestandingKey")
            ?? throw new ArgumentException("DocumentUndestandingKey is not set");

        string analyzerId = Environment.GetEnvironmentVariable("DocumentUndestandingAnalyzerId")
            ?? throw new ArgumentException("DocumentUndestandingAnalyzerId is not set");

        if (string.IsNullOrWhiteSpace(endPoint))
        {
            throw new ArgumentException("DocumentUndestandingEndPoint is not set");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("DocumentUndestandingKey is not set");
        }


        if (string.IsNullOrWhiteSpace(analyzerId))
        {
            throw new ArgumentException("DocumentUndestandingAnalyzerId is not set");
        }

        // Validate Endpoint
        if (!Uri.TryCreate(endPoint, UriKind.Absolute, out Uri? serviceUri) || serviceUri is null)
        {
            throw new ArgumentException("DocumentUndestandingEndPoint is not valid");
        }

        // Validate File URL
        if (!Uri.TryCreate(resourceUrl, UriKind.Absolute, out Uri? resourceUri) || resourceUri is null)
        {
            throw new ArgumentException("DocumentUndestandingResourceUrl is not valid");
        }


        // Set up Content Understanding client.
        ContentUnderstandingClientOptions clientOptions =
            new ContentUnderstandingClientOptions(ServiceVersionMap(apiVersion));

        bool hasKey = !string.IsNullOrWhiteSpace(apiKey) && !apiKey.Contains("{{CONTENT_UNDERSTANDING_KEY}}");
        ContentUnderstandingClient client = hasKey
            ? new ContentUnderstandingClient(serviceUri, new AzureKeyCredential(apiKey), clientOptions)
            : new ContentUnderstandingClient(serviceUri, new DefaultAzureCredential(), clientOptions);

        Operation<AnalysisResult> result = client.Analyze(
            WaitUntil.Completed,
            analyzerId,
            inputs: [new AnalysisInput { Uri = resourceUri }],
            cancellationToken: cancellationToken
        );

        return result.Value;
    }


    static ContentUnderstandingClientOptions.ServiceVersion ServiceVersionMap(string? version)
    {
        return version?.Trim() switch
        {
            "2025-11-01" => ContentUnderstandingClientOptions.ServiceVersion.V2025_11_01,
            _ => ContentUnderstandingClientOptions.ServiceVersion.V2025_11_01 // Default to the latest version
        };
    }
}