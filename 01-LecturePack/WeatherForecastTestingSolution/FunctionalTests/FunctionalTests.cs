using Xunit;
using WeatherForecastRest;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Http;
using CommonLib;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace FunctionalTests
{
    public class FunctionalTests
    {

        private readonly ITestOutputHelper _outputHelper;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="outputHelper">Output helper for debugging</param>
        public FunctionalTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _outputHelper.WriteLine("New Instance!");
        }

        // DEMO: Local testing
        const string EndpointUrlString = "https://localhost:9112/";


        // DEMO: Testing against azure deployed Web API App
        //const string EndpointUrlString = "https://app-lecture01-weatherforecast-test-linux-cscie94.azurewebsites.net/";

        /// <summary>
        /// Test retrieving all weather forecasts
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task PositiveTest_RetrieveAllWeatherForecasts()
        {
            _outputHelper.WriteLine($"The EndpointUrlString is: [{EndpointUrlString}]");

            // Arrange         
            using HttpClient httpClient = new HttpClient();
            WeatherForecastRestClient weatherForecastRestClient = new WeatherForecastRestClient(EndpointUrlString, httpClient);

            //�Act - Make the HTTP GET call
            var  weatherForecastResults = await weatherForecastRestClient.GetAllWeatherForecastsAsync();

            // Assert a result is returned
            Assert.NotNull(weatherForecastResults);

            _outputHelper.WriteLine($"The number of weather forecasts is: [{weatherForecastResults.Count}]");

            // Assert - Each entry has a summary
            int index = 0;
            foreach (var item in weatherForecastResults)
            {
                index++;
                Assert.False(string.IsNullOrWhiteSpace(item.Summary));
            }
        }

        /// <summary>
        /// Test creating a weather forecast
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task PositiveTest_CreateWeatherForecast()
        {
            // Arrange
            using HttpClient httpClient = new HttpClient();
            WeatherForecastRestClient weatherForecastRestClient = new WeatherForecastRestClient(EndpointUrlString, httpClient);
            WeatherForecastCreate weatherForecastInput = new WeatherForecastCreate()
            {
                Date = DateTime.UtcNow,
                Summary = "Test add a weather forecast",
                TemperatureC = 42
            };

            //�Act - Make the HTTP POST call
            WeatherForecastResult? result = await weatherForecastRestClient.CreateWeatherForecastAsync(weatherForecastInput);

            // Assert a result is returned
            Assert.NotNull(result);

            // Verify the item returned matches the input
            Assert.Equal(StatusCodes.Status201Created, weatherForecastRestClient.LastStatusCode);
            Assert.Equal(expected: weatherForecastInput.Summary, result.Summary);
            Assert.Equal(expected: weatherForecastInput.TemperatureC, result.TemperatureC);
            Assert.Equal(expected: weatherForecastInput.Date, result.Date);
        }

        /// <summary>
        /// Test update a weather forecast
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task PositiveTest_UpdateWeatherForecast()
        {
            // Arrange
            using HttpClient httpClient = new HttpClient();
            WeatherForecastRestClient weatherForecastRestClient = new WeatherForecastRestClient(EndpointUrlString, httpClient);
            WeatherForecastCreate weatherForecastInput = new WeatherForecastCreate()
            {
                Date = DateTime.UtcNow,
                Summary = "Test add a weather forecast",
                TemperatureC = 42
            };
            
            // Add the weather forecast entry
            WeatherForecastResult? result = await weatherForecastRestClient.CreateWeatherForecastAsync(weatherForecastInput);
            WeatherForecastUpdate weatherForecastUpdate = new WeatherForecastUpdate()
            {
                Summary = "Test update a weather forecast"
            };


            // Act - Update the entry
            await weatherForecastRestClient.PatchWeatherForecastRouteNameAsync(result.Id, weatherForecastUpdate);
            
            // Assert result are as expected
            Assert.Equal(StatusCodes.Status204NoContent, weatherForecastRestClient.LastStatusCode);

            // Retrieve the updated entry
            WeatherForecast updatedResult = await weatherForecastRestClient.GetWeatherForecastByIdAsync(result.Id);

            // Verify the item returned matches the input
            Assert.Equal(expected: weatherForecastUpdate.Summary, updatedResult.Summary);
            Assert.Equal(expected: weatherForecastInput.TemperatureC, updatedResult.TemperatureC);
            Assert.Equal(expected: weatherForecastInput.Date, updatedResult.Date);
        }

        /// <summary>
        /// Test creating a weather forecast
        /// </summary>
        /// <returns>Task</returns>
        [Theory]
        [InlineData(1)] // Empty summary
        [InlineData(60)] // To large summary
        public async Task PositiveTest_CreateWeatherForecast_MinMaxSummary(int summaryLength)
        {
            // Arrange
            using HttpClient httpClient = new HttpClient();
            WeatherForecastRestClient weatherForecastRestClient = new WeatherForecastRestClient(EndpointUrlString, httpClient);
            WeatherForecastCreate weatherForecastInput = new WeatherForecastCreate()
            {
                Date = DateTime.UtcNow,
                Summary = summaryLength < 0 ? null : new string(c: 'X', summaryLength),
                TemperatureC = 42
            };

            //�Act - Make the HTTP POST call
            WeatherForecastResult? result = await weatherForecastRestClient.CreateWeatherForecastAsync(weatherForecastInput);

            // Verify the item returned matches the input
            Assert.Equal(expected: weatherForecastInput.Summary, result.Summary);
            Assert.Equal(expected: weatherForecastInput.TemperatureC, result.TemperatureC);
            Assert.Equal(expected: weatherForecastInput.Date, result.Date);

            // Verify the location header is not null
            Assert.False(string.IsNullOrWhiteSpace(weatherForecastRestClient.LastLocationHeader));

            // Retrieve the weather forecast using the location header
            var locationHeaderResponse = await httpClient.GetAsync(weatherForecastRestClient.LastLocationHeader);

            // Verify the data was returned using location header url successfully
            Assert.Equal(StatusCodes.Status200OK, (int)locationHeaderResponse.StatusCode);

            // Get the content of the response returned from the request to the location header url
            string locationHeaderResponseString = await locationHeaderResponse.Content.ReadAsStringAsync();

            // Verify that a response was returned
            Assert.False(string.IsNullOrWhiteSpace(locationHeaderResponseString));

            // Verify that the response is the correct type
            WeatherForecast? locationHeaderResponseDeserialized = JsonConvert.DeserializeObject<WeatherForecast>(locationHeaderResponseString);
            Assert.NotNull(locationHeaderResponseDeserialized);
            
            // Verify the item returned matches the input
            Assert.Equal(expected: weatherForecastInput.Summary, locationHeaderResponseDeserialized?.Summary);
            Assert.Equal(expected: weatherForecastInput.TemperatureC, locationHeaderResponseDeserialized?.TemperatureC);
            Assert.Equal(expected: weatherForecastInput.Date, locationHeaderResponseDeserialized?.Date);            
        }

        /// <summary>
        /// Test creating a weather forecast verify the location header
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task PositiveTest_CreateWeatherForecast_LocationHeader()
        {
            // Arrange
            using HttpClient httpClient = new HttpClient();
            WeatherForecastRestClient weatherForecastRestClient = new WeatherForecastRestClient(EndpointUrlString, httpClient);
            WeatherForecastCreate weatherForecastInput = new WeatherForecastCreate()
            {
                Date = DateTime.UtcNow,
                Summary = new string(c: 'X', 60),
                TemperatureC = 41
            };

            //�Act - Make the HTTP POST call
            WeatherForecastResult? result = await weatherForecastRestClient.CreateWeatherForecastAsync(weatherForecastInput);

            // Retrieve the weather forecast using the location header
            var locationHeaderResponse = await httpClient.GetAsync(weatherForecastRestClient.LastLocationHeader);

            // Get the content of the response returned from the request to the location header url
            string locationHeaderResponseString = await locationHeaderResponse.Content.ReadAsStringAsync();

            // Assert - Verify the response based on the location header

            // Assert that a response was returned
            Assert.False(string.IsNullOrWhiteSpace(locationHeaderResponseString));

            // Assert that the response is the correct type
            WeatherForecast? locationHeaderResponseDeserialized = JsonConvert.DeserializeObject<WeatherForecast>(locationHeaderResponseString);
            Assert.NotNull(locationHeaderResponseDeserialized);

            // Assert the item returned matches the input
            Assert.Equal(expected: weatherForecastInput.Summary, locationHeaderResponseDeserialized?.Summary);
            Assert.Equal(expected: weatherForecastInput.TemperatureC, locationHeaderResponseDeserialized?.TemperatureC);
            Assert.Equal(expected: weatherForecastInput.Date, locationHeaderResponseDeserialized?.Date);
        }

        /// <summary>
        /// Test creating a weather forecast
        /// </summary>
        /// <returns>Task</returns>
        [Theory]
        [InlineData(-1)] // Null summary
        [InlineData(0)] // Empty summary
        [InlineData(61)] // To large summary
        public async Task NegativeTest_CreateWeatherForecast_InvalidSummary(int summaryLength)
        {
            // Arrange
            using HttpClient httpClient = new HttpClient();
            WeatherForecastRestClient weatherForecastRestClient = new WeatherForecastRestClient(EndpointUrlString, httpClient);
            WeatherForecastCreate weatherForecastInput = new WeatherForecastCreate()
            {
                Date = DateTime.UtcNow,
                Summary = summaryLength < 0 ? null : new string(c: 'X', summaryLength),
                TemperatureC = 42
            };

            //�Act - Make the HTTP POST call
            ApiException apiException = await Assert.ThrowsAsync<ApiException<ErrorResponse>>(() => weatherForecastRestClient.CreateWeatherForecastAsync(weatherForecastInput));
            
            // Assert 
            Assert.Equal(expected: StatusCodes.Status400BadRequest, actual: apiException.StatusCode);
        }

        /// <summary>
        /// Test creating a weather forecast
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task NegativeTest_reateWeatherForecast_NullSummary()
        {
            try
            {
                // Arrange
                using HttpClient httpClient = new HttpClient();
                WeatherForecastRestClient weatherForecastRestClient = new WeatherForecastRestClient(EndpointUrlString, httpClient);
                WeatherForecastCreate weatherForecastInput = new WeatherForecastCreate()
                { Date = DateTime.UtcNow, Summary = null, TemperatureC = 31 };

                //�Act - Make the HTTP POST call
                WeatherForecastResult? result = await weatherForecastRestClient.CreateWeatherForecastAsync(weatherForecastInput);

            }
            catch (ApiException<ErrorResponse> apiEx)
            {
                Assert.Equal(StatusCodes.Status400BadRequest, apiEx.StatusCode);
                Assert.Equal(ErrorNumbers.MustNotBeNull, apiEx.Result.ErrorNumber);
                Assert.Equal(nameof(WeatherForecastCreate.Summary), apiEx.Result.PropertyName);
            }
        }

        /// <summary>
        /// Test updating a weather forecast using PUT (full replacement)
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task PositiveTest_PutUpdateWeatherForecast()
        {
            // Arrange - Create a weather forecast first
            using HttpClient httpClient = new HttpClient();
            WeatherForecastRestClient weatherForecastRestClient = new WeatherForecastRestClient(EndpointUrlString, httpClient);
            WeatherForecastCreate weatherForecastInput = new WeatherForecastCreate()
            {
                Date = DateTime.UtcNow,
                Summary = "Initial Summary",
                TemperatureC = 20
            };
            
            // Create the initial weather forecast
            WeatherForecastResult? result = await weatherForecastRestClient.CreateWeatherForecastAsync(weatherForecastInput);
            Assert.NotNull(result);

            // Prepare updated data with all fields changed
            WeatherForecastCreate weatherForecastUpdate = new WeatherForecastCreate()
            {
                Date = DateTime.UtcNow.AddDays(1),
                Summary = "Completely Updated Summary",
                TemperatureC = 30
            };

            // Act - Use PUT to fully replace the resource
            await weatherForecastRestClient.PutWeatherForecastAsync(result.Id, weatherForecastUpdate);
            
            // Assert - Verify the update was successful
            Assert.Equal(StatusCodes.Status204NoContent, weatherForecastRestClient.LastStatusCode);

            // Retrieve the updated entry to verify all fields were replaced
            WeatherForecast updatedResult = await weatherForecastRestClient.GetWeatherForecastByIdAsync(result.Id);
            
            // Verify all fields match the updated data
            Assert.Equal(expected: weatherForecastUpdate.Summary, updatedResult.Summary);
            Assert.Equal(expected: weatherForecastUpdate.TemperatureC, updatedResult.TemperatureC);
            Assert.Equal(expected: weatherForecastUpdate.Date, updatedResult.Date);
        }

        /// <summary>
        /// Test creating a weather forecast using PUT with client-provided ID
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task PositiveTest_PutCreateWeatherForecastWithClientId()
        {
            // Arrange
            using HttpClient httpClient = new HttpClient();
            WeatherForecastRestClient weatherForecastRestClient = new WeatherForecastRestClient(EndpointUrlString, httpClient);
            
            // Client provides the ID
            string clientProvidedId = Guid.NewGuid().ToString();
            WeatherForecastCreate weatherForecastInput = new WeatherForecastCreate()
            {
                Date = DateTime.UtcNow,
                Summary = "Created with client-provided ID",
                TemperatureC = 25
            };

            // Act & Assert - PUT returns 201 by throwing ApiException with the result
            ApiException<WeatherForecastResult> apiException = await Assert.ThrowsAsync<ApiException<WeatherForecastResult>>(() => 
                weatherForecastRestClient.PutWeatherForecastAsync(clientProvidedId, weatherForecastInput));
            
            // Verify the creation was successful with 201 status
            Assert.Equal(StatusCodes.Status201Created, apiException.StatusCode);
            Assert.NotNull(apiException.Result);
            Assert.Equal(expected: clientProvidedId, actual: apiException.Result.Id);
            Assert.Equal(expected: weatherForecastInput.Summary, actual: apiException.Result.Summary);
            Assert.Equal(expected: weatherForecastInput.TemperatureC, actual: apiException.Result.TemperatureC);
            Assert.Equal(expected: weatherForecastInput.Date, actual: apiException.Result.Date);

            // Verify the resource can be retrieved
            WeatherForecast retrievedResult = await weatherForecastRestClient.GetWeatherForecastByIdAsync(clientProvidedId);
            Assert.NotNull(retrievedResult);
            Assert.Equal(expected: weatherForecastInput.Summary, actual: retrievedResult.Summary);
        }

        /// <summary>
        /// Test deleting a weather forecast
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task PositiveTest_DeleteWeatherForecast()
        {
            // Arrange - Create a weather forecast to delete
            using HttpClient httpClient = new HttpClient();
            WeatherForecastRestClient weatherForecastRestClient = new WeatherForecastRestClient(EndpointUrlString, httpClient);
            WeatherForecastCreate weatherForecastInput = new WeatherForecastCreate()
            {
                Date = DateTime.UtcNow,
                Summary = "To be deleted",
                TemperatureC = 15
            };
            
            // Create the weather forecast
            WeatherForecastResult? result = await weatherForecastRestClient.CreateWeatherForecastAsync(weatherForecastInput);
            Assert.NotNull(result);

            // Verify it exists
            WeatherForecast verifyExists = await weatherForecastRestClient.GetWeatherForecastByIdAsync(result.Id);
            Assert.NotNull(verifyExists);

            // Act - Delete the weather forecast
            await weatherForecastRestClient.DeleteWeatherForecastAsync(result.Id);
            
            // Assert - Verify the deletion was successful
            Assert.Equal(StatusCodes.Status204NoContent, weatherForecastRestClient.LastStatusCode);

            // Verify the resource no longer exists
            ApiException<ProblemDetails> apiException = await Assert.ThrowsAsync<ApiException<ProblemDetails>>(() => 
                weatherForecastRestClient.GetWeatherForecastByIdAsync(result.Id));
            Assert.Equal(StatusCodes.Status404NotFound, apiException.StatusCode);
        }

        /// <summary>
        /// Test deleting a non-existent weather forecast returns 404
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task NegativeTest_DeleteNonExistentWeatherForecast()
        {
            // Arrange
            using HttpClient httpClient = new HttpClient();
            WeatherForecastRestClient weatherForecastRestClient = new WeatherForecastRestClient(EndpointUrlString, httpClient);
            string nonExistentId = Guid.NewGuid().ToString();

            // Act & Assert - Attempt to delete a non-existent resource
            ApiException<ProblemDetails> apiException = await Assert.ThrowsAsync<ApiException<ProblemDetails>>(() => 
                weatherForecastRestClient.DeleteWeatherForecastAsync(nonExistentId));
            Assert.Equal(StatusCodes.Status404NotFound, apiException.StatusCode);
        }

    }
}