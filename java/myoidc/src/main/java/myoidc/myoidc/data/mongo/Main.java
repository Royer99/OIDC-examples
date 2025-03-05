package myoidc.myoidc.data.mongo;

import com.azure.core.credential.AccessToken;
import com.azure.core.credential.TokenRequestContext;
import com.azure.identity.ClientSecretCredential;
import com.azure.identity.ClientSecretCredentialBuilder;
import com.mongodb.MongoClientSettings;
import com.mongodb.MongoCredential;
import com.mongodb.MongoCredential.OidcCallback;
import com.mongodb.MongoCredential.OidcCallbackResult;
import com.mongodb.client.MongoClient;
import com.mongodb.client.MongoClients;
import com.mongodb.client.MongoDatabase;
import org.bson.Document;

public class Main {

   public static void main(String[] args) {
       try {
           String appClientId = "";
           String tenantId = "";
           String clientSecret = "";
           String connectionString = "";

           OidcCallback callback = (context) -> {
               ClientSecretCredential clientSecretCredential = new ClientSecretCredentialBuilder()
                   .clientId(appClientId)
                   .tenantId(tenantId)
                   .clientSecret(clientSecret)
                   .build();

               TokenRequestContext tokenRequestContext = new TokenRequestContext().addScopes(String.format("api://%s/.default", appClientId));
               AccessToken token = clientSecretCredential.getTokenSync(tokenRequestContext);

               System.out.println("**************************************************************");
               System.out.println("Token Value:");
               System.out.println(token.getToken());
               System.out.println("**************************************************************");
               return new OidcCallbackResult(token.getToken());
           };

           MongoCredential credential = MongoCredential.createOidcCredential(null).withMechanismProperty("OIDC_CALLBACK", callback);

           MongoClientSettings settings = MongoClientSettings.builder()
               .credential(credential)
               .uuidRepresentation(org.bson.UuidRepresentation.STANDARD)
               .applyConnectionString(new com.mongodb.ConnectionString(connectionString))
               .build();

           try (MongoClient mongoClient = MongoClients.create(settings)) {

               MongoDatabase database = mongoClient.getDatabase("sample_analytics");
               Document doc = database.getCollection("transactions").find().first();

               System.out.println("**************************************************************");
               if (doc != null) {
                   System.out.println("First document: " + doc.toJson());
               } else {
                   System.out.println("No documents found in the collection.");
               }
               System.out.println("**************************************************************");
           }

       } catch (Exception e) {
           e.printStackTrace();
       }
   }
}