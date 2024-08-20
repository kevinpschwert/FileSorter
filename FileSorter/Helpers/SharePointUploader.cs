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

        public static void UploadFile(ClientContext context, string uploadFolderUrl, string uploadFilePath)
        {
            var fileCreationInfo = new FileCreationInformation
            {
                Content = System.IO.File.ReadAllBytes(uploadFilePath),
                Overwrite = true,
                Url = Path.GetFileName(uploadFilePath)
            };
            var targetFolder = context.Web.GetFolderByServerRelativeUrl(uploadFolderUrl);
            var uploadFile = targetFolder.Files.Add(fileCreationInfo);
            context.Load(uploadFile);
            context.ExecuteQuery();
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




            //string accessToken = await GraphAuthProvider.GetAccessTokenAsync(clientId, tenantId, clientSecret);

            //string siteUrl = "https://zealitconsulltants.sharepoint.com/sites/Test";
            //string resource = "https://zealitconsulltants.sharepoint.com/sites/Test";

            //using (ClientContext cc = new AuthenticationManager().GetACSAppOnlyContext(_siteUrl, clientId, clientSecret))
            //{
            //    cc.Load(cc.Web, p => p.Title);
            //    cc.ExecuteQuery();
            //    Console.WriteLine(cc.Web.Title);
            //}


            //string realm = GetRealmFromTargetUrl(new Uri(siteUrl));

            //// Get access token
            //var tokenUrl = $"https://zealitconsulltants.sharepoint.com/sites/Test/_layouts/15/OAuthAuthorize.aspx";
            //var client = new HttpClient();
            //var requestBody = $"grant_type=client_credentials&client_id={clientId}@{realm}&client_secret={clientSecret}&resource={resource}/{realm}";
            //var content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");
            //var response = await client.PostAsync(tokenUrl, content);
            //var responseContent = await response.Content.ReadAsStringAsync();
            //var token = JObject.Parse(responseContent)["access_token"]?.ToString();

            //// Use the token in your SharePoint ClientContext
            //using (var context = new ClientContext(siteUrl))
            //{
            //    context.ExecutingWebRequest += (sender, e) =>
            //    {
            //        e.WebRequestExecutor.WebRequest.Headers["Authorization"] = "Bearer " + token;
            //    };

            //    Web web = context.Web;
            //    List docLib = web.Lists.GetByTitle(documentLibraryName);
            //    context.Load(docLib.RootFolder);
            //    context.ExecuteQuery();

            //    // Upload files code here
            //}

            ////Get the realm for the URL
            //string realm = TokenHelper.GetRealmFromTargetUrl(new Uri(siteUrl));

            ////Get the access token for the URL.  
            //string accessToken = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal, new Uri(siteUrl).Authority, realm).AccessToken;

            ////Create a client context object based on the retrieved access token
            //using (ClientContext cc = TokenHelper.GetClientContextWithAccessToken(siteUrl, accessToken))
            //{
            //    cc.Load(cc.Web, p => p.Title);
            //    cc.ExecuteQuery();
            //    Console.WriteLine(cc.Web.Title);
            //}




            //using (ClientContext context = new OfficeDevPnP.Core.AuthenticationManager().GetAppOnlyAuthenticatedContext(_siteUrl, Id, Secret))
            //{

            //var authManager = new PnP.Framework.AuthenticationManager(Id, tenantId, Secret);

            ////using (ClientContext context = new ClientContext(_siteUrl))
            ////{
            //using (var context = authManager.GetContext(_siteUrl))
            //{
            //    SecureString securePassword = new SecureString();
            //    foreach (char c in _password.ToCharArray()) securePassword.AppendChar(c);
            //    context.Credentials = new NetworkCredential(_username, securePassword);


            //    Web web = context.Web;
            //    List docLib = web.Lists.GetByTitle(documentLibraryName);
            //    context.Load(docLib.RootFolder);
            //    context.ExecuteQuery();

            //    string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

            //    foreach (var file in files)
            //    {
            //        string relativeFilePath = file.Replace(folderPath, "").TrimStart('\\');
            //        string fileUrl = Path.Combine(docLib.RootFolder.ServerRelativeUrl, relativeFilePath).Replace("\\", "/");

            //        using (FileStream fs = new FileStream(file, FileMode.Open))
            //        {
            //            FileCreationInformation fileCreationInfo = new FileCreationInformation
            //            {
            //                ContentStream = fs,
            //                Url = fileUrl,
            //                Overwrite = true
            //            };

            //            Microsoft.SharePoint.Client.File uploadFile = docLib.RootFolder.Files.Add(fileCreationInfo);
            //            context.Load(uploadFile);
            //            context.ExecuteQuery();

            //            Console.WriteLine($"{relativeFilePath} uploaded successfully.");
            //        }
            //    }
            //}
        }

        //public static string GetRealmFromTargetUrl(Uri targetApplicationUri)
        //{
        //    WebRequest request = WebRequest.Create(targetApplicationUri + "/_vti_bin/client.svc");
        //    request.Headers.Add("Authorization: Bearer ");

        //    try
        //    {
        //        using (request.GetResponse())
        //        {
        //        }
        //    }
        //    catch (WebException e)
        //    {
        //        if (e.Response == null)
        //        {
        //            return null;
        //        }

        //        string bearerResponseHeader = e.Response.Headers["WWW-Authenticate"];
        //        if (string.IsNullOrEmpty(bearerResponseHeader))
        //        {
        //            return null;
        //        }

        //        const string bearer = "Bearer realm=\"";
        //        int bearerIndex = bearerResponseHeader.IndexOf(bearer, StringComparison.Ordinal);
        //        if (bearerIndex < 0)
        //        {
        //            return null;
        //        }

        //        int realmIndex = bearerIndex + bearer.Length;

        //        if (bearerResponseHeader.Length >= realmIndex + 36)
        //        {
        //            string targetRealm = bearerResponseHeader.Substring(realmIndex, 36);

        //            Guid realmGuid;

        //            if (Guid.TryParse(targetRealm, out realmGuid))
        //            {
        //                return targetRealm;
        //            }
        //        }
        //    }
        //    return null;
        //}
    }

}
