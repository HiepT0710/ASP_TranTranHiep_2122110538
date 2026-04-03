using Microsoft.AspNetCore.Mvc;

namespace TranTranHiep_2122110538.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class HomeController : Controller
{
    [HttpGet]
    [Route("/")]
    [Route("Home/Index")]
    public IActionResult Index() => View();
}
