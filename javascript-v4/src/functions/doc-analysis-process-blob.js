const { app, input, output } = require('@azure/functions');
const { v4: uuidv4 } = require('uuid');
const { ApiKeyCredentials } = require('@azure/ms-rest-js');
const { ComputerVisionClient } = require('@azure/cognitiveservices-computervision');

const blobStorage = require('../blobStorage'); // Import blob-storage.js file

// This is a helper function to simulate a delay
const sleep = require('util').promisify(setTimeout);

const COSMOSDB_DATABASE_NAME = "TaxfreeForms";
const COSMOSDB_CONTAINER_NAME = "uploaded";

const imageExtensions = ["jpg", "jpeg", "png", "bmp", "gif", "tiff"];

async function analyzeImage_OCR(url) {

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


async function analyzeImage_GPT(url) {
   
    try {
        const contents = "placeholder for GPT result";

        return contents;

    } catch (err) {
        console.log(err);
    }
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
        }

        if (!extension || !imageExtensions.includes(extension.toLowerCase())) {
            // not processing file because it isn't a valid and accepted image extension
            context.log(`Invalid file extension. Only: ${imageExtensions.join(',')} are accepted.`);
            return;
        }

        const id = uuidv4().toString();
        const sasToken = blobStorage.getAccessToken(blobUrl);

        // Call the analyzeImage function
        const ocrResult = await analyzeImage_OCR(`${blobUrl}?${sasToken}`);


        // Call CHATGPT API
        const gptResult = await analyzeImage_GPT(`${blobUrl}?${sasToken}`);

        // `type` is the partition key 
        const dataToInsertToDatabase = {
            id,
            type: 'image',
            blobUrl,
            blobSize: blob.length,
            ...ocrResult,
            gptResult,
            trigger: context.triggerMetadata
        }

        return dataToInsertToDatabase;

    },
    return: output.cosmosDB({
        connection: 'CosmosDBConnection',
        databaseName: COSMOSDB_DATABASE_NAME,
        containerName: COSMOSDB_CONTAINER_NAME
    })
});