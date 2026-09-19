using System.Net;
using System.Text;
using AIITSupport.Application.Exceptions;
using AIITSupport.Domain.Entities;
using AIITSupport.Infrastructure.AI;
using Microsoft.Extensions.Options;
using Xunit;

namespace AIITSupport.Tests;

public class GroqAIServiceTests
{
    private static Ticket CreateTicket()
    {
        return new Ticket
        {
            Id = 100,
            Title = "Cannot connect to VPN",
            Description = "The company VPN is not connecting."
        };
    }

    private static GroqAIService CreateService(
        string responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(
            responseBody,
            statusCode);

        var httpClient = new HttpClient(handler);

        var options = Options.Create(new AIServiceOptions
        {
            BaseUrl = "https://test-groq.local",
            ApiKey = "test-key",
            Model = "openai/gpt-oss-20b",
            TimeoutSeconds = 30,
            MaxRetries = 2
        });

        return new GroqAIService(
            httpClient,
            options,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GroqAIService>.Instance);
    }

    [Fact]
    public async Task ValidResponse_ShouldReturnAnalysisResult()
    {
        var groqResponse = """
        {
          "choices": [
            {
              "message": {
                "content": "{\"category\":\"VPN\",\"urgency\":\"High\",\"confidence\":0.95,\"summary\":\"User cannot connect to VPN.\",\"recommendation\":\"Check VPN settings and credentials.\"}"
              }
            }
          ]
        }
        """;

        var service = CreateService(groqResponse);

        var result = await service.AnalyzeTicketAsync(CreateTicket());

        Assert.Equal("VPN", result.Category);
        Assert.Equal("High", result.Urgency);
        Assert.Equal(0.95, result.Confidence);
        Assert.Equal("User cannot connect to VPN.", result.Summary);
        Assert.Equal(
            "Check VPN settings and credentials.",
            result.Recommendation);
    }

    [Fact]
    public async Task InvalidJsonResponse_ShouldThrowAIProviderException()
    {
        var groqResponse = """
        {
          "choices": [
            {
              "message": {
                "content": "This is not valid JSON"
              }
            }
          ]
        }
        """;

        var service = CreateService(groqResponse);

        var exception = await Assert.ThrowsAsync<AIProviderException>(
            () => service.AnalyzeTicketAsync(CreateTicket()));

        Assert.Contains(
            "failed after 3 attempts",
            exception.Message);
    }

    [Fact]
    public async Task InvalidCategory_ShouldThrowAIProviderException()
    {
        var groqResponse = """
        {
          "choices": [
            {
              "message": {
                "content": "{\"category\":\"RandomCategory\",\"urgency\":\"High\",\"confidence\":0.95,\"summary\":\"Test issue.\",\"recommendation\":\"Investigate issue.\"}"
              }
            }
          ]
        }
        """;

        var service = CreateService(groqResponse);

        var exception = await Assert.ThrowsAsync<AIProviderException>(
            () => service.AnalyzeTicketAsync(CreateTicket()));

        Assert.Contains(
            "failed after 3 attempts",
            exception.Message);
    }

    [Fact]
    public async Task ConfidenceAboveOne_ShouldThrowAIProviderException()
    {
        var groqResponse = """
        {
          "choices": [
            {
              "message": {
                "content": "{\"category\":\"VPN\",\"urgency\":\"High\",\"confidence\":1.5,\"summary\":\"Test issue.\",\"recommendation\":\"Investigate issue.\"}"
              }
            }
          ]
        }
        """;

        var service = CreateService(groqResponse);

        var exception = await Assert.ThrowsAsync<AIProviderException>(
            () => service.AnalyzeTicketAsync(CreateTicket()));

        Assert.Contains(
            "failed after 3 attempts",
            exception.Message);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        private readonly HttpStatusCode _statusCode;

        public FakeHttpMessageHandler(
            string responseBody,
            HttpStatusCode statusCode)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(
                    _responseBody,
                    Encoding.UTF8,
                    "application/json")
            };

            return Task.FromResult(response);
        }
    }
}