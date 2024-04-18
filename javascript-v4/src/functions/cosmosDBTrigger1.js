const { app, input, output } = require('@azure/functions');

const constants = require('../constants');
/*
app.cosmosDB('cosmosDBTrigger1', {
    connectionStringSetting: 'CosmosDBConnection',
    databaseName: 'DatabaseName',
    collectionName:  'CollectionName',
    createLeaseCollectionIfNotExists: true,
    handler: (documents, context) => {
        context.log(`Cosmos DB function processed ${documents.length} documents`);
    }
});

*/