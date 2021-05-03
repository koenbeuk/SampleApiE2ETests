using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {
        readonly AccessTokenService _accessTokenService;

        public LoginController(AccessTokenService accessTokenService)
        {
            _accessTokenService = accessTokenService;
        }

        [HttpGet, HttpHead]
        public string Get()
        {
            // This simulates a login, obviously never ever ever use this in production....

            return _accessTokenService.Acquire();
        }

    }
}
