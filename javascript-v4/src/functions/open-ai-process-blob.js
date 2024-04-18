const { app, input, output } = require('@azure/functions');
const { v4: uuidv4 } = require('uuid');
const { DocumentAnalysisClient, AzureKeyCredential } = require("@azure/ai-form-recognizer");

// This is a helper function to simulate a delay
const sleep = require('util').promisify(setTimeout);

const blobStorage = require('../blobStorage'); // Import blobStorage.js file
const openaiClient = require('../openAI'); // Import openAI.js file
const constants = require('../constants');

const imageExtensions = ["jpg", "jpeg", "png", "bmp", "gif", "tiff"];

async function analyzeImage(url) {

    try {

        const endpoint = process.env.DocumentIntelligenceEndPoint;
        const apiKey = process.env.DocumentIntelligenceKey;

        // you can use the prebuilt-invoice or prebuilt-receipt model
        // as well as a custom model
        const modelId = process.env.DocumentIntelligenceModelId;

        const client = new DocumentAnalysisClient(endpoint, new AzureKeyCredential(apiKey));

        const poller = await client.beginAnalyzeDocumentFromUrl(modelId, url, {
            onProgress: ({ status }) => {
                console.log(`status: ${status}`);
            },
        });

        // There are more fields than just these three
        const { documents, pages, tables } = await poller.pollUntilDone();

        const isTFFResult = await openaiClient.isTaxFreeForm(url);
        const getTFFInfoResult = await openaiClient.getTaxFreeFormInfo(url);

        console.log(isTFFResult.choices[0].message.content);
        console.log(getTFFInfoResult.choices[0].message.content);


        /*
                console.log("Documents:");
                for (const document of documents || []) {
                    console.log(`Type: ${document.docType}`);
                    console.log("Fields:");
                    for (const [name, field] of Object.entries(document.fields)) {
                        console.log(
                            `Field ${name} has value '${field.value}' with a confidence score of ${field.confidence}`
                        );
                    }
                }
                console.log("Pages:");
                for (const page of pages || []) {
                    console.log(`Page number: ${page.pageNumber} (${page.width}x${page.height} ${page.unit})`);
                }
        
                console.log("Tables:");
                for (const table of tables || []) {
                    console.log(`- Table (${table.columnCount}x${table.rowCount})`);
                    for (const cell of table.cells) {
                        console.log(`  - cell (${cell.rowIndex},${cell.columnIndex}) "${cell.content}"`);
                    }
                }
        */

        return { documents, pages, tables, isTFFResult, getTFFInfoResult };

    } catch (err) {
        console.log(err);
    }
}

app.storageBlob('open-ai-process-blob-image', {
    path: 'images/{name}',
    connection: 'StorageConnection',
    handler: async (blob, context) => {

        context.log(`Storage blob 'open-ai-process-blob-image' url:${context.triggerMetadata.uri}, size:${blob.length} bytes`);

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

    },
    return: output.cosmosDB({
        connection: 'CosmosDBConnection',
        databaseName: constants.COSMOSDB_DATABASE_NAME,
        containerName: constants.COSMOSDB_CONTAINER_NAME
    })
});