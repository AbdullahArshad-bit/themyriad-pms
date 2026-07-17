using PMS.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;

namespace PMS.Controllers.Api
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [BasicAuthenticationApi]
    public class ApiTransportationController : ApiController
    {

    }
}