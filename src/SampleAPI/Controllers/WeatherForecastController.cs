using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SampleAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        readonly WeatherForecastService _forecastService;
        readonly AccessTokenService _accessTokenService;

        public WeatherForecastController(WeatherForecastService forecastService, AccessTokenService accessTokenService)
        {
            _forecastService = forecastService;
            _accessTokenService = accessTokenService;
        }

        [HttpGet, HttpHead]
        public WeatherForecast Get([Required]DateTime date)
        {
            return _forecastService.Get(date);
        }

        [HttpPost]
        public ActionResult Post([Required] DateTime date, [FromBody]WeatherForecast forecast)
        {
            if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            {
                return Unauthorized("An Authorization header is expected");
            }
            else if (!_accessTokenService.IsAuthorized(authorizationHeader.ToString()))
            {
                return Unauthorized("The authorization provided is invalid");
            }

            _forecastService.Set(date, forecast);
            return Ok();
        }
    }
}
