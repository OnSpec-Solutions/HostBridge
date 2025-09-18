using System.Web.Http;
using System.Web.Http.Description;

using HostBridge.Examples.Common;

namespace WebApi2.Controllers;

[RoutePrefix("api/scoped")]
public class ScopedController(IMyScoped scoped) : ApiController
{
    [Route("")]
    [HttpGet]
    [ResponseType(typeof(IMyScoped))]
    public IHttpActionResult GetScopedId()
    {
        return Ok(scoped);
    }
}