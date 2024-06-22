const { app, input, output } = require('@azure/functions');
const { v4: uuidv4 } = require('uuid');
const { ApiKeyCredentials } = require('@azure/ms-rest-js');
const { ComputerVisionClient } = require('@azure/cognitiveservices-computervision');


// This is a helper function to simulate a delay
const sleep = require('util').promisify(setTimeout);

const blobStorage = require('../blobStorage'); // Import blobStorage.js file
const constants = require('../constants');
const openaiClient = require('../openAI'); // Import openAI.js file

const imageExtensions = ["jpg", "jpeg", "png", "bmp", "gif", "tiff"];


// This function will analyze the image using the Computer Vision API
async function analyzeImage_OCR(url) {

    try {

        const endpoint = process.env.ComputerVisionEndPoint;
        const apiKey = process.env.ComputerVisionKey;

        const computerVisionClient = new ComputerVisionClient(
            new ApiKeyCredentials({ inHeader: { 'Ocp-Apim-Subscription-Key': apiKey } }), endpoint);

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
        const isTFFResult = await openaiClient.getTaxFreeFormInfo(url);
        return isTFFResult;

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

        //url is image
        const id = uuidv4().toString();
        const sasToken = blobStorage.getAccessToken(blobUrl);

        // Call the analyzeImage function
        const analysis = await analyzeImage_OCR(`${blobUrl}?${sasToken}`);

        // Call CHATGPT API
        const gptResult = await analyzeImage_GPT(`${blobUrl}?${sasToken}`);

        // `type` is the partition key 
        const dataToInsertToDatabase = {
            id,
            type: 'image',
            blobUrl,
            blobSize: blob.length,
            ...analysis,
            gptResult,
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