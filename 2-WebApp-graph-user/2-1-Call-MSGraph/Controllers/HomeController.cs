/*
Interesting Notes: 

 1. In the Startup.cs, we're instucting the framework to use oidc + we're attempting to obtain an access token for the initial scopes
 2. These initial scopes are the ones that will be shown to the user their consent - this should always be bare minimum.
 3. Whenever you add a scope in the API Permissions blade, you'll NOT see a consent for those scopes unless until YOU ask for it in your action methods:
    a. The scope you're interested in must be specified in the AuthorizeForScopes[scopes = new string[] {"chat.readwrite"}] of the action method
    b. In the action method, even if you do NOT specify the scopes (an empty array), when obtaining an access token, you'd still see all the
       scopes in the token, because these were the scopes which the user had granted the permissions to. So the point is, the scopes array with values in it is useless.
 4. In the Authentication blade, notice that both ID and Access tokens checkboxes are turned off, but even then we're able to obtain both!
    a. This may be because of the fact of the code that's configured to sign the user in. Looks like the library does this for us.
    b. However, in the 1-Org code which is only for authentication (openid), you'll have to set the ID token, otherwise you'll get an error.
 5. The point is if you're specifying scopes on Azure, you must also specify in your app 

 6. Once a given user has granted permissions during the consent process, nothing happens if those corresponding scopes are removed from the app!. 
    a. During addition of new scopes, they're asked, but NOT during removal (then how's this achieved?)
    b. You can go the Users blade in AAD, and then you Review Permissions, but in order to revoke, you'll have to run a PS script.
 */




using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WebApp_OpenIDConnect_DotNet_graph.Models;

namespace WebApp_OpenIDConnect_DotNet_graph.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly GraphServiceClient _graphServiceClient;

        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;
        readonly ITokenAcquisition _tokenAcquisition;

        private string[] _graphScopes;

        public HomeController(ILogger<HomeController> logger,
                            IConfiguration configuration,
                            GraphServiceClient graphServiceClient,
                            MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler,
                            ITokenAcquisition tokenAcquistion)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient;
            this._consentHandler = consentHandler;

            _tokenAcquisition = tokenAcquistion;

            // Capture the Scopes for Graph that were used in the original request for an Access token (AT) for MS Graph as
            // they'd be needed again when requesting a fresh AT for Graph during claims challenge processing
            _graphScopes = configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');
        }

        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public IActionResult Index()
        {
            return View();
        }

        //[AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        //[AuthorizeForScopes(Scopes = new string[] { "user.read", "calendars.readwrite" })]
        //[AuthorizeForScopes(Scopes = new string[] { "calendars.readwrite", "chat.readwrite" })]
        [AuthorizeForScopes(Scopes = new string[] { "contacts.readwrite" })]
        public async Task<IActionResult> Profile()
        {
            User currentUser = null;

            try
            {
                var idToken = await HttpContext.GetTokenAsync("id_token");
                var accessToken = await HttpContext.GetTokenAsync("access_token");

                //string[] scopes = new string[] { "user.read" };
                //string[] scopes = new string[] { "user.read", "calendars.readwrite" };
                string[] scopes = new string[] { "chat.readwrite" };
                var token = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);

                accessToken = await HttpContext.GetTokenAsync("access_token");

                currentUser = await _graphServiceClient.Me.Request().GetAsync();
            }
            // Catch CAE exception from Graph SDK
            catch (ServiceException svcex) when (svcex.Message.Contains("Continuous access evaluation resulted in claims challenge"))
            {
                try
                {
                    Console.WriteLine($"{svcex}");
                    string claimChallenge = WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(svcex.ResponseHeaders);
                    _consentHandler.ChallengeUser(_graphScopes, claimChallenge);
                    return new EmptyResult();
                }
                catch (Exception ex2)
                {
                    _consentHandler.HandleException(ex2);
                }
            }

            try
            {
                // Get user photo
                using (var photoStream = await _graphServiceClient.Me.Photo.Content.Request().GetAsync())
                {
                    byte[] photoByte = ((MemoryStream)photoStream).ToArray();
                    ViewData["Photo"] = Convert.ToBase64String(photoByte);
                }
            }
            catch (Exception pex)
            {
                Console.WriteLine($"{pex.Message}");
                ViewData["Photo"] = null;
            }

            ViewData["Me"] = currentUser;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string GetSecretFromKeyVault()
        {
            string uri = Environment.GetEnvironmentVariable("KEY_VAULT_URI");
            SecretClient client = new SecretClient(new Uri(uri), new DefaultAzureCredential());

            Response<KeyVaultSecret> secret = client.GetSecretAsync("Graph-App-Secret").Result;

            return secret.Value.Value;
        }
    }
}