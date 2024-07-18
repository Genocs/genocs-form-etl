using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using ProcessImageEx.Helpers;
using System;
using System.IO;

namespace ProcessImages;

public class ImagesFunction
{
    [FunctionName("Function1")]
    public static void Run(
        [BlobTrigger("images/{name}", Connection = "StorageConnection")] Stream myBlob,
        [CosmosDB(databaseName: "acquired_documents", containerName: "processed_form_2", Connection = "CosmosDBConnection")] out dynamic document,
        string name,
        ILogger log)
    {
        // Get resource URL with SAS token
        string resourceURLWithSAS = SasToken.GetSasToken(name);

        // Call the OpenAI API
        var azureOpenAiResponse = AzureOpenAIClient.Run(resourceURLWithSAS).Result;

        // Save into cosmosDB
        document = new { id = Guid.NewGuid(), type = "image", azureOpenAiResponse };

        log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
    }
}
