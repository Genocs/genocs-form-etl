const { app, input, output } = require('@azure/functions');
const { v4: uuidv4 } = require('uuid');
const { ApiKeyCredentials } = require('@azure/ms-rest-js');
const { ComputerVisionClient } = require('@azure/cognitiveservices-computervision');
const { AccountSASPermissions,
    AccountSASServices,
    AccountSASResourceTypes,
    SASProtocol,
    StorageSharedKeyCredential,
    generateBlobSASQueryParameters,
    generateAccountSASQueryParameters,
    BlobSASPermissions } = require('@azure/storage-blob');

// This is a helper function to simulate a delay
const sleep = require('util').promisify(setTimeout);

const STATUS_SUCCEEDED = "succeeded";
const STATUS_FAILED = "failed"

const COSMOSDB_DATABASE_NAME = "TaxfreeForms";
const COSMOSDB_CONTAINER_NAME = "uploaded";

const imageExtensions = ["jpg", "jpeg", "png", "bmp", "gif", "tiff"];

async function analyzeImage(url) {

    try {

        const computerVision_ResourceKey = process.env.ComputerVisionKey;
        const computerVision_Endpoint = process.env.ComputerVisionEndPoint;

        const computerVisionClient = new ComputerVisionClient(
            new ApiKeyCredentials({ inHeader: { 'Ocp-Apim-Subscription-Key': computerVision_ResourceKey } }), computerVision_Endpoint);

        const contents = await computerVisionClient.analyzeImage(url, {
            visualFeatures: ['ImageType', 'Categories', 'Tags', 'Description', 'Objects', 'Adult', 'Faces']
        });

        return contents;

    } catch (err) {
        console.log(err);
    }
}


// This function is used to generate SAS token for blob container
function getAccessToken() {

    const storage_AccountName = process.env.StorageAccountName;
    const storage_Key = process.env.StorageKey;

    const sharedKeyCredential = new StorageSharedKeyCredential(storage_AccountName, storage_Key)

    const sasOptions = {
        services: AccountSASServices.parse("bf").toString(),            // blobs, tables, queues, files
        resourceTypes: AccountSASResourceTypes.parse("sco").toString(), // service, container, object
        permissions: AccountSASPermissions.parse("r"),                  // permissions
        protocol: SASProtocol.Https,
        startsOn: new Date(),
        expiresOn: new Date(new Date().valueOf() + (60 * 1000 * 1)),   // 1 minutes
    };

    const sasToken = generateAccountSASQueryParameters(sasOptions, sharedKeyCredential).toString();
    console.log(`SAS token for blob container is: url?${sasToken}`);
    return sasToken;
}

// This function is used to generate SAS token for blob container
function getFileAccessToken(url) {

    const storage_AccountName = process.env.StorageAccountName;
    const storage_Key = process.env.StorageKey;
    const blobName = url.split('/').pop();

    const sharedKeyCredential = new StorageSharedKeyCredential(storage_AccountName, storage_Key);

    const sasOptions = {
        services: AccountSASServices.parse("bf").toString(),            // blobs, tables, queues, files
        resourceTypes: AccountSASResourceTypes.parse("sco").toString(), // service, container, object
        permissions: AccountSASPermissions.parse("r"),                  // permissions
        protocol: SASProtocol.Https,
        startsOn: new Date(),
        expiresOn: new Date(new Date().valueOf() + (60 * 1000 * 1)),   // 1 minutes
        blobName: blobName
    };

    const sasToken = generateBlobSASQueryParameters(sasOptions, sharedKeyCredential).toString();
    console.log(`SAS token for blob container is: url?${sasToken}`);
    return sasToken;
}

// Create a service SAS for a blob
function getBlobSasUri(url) {

    const storage_AccountName = process.env.StorageAccountName;
    const storage_Key = process.env.StorageKey;
    const storage_Container = process.env.StorageContainerName;

    const blobName = url.split('/').pop();

    const sharedKeyCredential = new StorageSharedKeyCredential(storage_AccountName, storage_Key);

    const sasOptions = {
        containerName: storage_Container,
        blobName: blobName,
        startsOn: new Date(),
        expiresOn: new Date(new Date().valueOf() + 3600 * 1000),
        permissions: BlobSASPermissions.parse("r")
    };

    const sasToken = generateBlobSASQueryParameters(sasOptions, sharedKeyCredential).toString();
    console.log(`SAS token for blob is: ${sasToken}`);
    return sasToken;
}

app.storageBlob('process-blob-image', {
    path: 'images/{name}',
    connection: 'StorageConnection',
    handler: async (blob, context) => {

        context.log(`Storage blob 'process-blob-image' url:${context.triggerMetadata.uri}, size:${blob.length} bytes`);

        const blobUrl = context.triggerMetadata.uri;
        const extension = blobUrl.split('.').pop();

        if (!blobUrl) {
            // url is empty
            context.log('blob url is null or empty');
            return;
        } else if (!extension || !imageExtensions.includes(extension.toLowerCase())) {
            // not processing file because it isn't a valid and accepted image extension
            context.log(`Invalid file extension. Only: ${imageExtensions.join(',')} are accepted.`);
            return;
        } else {
            //url is image
            const id = uuidv4().toString();
            const sasToken = getAccessToken(blobUrl);
            const analysis = await analyzeImage(`${blobUrl}?${sasToken}`);

            // `type` is the partition key 
            const dataToInsertToDatabase = {
                id,
                type: 'image',
                blobUrl,
                blobSize: blob.length,
                ...analysis,
                trigger: context.triggerMetadata
            }

            return dataToInsertToDatabase;
        }
    },
    return: output.cosmosDB({
        connection: 'CosmosDBConnection',
        databaseName: COSMOSDB_DATABASE_NAME,
        containerName: COSMOSDB_CONTAINER_NAME
    })
});