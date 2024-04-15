const { app, input, output } = require('@azure/functions');


const COSMOSDB_DATABASE_NAME = "TaxfreeForms";
const COSMOSDB_CONTAINER_NAME = "uploaded";
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