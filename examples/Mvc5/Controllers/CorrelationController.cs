using System.Web.Mvc;
using HostBridge.Abstractions;
using HostBridge.Core;

namespace Mvc5.Controllers;

public class CorrelationController(ICorrelationAccessor accessor) : Controller
{
    public ActionResult Index()
    {
        var id = accessor.CorrelationId ?? "<null>";
        return Content($"{Constants.CorrelationHeaderName}={id}", "text/plain");
    }
}