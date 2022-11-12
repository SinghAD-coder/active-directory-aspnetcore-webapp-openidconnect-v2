using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System.Diagnostics;
using System.Threading.Tasks;
using WebApp_OpenIDConnect_DotNet.Models;

namespace WebApp_OpenIDConnect_DotNet.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        ITokenAcquisition _tokenAcquisition;

        public HomeController(ILogger<HomeController> logger)//, ITokenAcquisition tokenAcquisition)
        {
            _logger = logger;
            //_tokenAcquisition = tokenAcquisition;
        }


        public async Task<IActionResult> Index()
        {
            try
            {
                var idToken = await HttpContext.GetTokenAsync("id_token");
                var accessToken = await HttpContext.GetTokenAsync("access_token");

                //string[] scopes = new string[] { "user.read" };
                //var token = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);

                //accessToken = await HttpContext.GetTokenAsync("access_token");
            }
            catch (System.Exception eXc)
            {
                
            }

            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}