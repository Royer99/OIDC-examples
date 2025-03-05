import os
from dotenv import load_dotenv
from azure.identity import ClientSecretCredential, DefaultAzureCredential
from pymongo import MongoClient
from pymongo.auth_oidc import OIDCCallback, OIDCCallbackContext, OIDCCallbackResult

load_dotenv()

tenant_id = os.getenv("AZURE_TENANT_ID")
client_id = os.getenv("AZURE_CLIENT_ID")
client_secret = os.getenv("AZURE_CLIENT_SECRET")
uri = os.getenv("MONGODB_URI")

class MyCallback(OIDCCallback):
    def fetch(self, context: OIDCCallbackContext) -> OIDCCallbackResult:
        #not managaed identity

        credential = ClientSecretCredential( tenant_id=tenant_id, client_id=client_id, client_secret=client_secret)
        token = credential.get_token(f"{client_id}/.default").token
        return OIDCCallbackResult(access_token=token)


props = {"OIDC_CALLBACK": MyCallback()}
try:
    c = MongoClient(uri, authMechanism="MONGODB-OIDC", authMechanismProperties=props)
    result = c.sample_analytics.transactions.find_one({})
    print(result)
    c.close()
except Exception as e:
    print(e)


