using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using SimpleWebAPIChatDemo.Settings;

internal class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        }
        );

        // Add services to the container.

        // Note: Don't forget to add this to the csproj file (removing /* and */)
        // Or use the Visual Studio UI settings do do this
        // See slide 48-56 in AzureAppService-01.pptx
        /*
            <!-- DEMO: Enable documentation file so API has user defined documentation via XML comments -->
            <PropertyGroup>
                <GenerateDocumentationFile>true</GenerateDocumentationFile>

            <!-- Disable warning: Missing XML comment for publicly visible type or member 'Type_or_Member'-->
            <NoWarn>1701;1702;1591</NoWarn>
            </PropertyGroup>
        */

        // Add controllers to the services collection
        // This allows the application to handle HTTP requests and provide REST interfaces
        builder.Services.AddControllers();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        // Adds OpenAPI/Swagger support to the application
        builder.Services.AddOpenApi();

        // Bind AISettings from appsettings.json
        AISettings? _aiSettings = builder.Configuration.GetSection("AISettings").Get<AISettings>()!;

        // Validate AISettings to ensure they are not null or empty
        ILogger logger = loggerFactory.CreateLogger("Program");

        if (_aiSettings is null
            || string.IsNullOrWhiteSpace(_aiSettings.DeploymentUri)
            || string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
        {

            if (_aiSettings == null)
            {
                logger.LogCritical("AISettings is null. Please ensure the configuration is present.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_aiSettings.DeploymentUri))
                {
                    logger.LogCritical("AISettings.DeploymentUri is null or empty.");
                }
                if (string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
                {
                    logger.LogCritical("AISettings.ApiKey is null or empty.");
                }
            }
            throw new InvalidOperationException("AISettings validation failed. Check the logs for details.");
        }

        // If validation passes, log success
        logger.LogInformation("AISettings loaded successfully.");

        builder.Services.AddSingleton(_aiSettings);

        // Initialize OpenAI service endpoint and API key credential
        Uri openAIServiceEndpointUri;
        AzureKeyCredential apiKeyCredential;
        openAIServiceEndpointUri = new Uri(_aiSettings.DeploymentUri);
        apiKeyCredential = new AzureKeyCredential(_aiSettings.ApiKey);
        RegisterOpenAIClient(builder, openAIServiceEndpointUri, apiKeyCredential, _aiSettings.DeploymentModelName);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            // Add anything needed only during development here
        }

        // Map OpenAPI endpoints
        // This maps the OpenAPI endpoints to the application
        // allowing the API documentation to be accessible
        app.MapOpenApi();

        // Redirect HTTP requests to HTTPS
        app.UseHttpsRedirection();

        // Enable authorization middleware
        app.UseAuthorization();

        // Map controller routes
        // This maps the controller routes to the application
        // allowing the application to handle HTTP requests and provide REST interfaces
        app.MapControllers();

        // Run the application
        app.Run();
    }

    /// <summary>
    /// Registers the OpenAI client with the specified parameters.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="openAIServiceEndpointUri">The OpenAI service endpoint URI.</param>
    /// <param name="apiKeyCredential">The API key credential.</param>
    /// <param name="deploymentName">The deployment name.</param>
    private static void RegisterOpenAIClient(WebApplicationBuilder builder,
                                         Uri openAIServiceEndpointUri,
                                         AzureKeyCredential apiKeyCredential,
                                         string deploymentName)
    {
        // Register the OpenAI client as a singleton service
        // A singleton service is created once and shared throughout the application's lifetime
        builder.Services.AddSingleton<IChatClient>(services =>
        {
            var azureOpenAiClient = new AzureOpenAIClient(openAIServiceEndpointUri, apiKeyCredential);
            var chatClient = azureOpenAiClient.GetChatClient(deploymentName);
            return chatClient.AsIChatClient();
        });
    }
}

