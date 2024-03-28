const { AccountSASPermissions,
    AccountSASServices,
    AccountSASResourceTypes,
    SASProtocol,
    StorageSharedKeyCredential,
    generateBlobSASQueryParameters,
    generateAccountSASQueryParameters,
    BlobSASPermissions } = require('@azure/storage-blob');


// Create a service SAS for a blob
function getBlobSasUri(url) {

    const storageAccountName = process.env.StorageAccountName;
    const storageKey = process.env.StorageKey;
    const storageContainer = process.env.StorageContainerName;

    const blobName = url.split('/').pop();

    const sharedKeyCredential = new StorageSharedKeyCredential(storageAccountName, storageKey);

    const sasOptions = {
        containerName: storageContainer,
        blobName: blobName,
        startsOn: new Date(),
        expiresOn: new Date(new Date().valueOf() + 3600 * 1000),
        permissions: BlobSASPermissions.parse("r")
    };

    const sasToken = generateBlobSASQueryParameters(sasOptions, sharedKeyCredential).toString();
    console.log(`SAS token for blob is: ${sasToken}`);
    return sasToken;
}

// Create a blob container SAS token
function getAccessToken() {

    const storageAccountName = process.env.StorageAccountName;
    const storageKey = process.env.StorageKey;

    const sharedKeyCredential = new StorageSharedKeyCredential(storageAccountName, storageKey)

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

    const storageAccountName = process.env.StorageAccountName;
    const storageKey = process.env.StorageKey;
    const blobName = url.split('/').pop();

    const sharedKeyCredential = new StorageSharedKeyCredential(storageAccountName, storageKey);

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

// Exporting functions to make them accessible from other files
module.exports = {
    getBlobSasUri,
    getAccessToken,
    getFileAccessToken
};

// exports.getBlobSasUri = getBlobSasUri;
// exports.getAccessToken = getAccessToken;
// exports.getFileAccessToken = getFileAccessToken;
