using Azure.AI.DocumentIntelligence;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.AI.ContentUnderstanding;
using Genocs.DocumentImporter.Helpers;

namespace Genocs.DocumentImporter;

public class ProcessBlobsFunction(ILogger<ProcessBlobsFunction> logger)
{
    private readonly ILogger<ProcessBlobsFunction> _logger = logger;

    [Function("ProcessBlobsFunction")]
    [CosmosDBOutput(databaseName: "gnx-documents", containerName: "incoming-docs", Connection = "CosmosDBConnection")]
    public async Task<IncomingDocument> ProcessBlobImage(
        [BlobTrigger("uploaded-forms/{name}", Connection = "StorageConnection")] Stream stream,
        string name,
        CancellationToken cancellationToken = default)
    {

        // Check if the file is an image
        if (!FileTypeHelper.IsValidFile(name))
        {
            _logger.LogError($"Invalid file type for {name}");

            return new IncomingDocument(
                Id: Guid.NewGuid().ToString(),
                Tenant: "gnx",
                BlobName: name,
                DocumentType: "unknown",
                AnalyzeResult: null,
                ContentUnderstandingResult: null,
                ProcessedAtUtc: DateTime.UtcNow);
        }

        // Get resource URL with SAS token
        string resourceURLWithSAS = SasToken.GetSasToken(name, "uploaded-forms");

        AnalyzeResult? azureDocumentIntelligenceAiResponse = null;
        AnalysisResult? azureContentUnderstandingAiResponse = null;
        try
        {
            // Call Content Understanding to extract text and layout
            azureContentUnderstandingAiResponse = await AzureContentUnderstandingHelper.Run(resourceURLWithSAS, _logger, cancellationToken: cancellationToken);

            // Call DocumentAI to extract OCR data
            //azureDocumentIntelligenceAiResponse = await AzureDocumentAIHelper.Run(resourceURLWithSAS, _logger, cancellationToken: cancellationToken);

            // Chunk, and index on SearchAI
            //  AzureSearchAIHelper.Run(azureDocumentIntelligenceAiResponse);

            // Call the OpenAI API
            // var azureOpenAiResponse = AzureOpenAIHelper.Run(resourceURLWithSAS).Result;

            // Alternatively:  Call the OpenAI API
            //openAiResponse = OpenAIHelper.RunAsync(resourceURLWithSAS).Result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing image {name}: {ex.Message}");
        }

        _logger.LogInformation("C# Blob trigger function Processed blob\n Name: {name}", name);

        return new IncomingDocument(
            Id: Guid.NewGuid().ToString(),
            Tenant: "gnx",
            BlobName: name,
            AnalyzeResult: azureDocumentIntelligenceAiResponse,
            ContentUnderstandingResult: azureContentUnderstandingAiResponse,
            DocumentType: Path.GetExtension(name).TrimStart('.').ToLowerInvariant(),
            ProcessedAtUtc: DateTime.UtcNow);
    }
}


public record IncomingDocument(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("tenant")] string Tenant,
    [property: JsonPropertyName("blobName")] string BlobName,
    [property: JsonPropertyName("documentType")] string DocumentType,
    [property: JsonPropertyName("analyzeResult")] AnalyzeResult? AnalyzeResult,
    [property: JsonPropertyName("contentUnderstandingResult")] AnalysisResult? ContentUnderstandingResult,
    [property: JsonPropertyName("processedAtUtc")] DateTime ProcessedAtUtc);
