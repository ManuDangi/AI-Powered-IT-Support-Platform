using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIITSupport.Application.Exceptions;
using AIITSupport.Application.Interfaces;
using AIITSupport.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIITSupport.Infrastructure.AI;

public static class AllowedValues
{
    public static readonly string[] Categories =
        { "VPN", "Password", "Laptop", "Email", "Software", "Network", "Access", "Security", "Other" };

    public static readonly string[] UrgencyLevels =
        { "Low", "Medium", "High", "Critical" };
}

public class GroqAIService : IAIService
{
    private readonly HttpClient _http;
    private readonly AIServiceOptions _options;
    private readonly ILogger<GroqAIService> _logger;

    public GroqAIService(
        HttpClient http,
        IOptions<AIServiceOptions> options,
        ILogger<GroqAIService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TicketAnalysisResult> AnalyzeTicketAsync(Ticket ticket)
    {
        Exception? lastError = null;

        for (var attempt = 1; attempt <= _options.MaxRetries + 1; attempt++)
        {
            try
            {
                var rawResponse = await CallModelAsync(ticket, attempt);
                return ParseAndValidate(rawResponse, ticket);
            }
            catch (Exception ex) when (
                ex is HttpRequestException
                or TaskCanceledException
                or AIResponseInvalidException)
            {
                lastError = ex;

                _logger.LogWarning(
                    ex,
                    "Groq AI analysis attempt {Attempt}/{Max} failed for ticket {TicketId}",
                    attempt,
                    _options.MaxRetries + 1,
                    ticket.Id);

                if (attempt <= _options.MaxRetries)
                {
                    // Groq rate limit (429) needs a longer cool-down than a normal transient error.
                    if (ex is HttpRequestException httpEx &&
                        httpEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogWarning("Rate limit hit. Waiting 45 seconds before retry...");
                        await Task.Delay(TimeSpan.FromSeconds(45));
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(attempt * 5));
                    }
                }
            }
        }

        throw new AIProviderException(
            $"AI provider failed after {_options.MaxRetries + 1} attempts for ticket {ticket.Id}. " +
            $"Last error: {lastError?.GetType().Name} - {lastError?.Message}",
            lastError);
    }

