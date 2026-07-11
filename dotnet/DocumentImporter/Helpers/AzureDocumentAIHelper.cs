using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Logging;

namespace Genocs.DocumentImporter.Helpers;

public class AzureDocumentAIHelper
{
    public static async Task<AnalyzeResult> Run(string resourceURL, ILogger log, CancellationToken cancellationToken = default)
    {
        string apiKey = Environment.GetEnvironmentVariable("DocumentIntelligenceKey") 
            ?? throw new ArgumentException("DocumentIntelligenceKey is not set");

        string endPoint = Environment.GetEnvironmentVariable("DocumentIntelligenceEndPoint") 
            ?? throw new ArgumentException("DocumentIntelligenceEndPoint is not set");

        string modelId = Environment.GetEnvironmentVariable("DocumentIntelligenceModelId") 
            ?? throw new ArgumentException("DocumentIntelligenceModelId is not set");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("DocumentIntelligenceKey is not set");
        }

        if (string.IsNullOrWhiteSpace(endPoint))
        {
            throw new ArgumentException("DocumentIntelligenceEndPoint is not set");
        }

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("DocumentIntelligenceModelId is not set");
        }

        AzureKeyCredential credential = new(apiKey);
        DocumentIntelligenceClient client = new(new Uri(endPoint), credential);

        // Create the URI to the resource
        Uri resourceUri = new(resourceURL);

        var options = BuildOptions(modelId, resourceUri, includeKeyValuePairs: SupportsKeyValuePairs(modelId));
        LogPreparedRequest(log, modelId, options.Features);

        AnalyzeResult result;
        try
        {
            // Start the analyze operation using the requested model.
            var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, options, cancellationToken);
            result = operation.Value;
        }
        catch (RequestFailedException ex) when (IsUnsupportedKeyValuePairs(ex) && options.Features.Contains(DocumentAnalysisFeature.KeyValuePairs))
        {
            log.LogWarning(
                "Model '{ModelId}' rejected keyValuePairs feature. Retrying without keyValuePairs.",
                modelId);

            var retryOptions = BuildOptions(modelId, resourceUri, includeKeyValuePairs: false);
            LogPreparedRequest(log, modelId, retryOptions.Features);

            var retryOperation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, retryOptions, cancellationToken);
            result = retryOperation.Value;
        }
        catch (RequestFailedException ex) when (IsModelNotFound(ex) && !modelId.Equals("prebuilt-read", StringComparison.OrdinalIgnoreCase))
        {
            // Some Cognitive Services endpoints do not expose prebuilt-document.
            var fallbackModelId = "prebuilt-read";
            log.LogWarning(
                "Document Intelligence model '{ModelId}' was not found. Falling back to '{FallbackModelId}'.",
                modelId,
                fallbackModelId);

            var fallbackOptions = BuildOptions(fallbackModelId, resourceUri, includeKeyValuePairs: false);
            LogPreparedRequest(log, fallbackModelId, fallbackOptions.Features, isFallback: true);

            var fallbackOperation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, fallbackOptions, cancellationToken);
            result = fallbackOperation.Value;
        }

        int pageCount = result.Pages?.Count ?? 0;
        int paragraphCount = result.Paragraphs?.Count ?? 0;
        int tableCount = result.Tables?.Count ?? 0;
        int keyValuePairCount = result.KeyValuePairs?.Count ?? 0;
        int contentLength = result.Content?.Length ?? 0;

        log.LogInformation(
            "Document Intelligence response summary: pages={PageCount}, paragraphs={ParagraphCount}, tables={TableCount}, keyValuePairs={KeyValuePairCount}, contentLength={ContentLength}",
            pageCount,
            paragraphCount,
            tableCount,
            keyValuePairCount,
            contentLength);

        return result;
    }

    private static bool SupportsKeyValuePairs(string modelId)
    {
        return modelId.Equals("prebuilt-document", StringComparison.OrdinalIgnoreCase);
    }

    private static AnalyzeDocumentOptions BuildOptions(string modelId, Uri resourceUri, bool includeKeyValuePairs)
    {
        var options = new AnalyzeDocumentOptions(modelId, resourceUri);
        options.Features.Add(DocumentAnalysisFeature.Barcodes);
        options.Features.Add(DocumentAnalysisFeature.OcrHighResolution);

        if (includeKeyValuePairs)
        {
            options.Features.Add(DocumentAnalysisFeature.KeyValuePairs);
        }

        options.OutputContentFormat = DocumentContentFormat.Markdown;
        return options;
    }

    private static void LogPreparedRequest(ILogger log, string modelId, System.Collections.Generic.IEnumerable<DocumentAnalysisFeature> features, bool isFallback = false)
    {
        string enabledFeatures = string.Join(", ", features.Select(feature => feature.ToString()));
        if (isFallback)
        {
            log.LogInformation(
                "Document Intelligence fallback request prepared with model '{ModelId}' and features [{Features}]",
                modelId,
                enabledFeatures);
            return;
        }

        log.LogInformation(
            "Document Intelligence request prepared with model '{ModelId}' and features [{Features}]",
            modelId,
            enabledFeatures);
    }

    private static bool IsModelNotFound(RequestFailedException ex)
    {
        return ex.Status == 404 &&
               ex.ErrorCode != null &&
               ex.ErrorCode.Equals("NotFound", StringComparison.OrdinalIgnoreCase) &&
               ex.Message.Contains("ModelNotFound", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnsupportedKeyValuePairs(RequestFailedException ex)
    {
        return ex.Status == 400 &&
               ex.ErrorCode != null &&
               ex.ErrorCode.Equals("InvalidArgument", StringComparison.OrdinalIgnoreCase) &&
               ex.Message.Contains("keyValuePairs", StringComparison.OrdinalIgnoreCase) &&
               ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase);
    }
}