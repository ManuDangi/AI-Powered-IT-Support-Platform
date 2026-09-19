using System.Net.Http.Headers;
using System.Net.Http.Json;
using AIITSupport.Application.DTOs;

namespace AIITSupport.Web.Services;

// Every call to the backend Web API goes through here. The frontend never
// touches EF Core / the database directly - it's a pure HTTP client of the
// same API that Swagger talks to, matching the "MVC as a separate frontend"
// approach from the project roadmap.
public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    private void Attach(string? token)
    {
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    // ---------- Auth ----------

    public async Task<(bool Success, AuthResponse? Result, string? Error)> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        if (!response.IsSuccessStatusCode)
            return (false, null, await ReadError(response));

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return (true, result, null);
    }

    public async Task<(bool Success, AuthResponse? Result, string? Error)> RegisterAsync(string name, string email, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/register", new RegisterRequest(name, email, password));
        if (!response.IsSuccessStatusCode)
            return (false, null, await ReadError(response));

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return (true, result, null);
    }

    // ---------- Tickets ----------

    public async Task<List<TicketResponse>> GetTicketsAsync(string token)
    {
        Attach(token);
        var response = await _http.GetAsync("/api/tickets");
        if (!response.IsSuccessStatusCode) return new List<TicketResponse>();
        return await response.Content.ReadFromJsonAsync<List<TicketResponse>>() ?? new List<TicketResponse>();
    }

    public async Task<TicketResponse?> GetTicketAsync(string token, int id)
    {
        Attach(token);
        var response = await _http.GetAsync($"/api/tickets/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TicketResponse>();
    }

    public async Task<(bool Success, TicketResponse? Result, string? Error)> CreateTicketAsync(string token, string title, string description)
    {
        Attach(token);
        var response = await _http.PostAsJsonAsync("/api/tickets", new CreateTicketRequest(title, description));
        if (!response.IsSuccessStatusCode)
            return (false, null, await ReadError(response));

        var result = await response.Content.ReadFromJsonAsync<TicketResponse>();
        return (true, result, null);
    }

    // ---------- AI ----------

    public async Task<(bool Success, AIAnalysisResponse? Result, string? Error)> AnalyzeAsync(string token, int ticketId)
    {
        Attach(token);
        var response = await _http.PostAsync($"/api/tickets/{ticketId}/analyze", null);
        if (!response.IsSuccessStatusCode)
            return (false, null, await ReadError(response));

        var result = await response.Content.ReadFromJsonAsync<AIAnalysisResponse>();
        return (true, result, null);
    }

    public async Task<AIAnalysisResponse?> GetAnalysisAsync(string token, int ticketId)
    {
        Attach(token);
        var response = await _http.GetAsync($"/api/tickets/{ticketId}/analysis");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AIAnalysisResponse>();
    }

    // ---------- Workflow ----------

    public async Task<(bool Success, ProcessResponse? Result, string? Error)> ProcessAsync(string token, int ticketId)
    {
        Attach(token);
        var response = await _http.PostAsync($"/api/tickets/{ticketId}/process", null);
        if (!response.IsSuccessStatusCode)
            return (false, null, await ReadError(response));

        var result = await response.Content.ReadFromJsonAsync<ProcessResponse>();
        return (true, result, null);
    }

    public async Task<(bool Success, string? Error)> ApproveAsync(string token, int ticketId, string? comments)
    {
        Attach(token);
        var response = await _http.PostAsJsonAsync($"/api/tickets/{ticketId}/approve", new ApprovalRequest(comments));
        return response.IsSuccessStatusCode ? (true, null) : (false, await ReadError(response));
    }

    public async Task<(bool Success, string? Error)> RejectAsync(string token, int ticketId, string? comments)
    {
        Attach(token);
        var response = await _http.PostAsJsonAsync($"/api/tickets/{ticketId}/reject", new RejectRequest(comments));
        return response.IsSuccessStatusCode ? (true, null) : (false, await ReadError(response));
    }

    public async Task<(bool Success, string? Error)> EscalateAsync(string token, int ticketId, string reason)
    {
        Attach(token);
        var response = await _http.PostAsJsonAsync($"/api/tickets/{ticketId}/escalate", new EscalateRequest(reason));
        return response.IsSuccessStatusCode ? (true, null) : (false, await ReadError(response));
    }

    // ---------- Audit ----------

    public async Task<List<AuditLogResponse>> GetAuditLogsAsync(string token)
    {
        Attach(token);
        var response = await _http.GetAsync("/api/audit-logs?take=200");
        if (!response.IsSuccessStatusCode) return new List<AuditLogResponse>();
        return await response.Content.ReadFromJsonAsync<List<AuditLogResponse>>() ?? new List<AuditLogResponse>();
    }

    private static async Task<string> ReadError(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            if (body != null && body.TryGetValue("message", out var msg))
                return msg?.ToString() ?? response.ReasonPhrase ?? "Request failed.";
        }
        catch { /* fall through to generic message below */ }

        return $"{(int)response.StatusCode} {response.ReasonPhrase}";
    }
}
