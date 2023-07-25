using System.Web.Http;

namespace SSO_NonSSO.NETFrameworkWebAPI.Controllers
{
    public class TestsController : ApiController
    {
        //[HttpGet]
        //public IHttpActionResult Get()
        //{
        //    return Ok("Unauthorized Response");
        //}

        //[Route("auth")]
        [HttpGet]
        [Authorize]
        public IHttpActionResult Get2()
        {
            return Ok("Authorized Response");
        }
    }
}