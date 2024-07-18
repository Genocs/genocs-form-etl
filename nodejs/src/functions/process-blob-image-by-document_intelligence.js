const { app, input, output } = require('@azure/functions');
const { v4: uuidv4 } = require('uuid');
const { DocumentAnalysisClient, AzureKeyCredential } = require("@azure/ai-form-recognizer");

const blobStorage = require('../blobStorage'); // Import blobStorage.js file
const constants = require('../constants');

const imageExtensions = ["jpg", "jpeg", "png", "bmp", "gif", "tiff"];

// This function will analyze the image using the Document Intelligence API
async function analyzeImage(url) {

    try {

        const endpoint = process.env.DocumentIntelligenceEndPoint;
        const apiKey = process.env.DocumentIntelligenceKey;

        // you can use the 'prebuilt-invoice' or 'prebuilt-receipt' model 
        // as well as a custom model
        const diModelId = process.env.DocumentIntelligenceModelId;

        const client = new DocumentAnalysisClient(endpoint, new AzureKeyCredential(apiKey));

        const poller = await client.beginAnalyzeDocumentFromUrl(diModelId, url, {
            onProgress: ({ status }) => {
                console.log(`status: ${status}`);
            },
        });

        // There are more fields than just these three
        const diResponse = await poller.pollUntilDone();

        return { ...diResponse };

    } catch (err) {
        console.log(err);
    }
}

app.storageBlob('process-blob-image-by-document_intelligence', {
    path: 'images/{name}',
    connection: 'StorageConnection',
    handler: async (blob, context) => {

        context.log(`Storage blob 'process-blob-image-by-document_intelligence' url:${context.triggerMetadata.uri}, size:${blob.length} bytes`);

        const blobUrl = context.triggerMetadata.uri;
        const extension = blobUrl.split('.').pop();

        if (!blobUrl) {
            // url is empty
            context.log('blob url is null or empty');
            return;
        }

        if (!extension || !imageExtensions.includes(extension.toLowerCase())) {
            // not processing file because it isn't a valid and accepted image extension
            context.log(`Invalid file extension. Only: ${imageExtensions.join(',')} are accepted.`);
            return;
        }

        // Check if the image is more than 4MB
        if (blob.length > 4 * 1024 * 1024) {
            context.log('Image is too large. Max size is 4MB');
            return;
        }

        // Check if the image is a valid TaxFree form image
        //const isTFFResult = await openaiClient.isTaxFreeForm(url);

        // Create a unique id for the document
        const id = uuidv4().toString();

        // Get the SAS token for the blob
        const sasToken = blobStorage.getAccessToken(blobUrl);

        // Run the analysis
        const analysisResult = await analyzeImage(`${blobUrl}?${sasToken}`);

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

    },
    return: output.cosmosDB({
        connection: 'CosmosDBConnection',
        databaseName: constants.COSMOSDB_DATABASE_NAME,
        containerName: constants.COSMOSDB_CONTAINER_NAME
    })
});