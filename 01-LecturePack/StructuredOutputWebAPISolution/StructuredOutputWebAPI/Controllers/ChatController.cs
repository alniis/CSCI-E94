using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using NJsonSchema;
using StructuredOutputWebAPI.Settings;
using System.Net.Mime;
using System.Text.Json;

namespace StructuredOutputWebAPI.Controllers
{
    /// <summary>
    /// Enumeration for demo types.
    /// </summary>
    public enum DemoType
    {
        Demo01 = 1,
        Demo02 = 2
    }

    [ApiController]
    [Route("[controller]")]
    public class ChatController : ControllerBase
    {

        public class KeyPhrasesResponse
        {
            public List<string> Phrases { get; set; } = [];
        }

        private readonly ILogger<ChatController> _logger;
        private readonly IChatClient _chatClient;
        private readonly AISettings _aISettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatController"/> class.
        /// </summary>
        /// <param name="chatClient">The chat client to interact with the AI model.</param>
        /// <param name="aISettings">The settings for the AI model.</param>
        /// <param name="logger">The logger instance for logging.</param>
        public ChatController(IChatClient chatClient,
                              AISettings aISettings,
                              ILogger<ChatController> logger)
        {
            _logger = logger;
            _chatClient = chatClient;
            _aISettings = aISettings;
        }

        /// <summary>
        /// Post a chat message to the AI model and get a response.
        /// </summary>
        /// <param name="userPrompt">The input prompt to send to the AI model.</param>
        /// <param name="demoToRun">The demo type to use. Accepts "1", "2", "Demo01", or "Demo02". Defaults to Demo01.</param>
        /// <returns>The response from the AI model as a string.</returns>
        [HttpPost(Name = "PostChat")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<KeyPhrasesResponse>> Post([FromBody] string userPrompt, [FromQuery] DemoType demoToRun = DemoType.Demo01)
        {
            JsonSchema schema = JsonSchema.FromType<KeyPhrasesResponse>();
            string jsonSchemaString = schema.ToJson();

            JsonElement jsonSchemaElement = JsonDocument.Parse(jsonSchemaString).RootElement;

            ChatResponseFormatJson chatResponseFormatJson = ChatResponseFormat.ForJsonSchema(jsonSchemaElement, "ChatResponse", "Chat response schema");

            // Create chat options using settings from AISettings
            ChatOptions chatOptions = new ChatOptions()
            {
                Temperature = _aISettings.Temperature, // Controls the randomness of the response
                TopP = _aISettings.TopP, // Controls the diversity of the response
                MaxOutputTokens = _aISettings.MaxOutputTokens, // Maximum number of tokens in the response
                ResponseFormat = chatResponseFormatJson // Format the response as JSON
            };

            ChatResponse responseCompletion;

            try
            {
                // Select demo based on query string parameter
                // ASP.NET Core automatically binds the query string parameter to the enum
                switch (demoToRun)
                {
                    case DemoType.Demo01:
                        responseCompletion = await Demo01PromptEnhancement(userPrompt, chatOptions);
                        break;
                    case DemoType.Demo02:
                        responseCompletion = await Demo02SystemPromptToProvideContext(userPrompt, chatOptions);
                        break;
                    default:
                        responseCompletion = await Demo01PromptEnhancement(userPrompt, chatOptions);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AI model");
                return this.StatusCode(500, $"Error calling AI model: {ex.Message}");
            }

            KeyPhrasesResponse response;

            try
            {
                // Log the raw response for debugging
                _logger.LogInformation("Raw AI response: {Response}", responseCompletion.Text ?? "(null)");

                // Check if response text is null or empty
                if (string.IsNullOrWhiteSpace(responseCompletion.Text))
                {
                    _logger.LogError("AI model returned null or empty response");
                    return this.StatusCode(500, "AI model returned null or empty response");
                }

                response = JsonSerializer.Deserialize<KeyPhrasesResponse>(responseCompletion.Text)!;

                // Validate that the response was successfully deserialized
                if (response == null)
                {
                    _logger.LogError("Failed to deserialize response - result was null");
                    return this.StatusCode(500, "Failed to deserialize response from AI model");
                }

                // Return the response text or a 500 error the LLM is unable to create
                // a response or the payload can't be deserialized. 
                return response;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize response from AI model. Response text: {ResponseText}", responseCompletion.Text ?? "(null)");
                return this.StatusCode(500, "Failed to deserialize response from AI model");
            }
        }

        /// <summary>
        /// Enhances the prompt to provide context to the AI model.
        /// </summary>
        /// <param name="prompt">The input prompt to send to the AI model.</param>
        /// <param name="chatOptions">The chat options to configure the AI model.</param>
        /// <returns>The response from the AI model.</returns>
        private async Task<ChatResponse> Demo01PromptEnhancement(string prompt, ChatOptions chatOptions)
        {
            string enhancedPrompt = $"Identify and return a JSON list of the most important 3 key phrases from the following text: {prompt}";
            ChatResponse responseCompletion = await _chatClient.GetResponseAsync(enhancedPrompt, options: chatOptions);
            return responseCompletion;
        }

        /// <summary>
        /// Uses a system message to provide context to the AI model.
        /// </summary>
        /// <param name="prompt">The input prompt to send to the AI model.</param>
        /// <param name="chatOptions">The chat options to configure the AI model.</param>
        /// <returns>The response from the AI model.</returns>
        private async Task<ChatResponse> Demo02SystemPromptToProvideContext(string prompt, ChatOptions chatOptions)
        {
            List<ChatMessage> messages = new List<ChatMessage>();
            messages.Add(new ChatMessage(ChatRole.System, "You will identify and return a JSON list of the most important 3 key phrases from the users input"));
            messages.Add(new ChatMessage(ChatRole.User, prompt));
            // Send the prompt to the AI model and get the response
            ChatResponse responseCompletion = await _chatClient.GetResponseAsync(messages, options: chatOptions);
            return responseCompletion;
        }
    }
}
