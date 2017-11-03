using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Blob; // Namespace for Blob storage types
using System.Configuration;
using System;
using System.Web.Http;

namespace DemoForCNTV
{
    public static class HttpTriggerFunction
    {
        [FunctionName("HttpTriggerFunction")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            string _CNTV_Domain = "http://asp.cntv.lxdns.com/";

            // parse query parameter
            string url = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "url", true) == 0)
                .Value;

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();

            // Set name to query string or body data
            url = url ?? data?.name;
            url = (url.Substring(0,1) == "/") ? url.Substring(1) : url;
            string[] sArray = url.Split('/');
            string container_name = sArray[0];
            string file_name = string.Join("/", sArray.Skip<string>(1));

            // Parse the connection string and return a reference to the storage account.
            string BlobStorage = ConfigurationManager.AppSettings.Get("AzureWebJobsStorage");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(BlobStorage);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            // CloudBlobContainer container = blobClient.GetContainerReference("mycontainer");
            CloudBlobContainer container = blobClient.GetContainerReference(container_name);
            container.CreateIfNotExists();

            // Create a new access policy for the account.
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessExpiryTime = DateTime.Now.AddHours(24);
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List;

            var blob = container.GetBlockBlobReference(file_name);

            if (blob.Exists())
            {
                return req.CreateResponse(HttpStatusCode.OK, blob.Uri.AbsoluteUri + container.GetSharedAccessSignature(sasConstraints));
            }
            else
            {
                // activate UploadToBlob func
                string upload_to_blob_url = "https://cntvdemo.azurewebsites.net/api/UploadToBlob?code=k6Gs2NMU35P0Pi9yswNsU3ZKjaAIQ4gos69UyXzg5bdc11c6RsuN5A==&url=" + url;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(upload_to_blob_url);
                request.Method = "GET";
                request.ContentType = "text/html;charset=UTF-8";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                return req.CreateResponse(HttpStatusCode.Found, _CNTV_Domain + url);
            }
        }
    }
}
