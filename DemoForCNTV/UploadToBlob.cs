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
using System.IO;
using System.Text;

namespace DemoForCNTV
{
    public static class UploadToBlob
    {
        [FunctionName("UploadToBlob")]
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
            url = (url.Substring(0, 1) == "/") ? url.Substring(1) : url;
            string[] sArray = url.Split('/');
            string container_name = sArray[0];
            string file_name = string.Join("/", sArray.Skip<string>(1));
            
            // Parse the connection string and return a reference to the storage account.
            string BlobStorage = ConfigurationManager.AppSettings.Get("AzureWebJobsStorage");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(BlobStorage);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(container_name);
            container.CreateIfNotExists();

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(file_name);

            // Create or overwrite the <file_name> blob with contents from URL.
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_CNTV_Domain + url);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();

            blockBlob.UploadFromStream(myResponseStream);
            myResponseStream.Close();

            return req.CreateResponse(HttpStatusCode.OK, "Upload " + file_name + " Down!");
        }
    }
}
