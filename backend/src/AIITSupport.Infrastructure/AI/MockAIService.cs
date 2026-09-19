using System.Text.Json;
using AIITSupport.Application.Interfaces;
using AIITSupport.Domain.Entities;

namespace AIITSupport.Infrastructure.AI;

public class MockAIService : IAIService
{
    public Task<TicketAnalysisResult> AnalyzeTicketAsync(Ticket ticket)
    {
        var title = ticket.Title?.ToLowerInvariant() ?? string.Empty;
        var description = ticket.Description?.ToLowerInvariant() ?? string.Empty;

        var text = $"{title} {description}";

        var category = "Other";
        var urgency = "Medium";
        var confidence = 0.92;
        var summary = "The ticket requires IT support investigation.";
        var recommendation = "A support agent should review the issue and provide the appropriate resolution.";

        // Password-related ticket
        if (text.Contains("password") || text.Contains("forgot password"))
        {
            category = "Password";
            urgency = "Medium";
            confidence = 0.95;
            summary = "The user is unable to access their account because of a password-related issue.";
            recommendation = "Verify the user's identity and guide them through the password reset process.";
        }
        // VPN-related ticket
        else if (text.Contains("vpn"))
        {
            category = "VPN";
            urgency = "High";
            confidence = 0.93;
            summary = "The user is experiencing a VPN connectivity issue.";
            recommendation = "Check VPN connectivity, credentials, and network configuration.";
        }
        // Security-related ticket
        else if (text.Contains("security") ||
                 text.Contains("hack") ||
                 text.Contains("phishing") ||
                 text.Contains("malware"))
        {
            category = "Security";
            urgency = "Critical";
            confidence = 0.98;
            summary = "The ticket indicates a potential security-related incident.";
            recommendation = "Escalate the incident to the security team for immediate investigation.";
        }
        // Access-related ticket
        else if (text.Contains("access") ||
                 text.Contains("permission") ||
                 text.Contains("unauthorized"))
        {
            category = "Access";
            urgency = "High";
            confidence = 0.94;
            summary = "The user is experiencing an access or permission-related issue.";
            recommendation = "Verify the user's access rights and required permissions.";
        }
        // Laptop-related ticket
        else if (text.Contains("laptop") ||
                 text.Contains("computer") ||
                 text.Contains("system"))
        {
            category = "Laptop";
            urgency = "Medium";
            confidence = 0.91;
            summary = "The user is experiencing an issue with their laptop or computer.";
            recommendation = "Perform basic hardware and software diagnostics on the user's system.";
        }
        // Email-related ticket
        else if (text.Contains("email") ||
                 text.Contains("outlook") ||
                 text.Contains("mail"))
        {
            category = "Email";
            urgency = "Medium";
            confidence = 0.94;
            summary = "The user is experiencing an email-related issue.";
            recommendation = "Check the user's mailbox, email configuration, and connectivity.";
        }
        // Network-related ticket
        else if (text.Contains("network") ||
                 text.Contains("internet") ||
                 text.Contains("wifi"))
        {
            category = "Network";
            urgency = "High";
            confidence = 0.93;
            summary = "The user is experiencing a network connectivity issue.";
            recommendation = "Check network connectivity, adapter configuration, and network availability.";
        }
        // Software-related ticket
        else if (text.Contains("software") ||
                 text.Contains("application") ||
                 text.Contains("app"))
        {
            category = "Software";
            urgency = "Medium";
            confidence = 0.90;
            summary = "The user is experiencing a software or application issue.";
            recommendation = "Check the application installation, configuration, and available updates.";
        }

        var rawResponse = JsonSerializer.Serialize(new
        {
            category,
            urgency,
            confidence,
            summary,
            recommendation
        });

        var result = new TicketAnalysisResult(
            category,
            urgency,
            confidence,
            summary,
            recommendation,
            rawResponse);

        return Task.FromResult(result);
    }
}