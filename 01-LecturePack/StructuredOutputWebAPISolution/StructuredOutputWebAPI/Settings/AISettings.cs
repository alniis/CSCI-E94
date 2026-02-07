namespace StructuredOutputWebAPI.Settings
{
    public class AISettings
    {
        /// <summary>
        /// Gets or sets the URI for the AI deployment.
        /// </summary>
        public string DeploymentUri { get; set; }
        /// <summary>
        /// Gets or sets the API key for accessing the AI service.
        /// </summary>
        public string ApiKey { get; set; }
        /// <summary>
        /// Gets or sets the name of the deployment model. Default is "gpt-4o-mini".
        /// </summary>
        public string DeploymentModelName { get; set; } = "gpt-5-mini";
        /// <summary>
        /// Gets or sets the temperature for the AI model. Default is 0.7f.
        /// A higher value for temperature will result in more random outputs.
        /// A lower value for temperature will result in more deterministic outputs.
        /// </summary>
        public float Temperature { get; set; } = 1.0f;
        /// <summary>
        /// Gets or sets the top-p value for the AI model. Default is 1.0f.
        /// Higher values (e.g., 0.7–1.0) → More randomness and creativity, allowing for more diverse word choices.
        /// Lower values (e.g., 0.1–0.3) → More deterministic and focused, choosing the highest probability tokens.
        /// Very high values (e.g., 1.5 or more) → Can make the output too chaotic and incoherent.
        /// Temperature adjusts how much the model deviates from picking the highest-probability words.
        /// </summary>
        public float TopP { get; set; } = 1.0f;
        /// <summary>
        /// Gets or sets the maximum number of output tokens. Default is 500.
        /// Range: Between 0 and 1 (where 1 includes all tokens, and lower values restrict choices to high-probability tokens).
        /// Top-p filters out lower-probability words before selecting from the remaining ones.
        /// </summary>
        public int MaxOutputTokens { get; set; } = 500;
    }
}
