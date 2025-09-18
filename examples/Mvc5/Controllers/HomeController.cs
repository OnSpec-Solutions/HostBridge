using System.Web.Mvc;

using HostBridge.Examples.Common;

namespace Mvc5.Controllers;

public class HomeController(IMyScoped scoped) : Controller
{
    public ActionResult Index()
    {
        return View(new { Id = scoped.Id.ToString("B") });
    }
}