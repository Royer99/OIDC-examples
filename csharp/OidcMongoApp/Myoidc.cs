using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Authentication.Oidc; // ✅ Correct OIDC namespace
using Azure.Identity;
using Azure.Core;
using dotenv.net;
using System.Dynamic;
using System.Text.Json;

class MyOidc
{
    static async Task Main(string[] args)
    {
        // Load environment variables from .env file
        DotEnv.Load();
        var envVars = DotEnv.Read();

        var tenantId = envVars["AZURE_TENANT_ID"];
        var clientId = envVars["AZURE_CLIENT_ID"];
        var clientSecret = envVars["AZURE_CLIENT_SECRET"];
        var mongoDbUri = envVars["MONGODB_URI"]+"&authMechanism=MONGODB-OIDC&authSource=$external";

        Console.WriteLine("🔹 Tenant ID: " + tenantId);
        Console.WriteLine("🔹 Client ID: " + clientId);
        Console.WriteLine("🔹 Connecting to MongoDB...");

        try
        {
            // Create an instance of the OIDC callback class
            var oidcCallback = new MyOidcCallback(tenantId, clientId, clientSecret);

            // Configure MongoDB client settings
            var mongoClientSettings = MongoClientSettings.FromConnectionString(mongoDbUri);
            mongoClientSettings.Credential = MongoCredential.CreateOidcCredential(oidcCallback); // ✅ Pass instance

            // Initialize MongoDB client
            var client = new MongoClient(mongoClientSettings);

            // Access database and collection
            var database = client.GetDatabase("sample_analytics");
            var collection = database.GetCollection<dynamic>("transactions");

            // Fetch data from MongoDB
            var results = await collection.Find(Builders<dynamic>.Filter.Empty).FirstOrDefaultAsync();

            Console.WriteLine("✅ Successfully connected to MongoDB!");
            //return object
            Console.WriteLine(results.ToString());
        
            foreach (var document in results)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(document, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Error: " + ex.Message);
        }
    }
}

// ✅ Define a class that implements IOidcCallback correctly
class MyOidcCallback : IOidcCallback
{
    private readonly string tenantId;
    private readonly string clientId;
    private readonly string clientSecret;

    public MyOidcCallback(string tenantId, string clientId, string clientSecret)
    {
        this.tenantId = tenantId;
        this.clientId = clientId;
        this.clientSecret = clientSecret;
    }

    // ✅ Correct implementation of required method in IOidcCallback
    public MongoDB.Driver.Authentication.Oidc.OidcAccessToken GetOidcAccessToken(OidcCallbackParameters parameters  , CancellationToken cancellationToken){
        return new MongoDB.Driver.Authentication.Oidc.OidcAccessToken("token", null);
    }

    public async Task<MongoDB.Driver.Authentication.Oidc.OidcAccessToken> GetOidcAccessTokenAsync(OidcCallbackParameters parameters, CancellationToken cancellationToken)
    {
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        var accessToken = await credential.GetTokenAsync(
            new TokenRequestContext(new[] { clientId+"/.default" }),
            cancellationToken
        );

        if (string.IsNullOrEmpty(accessToken.Token))
        {
            throw new Exception("❌ Failed to retrieve a valid access token.");
        }

        Console.WriteLine("✅ OIDC Token Acquired");
        Console.WriteLine("token: " + accessToken.Token);

        return new MongoDB.Driver.Authentication.Oidc.OidcAccessToken(accessToken.Token, null);
    }
}
