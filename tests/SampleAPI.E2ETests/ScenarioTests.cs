using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using ScenarioTests;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SampleAPI.E2ETests
{
    public partial class ScenarioTests
    {
        readonly ITestOutputHelper _testOutputHelper;

        public ScenarioTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        void LogResponse(HttpResponseMessage responseMessage)
        {
            _testOutputHelper.WriteLine($"Received response: {responseMessage}");
        }

        [Scenario(NamingPolicy = ScenarioTestMethodNamingPolicy.Test)]
        public async Task SampleScenario(ScenarioContext scenario)
        {
            // Build a test server to test against, see: https://github.com/dotnet-architecture/eShopOnContainers/blob/dev/src/Services/Ordering/Ordering.FunctionalTests/OrderingScenarioBase.cs
            using var testServer = new TestServer(
                new WebHostBuilder()
                    .UseContentRoot(Path.GetDirectoryName(typeof(ScenarioTests).Assembly.Location))
                    .UseStartup<Startup>()
                );

            // Create a client so that we can interact with our server
            using var testClient = testServer.CreateClient();

            await scenario.Fact("We can communicate with our server", async () =>
            {
                var response = await testClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/"));
                LogResponse(response);
                
                Assert.True(response.IsSuccessStatusCode);
            });

            await scenario.Fact("We can get a forecast from our server for today", async () =>
            {
                var response = await testClient.GetAsync($"/weatherforecast?date={DateTime.Now}");
                LogResponse(response);

                Assert.True(response.IsSuccessStatusCode);
            });

            // Start to share a variable amongst the remaining tests
            var today = DateTime.Now.Date;

            await scenario.Fact("Foreacasts returned are consistent", async () =>
            {
                var result1 = await testClient.GetStringAsync($"/weatherforecast?date={today}");
                var result2 = await testClient.GetStringAsync($"/weatherforecast?date={today}");

                Assert.Equal(result1, result2);
            });

            await scenario.Fact("We receive an Unauthorized response when trying to set a forecast", async () =>
            {
                var response = await testClient.PostAsJsonAsync($"/weatherforecast?date={today}", new WeatherForecast
                {
                    Date = today,
                    TemperatureC = 1337,
                    Summary = "Hottttt"
                });

                LogResponse(response);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            });

            // At this point in the scenario we need an access token so acquire one and configure it with the TestClient
            var accessToken = await testClient.GetStringAsync("/login");
            Assert.NotNull(accessToken);

            if (!testClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", accessToken))
            {
                throw new InvalidOperationException("Unable to set the authorization token");
            }

            await scenario.Fact("We can now set a forecast", async () =>
            {
                var response = await testClient.PostAsJsonAsync($"/weatherforecast?date={today}", new WeatherForecast
                {
                    Date = today,
                    TemperatureC = 1337,
                    Summary = "Hottttt still..."
                });

                LogResponse(response);
                Assert.True(response.IsSuccessStatusCode);
            });

            var sampleDays = Enumerable.Range(0, 7).Select(dayIndex => today.AddDays(dayIndex));

            // Set some sample forecasts for the next 7 days
            foreach (var sampleDate in sampleDays)
            {
                var response = await testClient.PostAsJsonAsync($"/weatherforecast?date={sampleDate}", new WeatherForecast
                {
                    Date = today,
                    TemperatureC = (int)(sampleDate - today).TotalDays,
                    Summary = sampleDate == today ? "Freezing..." : "Still cold, at least warmer than yesterday..."
                });

                response.EnsureSuccessStatusCode();
            }

            // Now that we have some forecasts configured, we want to ensure that they stick
            foreach (var sampleDate in sampleDays.Reverse())
            {
                await scenario.Theory("Ensure that our previously configured forecast is persisted", sampleDate, async () =>
                {
                    var forecast = await testClient.GetFromJsonAsync<WeatherForecast>($"/weatherforecast?date={sampleDate}");
                    Assert.NotNull(forecast);
                    Assert.Equal((int)(sampleDate - today).TotalDays, forecast.TemperatureC);
                });
            }
        }
    }
}
