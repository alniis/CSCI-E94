using CommonLib;
using Microsoft.AspNetCore.Mvc;

namespace WeatherForecastTesting.Controllers
{
    /// <summary>
    /// Provides fake weather forecast information
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")] // See: https://en.wikipedia.org/wiki/Media_type    
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

        private readonly ILogger<WeatherForecastController> _logger;
        private static readonly Dictionary<string, WeatherForecast> _weatherForecasts = new Dictionary<string, WeatherForecast>();

        private const string GetWeatherForecastsRouteName = "GetAllWeatherForecasts";
        private const string GetWeatherForecastRouteName = "GetWeatherForecastById";
        private const string CreateWeatherForecastRouteName = "CreateWeatherForecast";
        private const string PatchWeatherForecastRouteName = "PatchWeatherForecastRouteName";
        private const string PutWeatherForecastRouteName = "PutWeatherForecast";
        private const string DeleteWeatherForecastRouteName = "DeleteWeatherForecast";

        /// <summary>
        /// Instance constructor 
        /// </summary>
        /// <param name="logger">The logger instance to log to</param>
        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initialize static data
        /// </summary>
        /// <remarks>
        /// Static constructors are only called once and called before any other method is called
        /// </remarks>
        static WeatherForecastController()
        {
            var randomWeatherForecastsToAdd = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray();

            foreach (var weatherForecast in randomWeatherForecastsToAdd)
            {
                _weatherForecasts.Add(Guid.NewGuid().ToString(), weatherForecast);
            }
        }

