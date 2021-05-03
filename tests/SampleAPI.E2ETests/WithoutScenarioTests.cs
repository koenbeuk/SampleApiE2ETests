using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SampleAPI.E2ETests
{
    public class WithoutScenarioTests : IDisposable
    {
        readonly ITestOutputHelper _testOutputHelper;
        readonly TestServer _testServer;
        readonly HttpClient _testClient;

        public WithoutScenarioTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;

            _testServer = new TestServer(
                new WebHostBuilder()
                    .UseContentRoot(Path.GetDirectoryName(typeof(ScenarioTests).Assembly.Location))
                    .UseStartup<Startup>()
                );

            _testClient = _testServer.CreateClient();
        }

        void LogResponse(HttpResponseMessage responseMessage)
        {
            _testOutputHelper.WriteLine($"Received response: {responseMessage}");
        }

        async Task AuthorizeTestClient()
        {
            var accessToken = await _testClient.GetStringAsync("/login");
            Assert.NotNull(accessToken);

            if (!_testClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", accessToken))
            {
                throw new InvalidOperationException("Unable to set the authorization token");
            }
        }

        [Fact]
        public async Task We_can_communicate_with_our_server()
        {
            var response = await _testClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/"));
            LogResponse(response);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task We_can_get_a_forecast_from_our_server_for_today()
        {
            var response = await _testClient.GetAsync($"/weatherforecast?date={DateTime.Now}");
            LogResponse(response);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Foreacasts_returned_are_consistent()
        {
            var today = DateTime.Now.Date;

            var result1 = await _testClient.GetStringAsync($"/weatherforecast?date={today}");
            var result2 = await _testClient.GetStringAsync($"/weatherforecast?date={today}");

            Assert.Equal(result1, result2);
        }

        [Fact]
        public async Task We_receive_an_Unauthorized_response_when_trying_to_set_a_forecast()
        {
            var today = DateTime.Now.Date;

            var response = await _testClient.PostAsJsonAsync($"/weatherforecast?date={today}", new WeatherForecast
            {
                Date = today,
                TemperatureC = 1337,
                Summary = "Hottttt"
            });

            LogResponse(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task We_can_now_set_a_forecast()
        {
            await AuthorizeTestClient();

            var today = DateTime.Now.Date;

            var response = await _testClient.PostAsJsonAsync($"/weatherforecast?date={today}", new WeatherForecast
            {
                Date = today,
                TemperatureC = 1337,
                Summary = "Hottttt still..."
            });

            LogResponse(response);
            Assert.True(response.IsSuccessStatusCode);
        }

        public static IEnumerable<object[]> SampleDays()
            => Enumerable.Range(0, 7).Select(dayIndex => DateTime.Now.Date.AddDays(dayIndex)).Select(x => new object[] { x });

        async Task SetupSampleDays()
        {
            var today = DateTime.Now.Date;

            // Set some sample forecasts for the next 7 days
            foreach (var sampleDate in SampleDays().Select(x => (DateTime)x[0]))
            {
                var response = await _testClient.PostAsJsonAsync($"/weatherforecast?date={sampleDate}", new WeatherForecast
                {
                    Date = today,
                    TemperatureC = (int)(sampleDate - today).TotalDays,
                    Summary = sampleDate == today ? "Freezing..." : "Still cold, at least warmer than yesterday..."
                });

                response.EnsureSuccessStatusCode();
            }
        }

        [Theory]
        [MemberData(nameof(SampleDays))]
        public async Task Ensure_that_our_previously_configured_forecast_is_persisted(DateTime sampleDate)
        {
            var today = DateTime.Now.Date;

            await AuthorizeTestClient();
            await SetupSampleDays();

            var forecast = await _testClient.GetFromJsonAsync<WeatherForecast>($"/weatherforecast?date={sampleDate}");
            Assert.NotNull(forecast);
            Assert.Equal((int)(sampleDate - today).TotalDays, forecast.TemperatureC);
        }

        public void Dispose()
        {
            _testClient.Dispose();
            _testServer.Dispose();
        }
    }
}
