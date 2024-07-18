using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using System;

namespace ProcessImageEx.Helpers;

/// <summary>
/// Blob storage helper function
/// </summary>
public class SasToken
{
    /// <summary>
    ///  CreateAccountSAS based on StorageSharedKeyCredential. Class that create a sas token for the blob storage
    /// </summary>
    /// <param name="sharedKey">The sharedKey</param>
    /// <returns></returns>
    public static string CreateAccountSAS(StorageSharedKeyCredential sharedKey)
    {
        // Create a SAS token that's valid for 5 minutes
        AccountSasBuilder sasBuilder = new AccountSasBuilder()
        {
            Services = AccountSasServices.Blobs | AccountSasServices.Queues,
            ResourceTypes = AccountSasResourceTypes.Service,
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(5),
            Protocol = SasProtocol.Https
        };

        // Set the permission for the SAS token
        sasBuilder.SetPermissions(AccountSasPermissions.Read);

        // Use the key to get the SAS token
        string sasToken = sasBuilder.ToSasQueryParameters(sharedKey).ToString();

        return sasToken;
    }

    /// <summary>
    /// Method that create a sas token for the blob storage
    /// </summary>
    /// <param name="blobName">The name of the blob</param>
    /// <param name="onlySAS">Return only the SAS token</param>
    /// <returns>The sas token</returns>
    public static string GetSasToken(string blobName, bool onlySAS = false)
    {
        string storageAccount = Environment.GetEnvironmentVariable("StorageAccountName");
        string storageKey = Environment.GetEnvironmentVariable("StorageAccountKey");
        string containerName = Environment.GetEnvironmentVariable("StorageContainerName");

        if (string.IsNullOrWhiteSpace(storageAccount))
        {
            throw new ArgumentException("StorageAccountName is not set");
        }

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("StorageAccountKey is not set");
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("StorageContainerName is not set");
        }

        // Create a SAS token that's valid for 5 minutes
        // on blob resource
        BlobSasBuilder sasBuilder = new BlobSasBuilder()
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow,
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        // Set the permission for read
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        // Use the builder to get the SAS token
        string sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(storageAccount, storageKey)).ToString();

        // Check whether I need to return the SAS token only or the full URI
        if (onlySAS)
        {
            return sasToken;
        }

        string blobServiceURI = $"https://{storageAccount}.blob.core.windows.net/{containerName}/{blobName}?{sasToken}";
        return blobServiceURI;


        // StorageSharedKeyCredential storageSharedKeyCredential = new(storageAccount, storageKey);
        // 
        // // Create a BlobServiceClient object with the account SAS appended
        // string blobServiceURI = $"https://{storageAccount}.blob.core.windows.net";
        // sasToken = await CreateAccountSAS(storageSharedKeyCredential);
        // BlobServiceClient blobServiceClientAccountSAS = new BlobServiceClient(new Uri($"{blobServiceURI}?{sasToken}"));
        // 
        // return sasToken;
    }



    public static Uri CreateServiceSASBlob(
                                            BlobClient blobClient,
                                            string storedPolicyName = null)
    {
        // Check if BlobContainerClient object has been authorized with Shared Key
        if (blobClient.CanGenerateSasUri)
        {
            // Create a SAS token that's valid for one day
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = blobClient.GetParentBlobContainerClient().Name,
                BlobName = blobClient.Name,
                Resource = "b"
            };

            if (storedPolicyName == null)
            {
                sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(5);
                sasBuilder.SetPermissions(BlobContainerSasPermissions.Read);
            }
            else
            {
                sasBuilder.Identifier = storedPolicyName;
            }

            Uri sasURI = blobClient.GenerateSasUri(sasBuilder);

            return sasURI;
        }
        else
        {
            // Client object is not authorized via Shared Key
            return null;
        }
    }
}
