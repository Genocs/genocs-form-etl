using Azure.AI.DocumentIntelligence;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using ProcessImageEx.Helpers;
using System;
using System.IO;

namespace ProcessImages;

public class ImagesFunction
{
    [FunctionName("ProcessBlobImageFunction")]
    public static void Run(
        [BlobTrigger("uploaded-forms/{name}", Connection = "StorageConnection")] Stream myBlob,
        [CosmosDB(databaseName: "acquired_documents", containerName: "acquired_forms", Connection = "CosmosDBConnection")] out dynamic document,
        string name,
        ILogger log)
    {

        // Check if the file is an image
        if (!FileTypeHelper.IsValidFile(name))
        {
            log.LogError($"Invalid file type for {name}");
            document = null;
            return;
        }

        // Get resource URL with SAS token
        string resourceURLWithSAS = SasToken.GetSasToken(name);


        AnalyzeResult azureDocumentIntelligenceAiResponse = null;
        string openAiResponse = null;


        try
        {
            // Call DocumentAI to extract OCR data
            azureDocumentIntelligenceAiResponse = AzureDocumentAIHelper.Run(resourceURLWithSAS).Result;

            // Chunk, and index on SearchAI
            // AzureSearchAIHelper.Run(azureDocumentIntelligenceAiResponse);

            // Call the OpenAI API
            // var azureOpenAiResponse = AzureOpenAIHelper.Run(resourceURLWithSAS).Result;

            // Alternatively:  Call the OpenAI API
            openAiResponse = OpenAIHelper.RunAsync(resourceURLWithSAS).Result;
        }
        catch (Exception ex)
        {
            log.LogError($"Error processing image {name}: {ex.Message}");
        }

        // Save into cosmosDB
        document = new { id = Guid.NewGuid(), image = name, type = "image", azureDocumentIntelligenceAiResponse, openAiResponse };

        log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
    }
}
