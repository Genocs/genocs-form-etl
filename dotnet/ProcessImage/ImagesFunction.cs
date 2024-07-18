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
        [BlobTrigger("images/{name}", Connection = "StorageConnection")] Stream myBlob,
        [CosmosDB(databaseName: "acquired_documents", containerName: "processed_form_2", Connection = "CosmosDBConnection")] out dynamic document,
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

        // Call DocumentAI to extract OCR data
        var azureDocumentIntelligenceAiResponse = AzureDocumentAIHelper.Run(resourceURLWithSAS).Result;

        // Chunk, and index on SearchAI
        // AzureSearchAIHelper.Run(azureDocumentIntelligenceAiResponse);

        // Call the OpenAI API
        var azureOpenAiResponse = AzureOpenAIHelper.Run(resourceURLWithSAS).Result;

        // Save into cosmosDB
        document = new { id = Guid.NewGuid(), type = "image", azureOpenAiResponse, azureDocumentIntelligenceAiResponse };

        log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
    }
}
