import azure.functions as func
import logging
import yfinance as yf
import json
import pandas as pd
import os
from datetime import datetime, timedelta

from azure.core.credentials import AzureKeyCredential
from azure.ai.documentintelligence import DocumentIntelligenceClient
from azure.ai.documentintelligence.models import AnalyzeDocumentRequest, ContentFormat, AnalyzeResult
# from azure.identity import DefaultAzureCredential

from azure.storage.blob import BlobServiceClient

app = func.FunctionApp(http_auth_level=func.AuthLevel.ANONYMOUS)

@app.route(route="get_financials")
def get_financials(req: func.HttpRequest) -> func.HttpResponse:

    # This logging is tracked in the Azure Application Insights
    logging.info('Python HTTP trigger function processed a request.')

    # Extracting parameters from the request
    ticker_symbol = req.params.get('ticker')
    method_name = req.params.get('method')

    if not ticker_symbol or not method_name:
        return func.HttpResponse(
            "Please pass a ticker symbol and method name on the query string or in the request body",
            status_code=400
        )

    try:
        # Fetching data from yfinance
        ticker = yf.Ticker(ticker_symbol)
        data = getattr(ticker, method_name)

        # Handling callable methods vs properties
        if callable(data):
            result = data()  # Call the method if it's callable
        else:
            result = data   # Use the property value directly if not callable

        # Formatting the result for JSON response
        if isinstance(result, pd.DataFrame):
            result = result.to_json()
        elif not isinstance(result, str):
            result = json.dumps(result)

        return func.HttpResponse(result, mimetype="application/json")
    except Exception as e:
        logging.error(f"Error occurred: {e}")
        return func.HttpResponse(
            "Error occurred: " + str(e),
            status_code=500
        )


def get_blob_url_with_sas_token(blob_name: str) -> str:

    storage_account_name = os.environ["StorageAccountName"]
    storage_account_key = os.environ["StorageAccountKey"]

    account_url = f"https://{storage_account_name}.blob.core.windows.net"
    container_name = "images"

    # Create the BlobServiceClient object
    blob_service_client = BlobServiceClient(
        account_url=account_url, credential=storage_account_key)

    # Instantiate a ContainerClient
    container_client = blob_service_client.get_container_client(container_name)

    from azure.storage.blob import AccessPolicy, ContainerSasPermissions
    access_policy = AccessPolicy(permission=ContainerSasPermissions(read=True),
                                 expiry=datetime.utcnow() + timedelta(hours=1),
                                 start=datetime.utcnow() - timedelta(minutes=1))

    identifiers = {'default_access_policy': access_policy}

    # Set the access policy on the container
    container_client.set_container_access_policy(
        signed_identifiers=identifiers)

    # Use access policy to generate a sas token
    from azure.storage.blob import generate_container_sas

    sas_token = generate_container_sas(
        container_client.account_name,
        container_client.container_name,
        account_key=container_client.credential.account_key,
        policy_id='default_access_policy'
    )

    result = f"{account_url}/{blob_name}?{sas_token}"

    print(result)
    return result


# This function is triggered by a new blob being uploaded to the specified container


@app.blob_trigger(arg_name="myblob",
                  path="images/{name}",
                  connection="StorageConnection")
def BlobTrigger(myblob: func.InputStream, context: func.Context):

    #context.log(f"Storage blob 'process-blob-image-by-document_intelligence' url:{context.triggerMetadata.uri}, size:{myblob.length} bytes")

    blobUrl = context.triggerMetadata.uri;
    extension = blobUrl.split('.').pop();

    print(blobUrl)
    print(extension)

    return


    endpoint = os.environ["DocumentIntelligenceEndPoint"]
    key = os.environ["DocumentIntelligenceKey"]

    # Get the URL of the blob with SAS token
    container_sas_url = get_blob_url_with_sas_token(myblob.name)

    print(container_sas_url)

    # Create the DocumentIntelligenceClient object
    di_client = DocumentIntelligenceClient(endpoint=endpoint, credential=AzureKeyCredential(key))

    poller = di_client.begin_analyze_document(
        "prebuilt-layout",
        AnalyzeDocumentRequest(url_source=container_sas_url),
        output_content_format=ContentFormat.MARKDOWN,
    )

    ##result: AnalyzeResult = poller.result()

    #print(f"Here's the full content in format {result.content_format}:\n")
    #print(result.content)    

    # with open(blog_url, "rb") as f:
    #     poller = di_client.begin_analyze_document(
    #         "prebuilt-layout", 
    #         analyze_request=f,
    #         content_type="application/octet-stream"
    #     )
    #    result: AnalyzeResult = poller.result()

    logging.info(f"Python blob trigger function processed blob"
                 f"Name: {myblob.name}"
                 f"Blob Size: {myblob.length} bytes")
