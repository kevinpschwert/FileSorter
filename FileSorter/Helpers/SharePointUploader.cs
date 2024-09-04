//using System.Net;
using System.Security;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using System.Net;
using FileSorter.Models;

namespace FileSorter.Helpers
{

    public class SharePointUploader
    {
        private string _siteUrl;
        private string _username;
        private string _password;
        private readonly IConfiguration _configuration;

        public SharePointUploader(string siteUrl, string username, string password, IConfiguration configuration)
        {
            _siteUrl = siteUrl;
            _username = username;
            _password = password;
            _configuration = configuration;
        }

        public async Task UploadFolder(string folderPath, string documentLibraryName)
        {
            var TokenEndpoint = _configuration["SharePointOnline:TokenEndpoint"];
            var ClientID = _configuration["SharePointOnline:client_id"];
            var ClientSecret = _configuration["SharePointOnline:client_secret"];
            var resource = _configuration["SharePointOnline:resource"];
            var GrantType = _configuration["SharePointOnline:grant_type"];
            var Tenant = _configuration["SharePointOnline:tenant"];
            TokenEndpoint = string.Format(TokenEndpoint, Tenant);



            var keyValues = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", GrantType),
                new KeyValuePair<string, string>("client_id", ClientID),
                new KeyValuePair<string, string>("client_secret", ClientSecret),
                new KeyValuePair<string, string>("resource", resource),
                new KeyValuePair<string, string>("tenant", Tenant),
            };

            HttpContent content = new FormUrlEncodedContent(keyValues);

            var httpClient = new HttpClient();
            var response = httpClient.PostAsync(TokenEndpoint, content).Result;
            var token = response.Content.ReadAsStringAsync().Result;
            var accessToken = (JsonConvert.DeserializeObject<AccessToken>(token)).access_token;


            var SiteDataEndPoint = _configuration["SharePointOnline:SiteDataEndPoint"];

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            response = httpClient.GetAsync(SiteDataEndPoint).Result;
            var siteData = response.Content.ReadAsStringAsync().Result;
            var sharepointSite = JsonConvert.DeserializeObject<SharePointSite>(siteData);


            var ListsEndPoint = _configuration["SharePointOnline:ListsEndPoint"];
            ListsEndPoint = string.Format(ListsEndPoint, sharepointSite.id);


            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            response = httpClient.GetAsync(ListsEndPoint).Result;
            var listData = response.Content.ReadAsStringAsync().Result;
            var sharePointList = JsonConvert.DeserializeObject<SharePointList>(listData);
            var listid = sharePointList.value.FirstOrDefault(obj => obj.displayName == "Infor Services Bidpacks").id;


            var ListDataEndPoint = _configuration["SharePointOnline:ListDataByFilter"];
            ListDataEndPoint = string.Format(ListDataEndPoint, sharepointSite.id, listid);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            response = httpClient.GetAsync(ListDataEndPoint).Result;

            //Below logic is to handle TooManyRequests Error. We wait for seconds mentioned in Header with name "Retry-After" and try to call the endpoint again.
            int maxRetryCount = 3;
            int retriesCount = 0;

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                do
                {
                    // Determine the retry after value - use the "Retry-After" header
                    var retryAfterInterval = Int32.Parse(response.Headers.GetValues("Retry-After").FirstOrDefault());

                    //we get retryAfterInterval in seconds. We need to pass milliseconds to Thread.Sleep method, hence we multiply retryAfterInterval with 1000
                    System.Threading.Thread.Sleep(retryAfterInterval * 1000);
                    response = httpClient.GetAsync(ListDataEndPoint).Result;
                    retriesCount += 1;
                } while (response.StatusCode == HttpStatusCode.TooManyRequests && retriesCount <= maxRetryCount);
            }

            var ListData = response.Content.ReadAsStringAsync().Result;

            //Updating List fields
            var ListFieldsUpdateEndPoint = _configuration["SharePointOnline:ListFieldsUpdateEndPoint"];
            ListFieldsUpdateEndPoint = string.Format(ListFieldsUpdateEndPoint, sharepointSite.id, listid, "ItemId");

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var sharePointObject = new
            {
                field1 = "value1",
                field2 = "value2"
            };

            string strSharePointObject = JsonConvert.SerializeObject(sharePointObject, Newtonsoft.Json.Formatting.Indented);
            var httpContent = new StringContent(strSharePointObject);
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var updateResponse = httpClient.PatchAsync(ListFieldsUpdateEndPoint, httpContent);
        }
    }

}
