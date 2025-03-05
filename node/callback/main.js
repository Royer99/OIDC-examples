// Import MongoDB driver and Azure Identity
const { MongoClient } = require("mongodb");
const { ClientSecretCredential } = require("@azure/identity");
require("dotenv").config();

// Get environment variables
var tenantId = process.env["AZURE_TENANT_ID"];
var clientId = process.env["AZURE_CLIENT_ID"];
var clientSecret = process.env["AZURE_CLIENT_SECRET"];

async function myCallback(request) {
  try {
    const credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
    console.log(`${clientId}/.default`);

    const tokenResponse = await credential.getToken(`${clientId}/.default`);
    console.log("OIDC Token:", tokenResponse.token);

    return { accessToken: tokenResponse.token};
  } catch (error) {
    console.error("Error getting OIDC token:", error);
    throw error;
  }
}

// Construct MongoDB URI with OIDC authentication
const uri = `${process.env.MONGODB_URI}&authMechanism=MONGODB-OIDC`;

// Create a new MongoClient with the correct authentication properties
const client = new MongoClient(uri, {
  authMechanismProperties: { OIDC_CALLBACK: myCallback },
});

// Function to connect to MongoDB and fetch data
async function run() {
  try {
    // Connect to MongoDB
    await client.connect();
    console.log("Connected to MongoDB");

    // Query the database
    const result = await client
      .db("sample_analytics")
      .collection("transactions")
      .findOne({});

    console.log("Query Result:", result);
  } catch (error) {
    console.error("Error connecting to MongoDB:", error);
  } finally {
    // Close the connection
    await client.close();
  }
}

// Run the function
run().catch(console.dir);