        /// <summary>
        /// Provides a randomly generated set of weather forecasts
        /// </summary>
        /// <returns>A list of weather forecasts</returns>
        /// <remarks>
        /// Sample request:
        /// 
        /// GET /weatherforecast
        /// 
        /// </remarks>
        /// <response code="200">Indicates the request was successful</response>
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<WeatherForecastResult>))]
        [HttpGet(Name = GetWeatherForecastsRouteName)]
        public IEnumerable<WeatherForecastResult> GetAllWeatherForecasts()
        {
            return (from weatherForecast in _weatherForecasts
                    select
                    new WeatherForecastResult()
                    {
                        Id = weatherForecast.Key,
                        Date = weatherForecast.Value.Date,
                        Summary = weatherForecast.Value.Summary,
                        TemperatureC = weatherForecast.Value.TemperatureC
                    }).ToArray();
        }

        /// <summary>
        /// Retrieves the weather forecast by id
        /// </summary>
        /// <param name="id">The Id of the weather forecast</param>
        /// <returns>The weather forecast identified by id</returns>        
        /// <response code="200">Indicates the request was successful</response>
        /// <response code="404">Indicates the weather forecast was not found</response>
        [ProducesResponseType(type: typeof(WeatherForecast), statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
        [ProducesResponseType(statusCode: StatusCodes.Status500InternalServerError)]
        [HttpGet("{id}", Name = GetWeatherForecastRouteName)]
        public ActionResult<WeatherForecast> GetAWeatherForecastById(string id)
        {
            try
            {
                if (id.Contains("BadRobot"))
                {
                    throw new Exception("Error simulation");
                }

                if (_weatherForecasts.ContainsKey(id))
                {
                    return _weatherForecasts[id];
                }
            }
            catch (Exception ex)
            {
                string causalityId = Guid.NewGuid().ToString();
                _logger.LogCritical(exception: ex, message: "The causality ID is {id}", args: causalityId);
                // Don't return raw exception information to the caller
                // Note: An internally referenceable causality id that could be tied to internally logged information
                //       is recommended so developers can find and resolve the root cause of the issue.
                return StatusCode(StatusCodes.Status500InternalServerError, $"We are sorry experiencing technical difficulties at this time! Provide this number to tech support {causalityId}");
            }

            return NotFound();
        }

        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
        [HttpPatch("{id}", Name = PatchWeatherForecastRouteName)]
        public ActionResult Patch(string id, [FromBody] WeatherForecastUpdate weatherForecastUpdate)
        {
            if (!_weatherForecasts.ContainsKey(id))
            {
                return NotFound();
            }

            // Update the entry with values provided, skip update where null
            _weatherForecasts[id].Date = weatherForecastUpdate.Date ?? _weatherForecasts[id].Date;
            _weatherForecasts[id].Summary = weatherForecastUpdate.Summary ?? _weatherForecasts[id].Summary;
            _weatherForecasts[id].TemperatureC = weatherForecastUpdate.TemperatureC ?? _weatherForecasts[id].TemperatureC;

            return NoContent();
        }

        /// <summary>
        /// Creates a new weather forecast entry and adds it to the list of weather forecasts
        /// </summary>
        /// <param name="WeatherForecastCreate">The weather forecast to ad</param>
        /// <response code="201">Indicates the weather forecast was added successfully</response>
        [ProducesResponseType(type: typeof(WeatherForecastResult), statusCode: StatusCodes.Status201Created)]
        [ProducesResponseType(type: typeof(ErrorResponse), statusCode: StatusCodes.Status400BadRequest)]
        [HttpPost(Name = CreateWeatherForecastRouteName)]
        public ActionResult Post([FromBody] WeatherForecastCreate WeatherForecastCreate)
        {
            // Validate input
            if (WeatherForecastCreate == null)
            {
                return BadRequest(new ErrorResponse()
                {
                    ErrorMessage = "Input body must not be null",
                    ErrorNumber = ErrorNumbers.MustNotBeNull,
                    PropertyName = nameof(WeatherForecastCreate)
                });
            }

            if (string.IsNullOrWhiteSpace(WeatherForecastCreate.Summary))
            {
                return BadRequest(new ErrorResponse()
                {
                    ErrorMessage = "Input must not be null",
                    ErrorNumber = ErrorNumbers.MustNotBeNull,
                    PropertyName = nameof(WeatherForecastCreate.Summary)
                });
            }

            WeatherForecast weatherForecast = new WeatherForecast()
            {
                Date = WeatherForecastCreate.Date,
                Summary = WeatherForecastCreate.Summary,
                TemperatureC = WeatherForecastCreate.TemperatureC
            };

            string newKeyAdded = Guid.NewGuid().ToString();
            _weatherForecasts.Add(newKeyAdded, weatherForecast);

            WeatherForecastResult weatherForecastResult = new WeatherForecastResult()
            {
                Id = newKeyAdded,
                Date = WeatherForecastCreate.Date,
                Summary = WeatherForecastCreate.Summary,
                TemperatureC = WeatherForecastCreate.TemperatureC
            };

            return CreatedAtRoute(GetWeatherForecastRouteName, routeValues: new { id = newKeyAdded }, value: weatherForecastResult);
        }

        /// <summary>
        /// Updates an existing weather forecast with the full payload (idempotent update)
        /// or creates a new weather forecast with a client-provided ID
        /// </summary>
        /// <param name="id">The ID of the weather forecast to update or create</param>
        /// <param name="weatherForecastCreate">The complete weather forecast data</param>
        /// <returns>NoContent for update, Created for new resource</returns>
        /// <remarks>
        /// PUT is used for two scenarios:
        /// 1. Full update - Replaces entire resource, requires all fields
        /// 2. Create with client-provided ID - Client specifies the resource identifier
        /// 
        /// Sample request:
        /// 
        /// PUT /api/weatherforecast/123e4567-e89b-12d3-a456-426614174000
        /// {
        ///     "date": "2026-01-20T12:00:00Z",
        ///     "temperatureC": 25,
        ///     "summary": "Warm"
        /// }
        /// 
        /// </remarks>
        /// <response code="204">Indicates the weather forecast was updated successfully</response>
        /// <response code="201">Indicates the weather forecast was created successfully with client-provided ID</response>
        /// <response code="400">Indicates invalid input data</response>
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
        [ProducesResponseType(type: typeof(WeatherForecastResult), statusCode: StatusCodes.Status201Created)]
        [ProducesResponseType(type: typeof(ErrorResponse), statusCode: StatusCodes.Status400BadRequest)]
        [HttpPut("{id}", Name = PutWeatherForecastRouteName)]
        public ActionResult Put(string id, [FromBody] WeatherForecastCreate weatherForecastCreate)
        {
            // Validate input
            if (weatherForecastCreate == null)
            {
                return BadRequest(new ErrorResponse()
                {
                    ErrorMessage = "Input body must not be null",
                    ErrorNumber = ErrorNumbers.MustNotBeNull,
                    PropertyName = nameof(weatherForecastCreate)
                });
            }

            if (string.IsNullOrWhiteSpace(weatherForecastCreate.Summary))
            {
                return BadRequest(new ErrorResponse()
                {
                    ErrorMessage = "Input must not be null",
                    ErrorNumber = ErrorNumbers.MustNotBeNull,
                    PropertyName = nameof(weatherForecastCreate.Summary)
                });
            }

            // Check if resource exists
            bool isUpdate = _weatherForecasts.ContainsKey(id);

            // Create the weather forecast object
            WeatherForecast weatherForecast = new WeatherForecast()
            {
                Date = weatherForecastCreate.Date,
                Summary = weatherForecastCreate.Summary,
                TemperatureC = weatherForecastCreate.TemperatureC
            };

            if (isUpdate)
            {
                // PUT for update - Replace the entire resource
                _weatherForecasts[id] = weatherForecast;
                return NoContent();
            }
            else
            {
                // PUT for create with client-provided ID
                _weatherForecasts.Add(id, weatherForecast);

                WeatherForecastResult weatherForecastResult = new WeatherForecastResult()
                {
                    Id = id,
                    Date = weatherForecastCreate.Date,
                    Summary = weatherForecastCreate.Summary,
                    TemperatureC = weatherForecastCreate.TemperatureC
                };

                return CreatedAtRoute(GetWeatherForecastRouteName, routeValues: new { id = id }, value: weatherForecastResult);
            }
        }

        /// <summary>
        /// Deletes a weather forecast by ID
        /// </summary>
        /// <param name="id">The ID of the weather forecast to delete</param>
        /// <returns>NoContent if deleted, NotFound if resource doesn't exist</returns>
        /// <remarks>
        /// Sample request:
        /// 
        /// DELETE /api/weatherforecast/123e4567-e89b-12d3-a456-426614174000
        /// 
        /// </remarks>
        /// <response code="204">Indicates the weather forecast was deleted successfully</response>
        /// <response code="404">Indicates the weather forecast was not found</response>
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
        [HttpDelete("{id}", Name = DeleteWeatherForecastRouteName)]
        public ActionResult Delete(string id)
        {
            // Check if the resource exists
            if (!_weatherForecasts.ContainsKey(id))
            {
                return NotFound();
            }

            // Remove the resource
            _weatherForecasts.Remove(id);

            return NoContent();
        }
    }
}