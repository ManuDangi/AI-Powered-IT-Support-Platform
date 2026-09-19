namespace AIITSupport.Infrastructure.AI;

public class AIServiceOptions
{
    public const string SectionName = "Ai";

    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1/chat/completions";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "openai/gpt-oss-20b";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 2;
}