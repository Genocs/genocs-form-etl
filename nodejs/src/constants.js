// This file contains the values of the constants used in the application
// The values of these constants are used in other files
// This file do not contains any sensitive information apart from the name o the database and container
// PLEASE DO NOT ADD ANY SENSITIVE INFORMATION IN THIS FILE
// PLAN TO SETUP ENVIRONMENT VARIABLES FOR SENSITIVE INFORMATION

// The name of the database
const COSMOSDB_DATABASE_NAME = "acquired_documents";

// The name of the container where the data is uploaded
const COSMOSDB_CONTAINER_NAME = "processed_forms";

// Exporting functions to make them accessible from other files
module.exports = {
    COSMOSDB_DATABASE_NAME,
    COSMOSDB_CONTAINER_NAME
};

// exports.COSMOSDB_DATABASE_NAME = COSMOSDB_DATABASE_NAME;
// exports.COSMOSDB_CONTAINER_NAME = COSMOSDB_CONTAINER_NAME;
// exports.getFileAccessToken = getFileAccessToken;