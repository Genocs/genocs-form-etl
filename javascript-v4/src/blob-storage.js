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

// Create a blob container SAS token
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

// Exporting functions to make them accessible from other files
module.exports = {
    getBlobSasUri,
    getAccessToken,
    getFileAccessToken
};

// exports.getBlobSasUri = getBlobSasUri;
// exports.getAccessToken = getAccessToken;
// exports.getFileAccessToken = getFileAccessToken;
