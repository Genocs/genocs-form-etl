const COSMOSDB_DATABASE_NAME = "acquired_documents";
const COSMOSDB_CONTAINER_NAME = "processed_forms";

// Exporting functions to make them accessible from other files
module.exports = {
    COSMOSDB_DATABASE_NAME,
    COSMOSDB_CONTAINER_NAME
};

// exports.COSMOSDB_DATABASE_NAME = COSMOSDB_DATABASE_NAME;
// exports.COSMOSDB_CONTAINER_NAME = COSMOSDB_CONTAINER_NAME;
// exports.getFileAccessToken = getFileAccessToken;