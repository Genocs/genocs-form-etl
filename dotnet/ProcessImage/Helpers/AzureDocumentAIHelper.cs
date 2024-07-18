using Azure;
using Azure.AI.DocumentIntelligence;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessImageEx.Helpers;

public class AzureDocumentAIHelper
{
    public static async Task<AnalyzeResult> Run(string resourceURL)
    {
        string apiKey = Environment.GetEnvironmentVariable("DocumentIntelligenceKey");
        string endPoint = Environment.GetEnvironmentVariable("DocumentIntelligenceEndPoint");
        string modelId = Environment.GetEnvironmentVariable("DocumentIntelligenceModelId");

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

        AzureKeyCredential credential = new AzureKeyCredential(apiKey);
        DocumentIntelligenceClient client = new DocumentIntelligenceClient(new Uri(endPoint), credential);


        // Create the URI to the resource
        Uri resourceUri = new Uri(resourceURL);

        var content = new AnalyzeDocumentContent() { UrlSource = resourceUri };

        var features = new List<DocumentAnalysisFeature>();
        features.Add(DocumentAnalysisFeature.Barcodes);
        features.Add(DocumentAnalysisFeature.OcrHighResolution);


        // Start the analyze operation
        var operation = await client.AnalyzeDocumentAsync(
                                                            waitUntil: WaitUntil.Completed,
                                                            modelId: modelId,
                                                            analyzeRequest: content,
                                                            features: features,
                                                            outputContentFormat: ContentFormat.Markdown);

        AnalyzeResult result = operation.Value;

        return result;
    }
}
