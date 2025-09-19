using System.Web.Http;
using System.Web.Http.Description;
using HostBridge.Abstractions;
using HostBridge.Core;

namespace OwinComposite.Controllers;

[RoutePrefix("api/correlation")]
public class CorrelationController(ICorrelationAccessor accessor) : ApiController
{
    [Route("")]
    [HttpGet]
    [ResponseType(typeof(object))]
    public IHttpActionResult Get()
    {
        return Ok(new { Header = Constants.CorrelationHeaderName, Id = accessor.CorrelationId });
    }
}