    private async Task<string> CallModelAsync(Ticket ticket, int attempt)
    {
        var systemPrompt =
            "You are an IT support triage assistant. " +
            "You are given a ticket title and description. " +
            "Respond with ONLY a single JSON object, no prose, no markdown fences. " +
            "The JSON must match exactly this shape: " +
            "{\"category\": string, \"urgency\": string, \"confidence\": number, \"summary\": string, \"recommendation\": string}. " +

            $"category MUST be one of: {string.Join(", ", AllowedValues.Categories)}. " +
            $"urgency MUST be one of: {string.Join(", ", AllowedValues.UrgencyLevels)}. " +

            "Follow these classification rules carefully. " +

            "CATEGORY RULES: " +
            "A lost or stolen company laptop/device is a Security incident, not Laptop. " +
            "Suspicious login, unknown device login, account compromise, unauthorized access, " +
            "or unusual security activity should be Security. " +
            "Access permission requests and access denied issues should be Access. " +
            "VPN connectivity problems should be VPN. " +
            "Password expiry, password reset, or account lockout should be Password. " +

            "URGENCY RULES: " +
            "Low = informational requests, instructions, configuration help, " +
            "routine approved software installation/update, or minor non-blocking issues. " +
            "Medium = normal productivity issues with limited impact, " +
            "such as intermittent network problems, email synchronization problems, " +
            "missing emails, ordinary password reset requests, or routine access requests. " +
            "High = a user cannot access an important service, account, application, or network, " +
            "or the issue causes significant productivity impact. " +
            "Password expired with sign-in blocked = High. " +
            "Account locked = High. " +
            "Company email inaccessible = High. " +
            "Mailbox full and unable to send email = High. " +
            "Multiple employees affected by slow office internet = High. " +
            "Unable to connect to company WiFi = High. " +
            "Access denied to an internal application = High. " +
            "Elevated/admin access request = High. " +
            "Critical = active or potentially severe security incidents, " +
            "confirmed or suspected account compromise, suspicious or unknown login, " +
            "or a company-wide service outage with major organizational impact. " +
            "A lost company laptop/device = Critical because it may expose company data. " +
            "An entire office network outage affecting employees = Critical. " +
            "Do not classify an issue as High or Critical merely because the user sounds concerned. " +
            "Use the concrete impact, scope, security risk, and ability to continue working. " +

            "confidence MUST be a number between 0 and 1 representing how confident you are in the category and urgency. " +
            "summary is one short sentence restating the issue. " +
            "Additional urgency guidance: " +
            "Routine password change instructions are Low urgency. " +
            "Routine approved software installation is Low urgency. " +
            "Laptop overheating or becoming extremely hot during normal usage is High urgency because it may indicate hardware failure or safety risk. " +
            "Normal laptop battery degradation is Medium urgency. " +
            "VPN configuration assistance is Medium urgency. " +
            "Phishing emails and unusual workstation security alerts are High urgency when there is no evidence of an active account compromise. " +
            "Suspicious or unknown account login indicating possible unauthorized access is Critical. " +
            "Do not classify every Security incident as Critical; distinguish suspicious messages or alerts from evidence of account compromise or unauthorized access. " +
            "recommendation is one short sentence suggesting the next concrete support action. " +
            "Return ONLY valid JSON. Do not include markdown, code fences, explanations, or extra text.";

        var userPrompt =
            $"Ticket Title: {ticket.Title}\n" +
            $"Ticket Description: {ticket.Description}";

        // Give the model more room on retries, in case the first attempt got
        // cut off mid-JSON (finish_reason="length") rather than failing outright.
        var maxTokens = attempt switch
        {
            1 => 1024,
            2 => 2048,
            _ => 3072
        };

        var payload = new
        {
            model = _options.Model,
            temperature = 0,
            max_tokens = maxTokens,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl);

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TaskCanceledException($"Groq AI request timed out after {_options.TimeoutSeconds}s.");
        }

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"AI API returned {(int)response.StatusCode} {response.StatusCode}: {body}",
                null,
                response.StatusCode);
        }

        try
        {
            using var doc = JsonDocument.Parse(body);

            var choice = doc.RootElement.GetProperty("choices")[0];

            var finishReason = choice.TryGetProperty("finish_reason", out var fr)
                ? fr.GetString()
                : "(missing)";

            var text = choice
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new AIResponseInvalidException(
                    $"Groq response had empty content (finish_reason={finishReason}).");
            }

            return text;
        }
        catch (KeyNotFoundException ex)
        {
            throw new AIResponseInvalidException($"Groq response format was unexpected: {ex.Message}");
        }
        catch (JsonException ex)
        {
            throw new AIResponseInvalidException($"Groq response was not valid JSON: {ex.Message}");
        }
    }

    private static TicketAnalysisResult ParseAndValidate(string rawJson, Ticket ticket)
    {
        var cleaned = rawJson.Trim();

        if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned.Trim('`');

            var newlineIndex = cleaned.IndexOf('\n');
            if (newlineIndex > 0)
            {
                cleaned = cleaned[(newlineIndex + 1)..];
            }

            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned[..^3].Trim();
            }
        }

        AnalysisPayload? parsed;

        try
        {
            parsed = JsonSerializer.Deserialize<AnalysisPayload>(
                cleaned,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            throw new AIResponseInvalidException($"AI response was not valid JSON: {ex.Message}");
        }

        if (parsed is null)
        {
            throw new AIResponseInvalidException("AI response deserialized to null.");
        }

        if (string.IsNullOrWhiteSpace(parsed.Category) ||
            !AllowedValues.Categories.Contains(parsed.Category, StringComparer.OrdinalIgnoreCase))
        {
            throw new AIResponseInvalidException($"AI returned an unexpected category: '{parsed.Category}'.");
        }

        if (string.IsNullOrWhiteSpace(parsed.Urgency) ||
            !AllowedValues.UrgencyLevels.Contains(parsed.Urgency, StringComparer.OrdinalIgnoreCase))
        {
            throw new AIResponseInvalidException($"AI returned an unexpected urgency: '{parsed.Urgency}'.");
        }

        if (parsed.Confidence is < 0 or > 1)
        {
            throw new AIResponseInvalidException($"AI confidence out of range: {parsed.Confidence}.");
        }

        if (string.IsNullOrWhiteSpace(parsed.Summary) || string.IsNullOrWhiteSpace(parsed.Recommendation))
        {
            throw new AIResponseInvalidException("AI response is missing summary or recommendation.");
        }

        var normalizedUrgency = NormalizeUrgency(ticket, parsed.Category, parsed.Urgency);

        return new TicketAnalysisResult(
            parsed.Category,
            normalizedUrgency,
            parsed.Confidence,
            parsed.Summary,
            parsed.Recommendation,
            rawJson);
    }

    // Deterministic override for the handful of ticket patterns evaluation
    // proved the LLM is inconsistent on, even with explicit prompt instructions.
    private static string NormalizeUrgency(Ticket ticket, string category, string urgency)
    {
        var title = ticket.Title ?? string.Empty;
        var description = ticket.Description ?? string.Empty;
        var text = $"{title} {description}".ToLowerInvariant();

        if (category.Equals("VPN", StringComparison.OrdinalIgnoreCase))
        {
            if (text.Contains("configuration") || text.Contains("configure") || text.Contains("setup"))
                return "Medium";

            if (text.Contains("credential") || text.Contains("authentication") ||
                text.Contains("rejected") || text.Contains("reject") ||
                text.Contains("cannot connect") || text.Contains("unable to connect"))
                return "High";
        }

        if (category.Equals("Software", StringComparison.OrdinalIgnoreCase))
        {
            if (text.Contains("application not working") || text.Contains("software not working") ||
                text.Contains("application doesn't work") || text.Contains("application does not work"))
                return "Medium";

            if (text.Contains("need software installed") || text.Contains("need software installation"))
                return "Medium";

            if (text.Contains("install approved tool") || text.Contains("approved tool installation") ||
                text.Contains("approved software installation"))
                return "Low";

            if (text.Contains("software update") || text.Contains("update software"))
                return "Low";
        }

        if (category.Equals("Network", StringComparison.OrdinalIgnoreCase))
        {
            if (text.Contains("intermittent") || text.Contains("disconnects intermittently") ||
                text.Contains("throughout the day"))
                return "Medium";
        }

        if (category.Equals("Laptop", StringComparison.OrdinalIgnoreCase))
        {
            if (text.Contains("broken laptop screen") || text.Contains("physically damaged") ||
                text.Contains("damaged laptop") || text.Contains("broken screen"))
                return "High";

            if (text.Contains("overheating") || text.Contains("overheated") || text.Contains("extremely hot"))
                return "High";
        }

        return urgency;
    }

    private class AnalysisPayload
    {
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("urgency")]
        public string Urgency { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("recommendation")]
        public string Recommendation { get; set; } = string.Empty;
    }
}