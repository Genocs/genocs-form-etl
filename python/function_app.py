import azure.functions as func
import logging
import yfinance as yf
import json
import pandas as pd

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


@app.blob_trigger(arg_name="myblob",
                  path="images/{name}",
                  connection="StorageConnection")
def BlobTrigger(myblob: func.InputStream):
    logging.info(f"Python blob trigger function processed blob"
                 f"Name: {myblob.name}"
                 f"Blob Size: {myblob.length} bytes")
