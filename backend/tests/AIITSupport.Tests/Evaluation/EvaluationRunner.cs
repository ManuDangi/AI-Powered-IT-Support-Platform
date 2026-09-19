using System.Diagnostics;
using AIITSupport.Application.Interfaces;
using AIITSupport.Infrastructure.AI;
using AIITSupport.Infrastructure.Policies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIITSupport.Tests.Evaluation;

public static class EvaluationRunner
{
    public static async Task RunAsync()
    {
        var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("GROQ_API_KEY is not configured.");
            return;
        }

        var options = Options.Create(new AIServiceOptions
        {
            BaseUrl = "https://api.groq.com/openai/v1/chat/completions",
            ApiKey = apiKey,
            Model = "openai/gpt-oss-20b",
            TimeoutSeconds = 30,
            MaxRetries = 2
        });

        using var httpClient = new HttpClient();

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        var logger = loggerFactory.CreateLogger<GroqAIService>();

        var service = new GroqAIService(
            httpClient,
            options,
            logger);

        var policyService = new RuleBasedPolicyService();

        var total = EvaluationDataset.Cases.Count;

        var successfulCases = 0;
        var errorCases = 0;

        var categoryCorrect = 0;
        var urgencyCorrect = 0;

        var escalationCorrect = 0;
        var falseEscalations = 0;
        var missedEscalations = 0;

        var urgencyFailures = new List<string>();
        var escalationFailures = new List<string>();

        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine("========================================");
        Console.WriteLine(" AIITSupport - AI Evaluation");
        Console.WriteLine("========================================");
        Console.WriteLine($"Total cases: {total}");
        Console.WriteLine($"Model: {options.Value.Model}");
        Console.WriteLine();

        for (var i = 0; i < total; i++)
        {
            var testCase = EvaluationDataset.Cases[i];
            var caseWatch = Stopwatch.StartNew();

            try
            {
                var ticket = new AIITSupport.Domain.Entities.Ticket
                {
                    Id = i + 1,
                    Title = testCase.Title,
                    Description = testCase.Description
                };

                var result = await service.AnalyzeTicketAsync(ticket);

                var policyDecision =
                    await policyService.EvaluateAsync(ticket, result);

                caseWatch.Stop();

                successfulCases++;

                var categoryOk = string.Equals(
                    result.Category,
                    testCase.ExpectedCategory,
                    StringComparison.OrdinalIgnoreCase);

                var urgencyOk = string.Equals(
                    result.Urgency,
                    testCase.ExpectedUrgency,
                    StringComparison.OrdinalIgnoreCase);

                var actualEscalation =
                    policyDecision.RequiresHumanReview;

                var escalationOk =
                    actualEscalation == testCase.ExpectedEscalation;

                if (categoryOk)
                {
                    categoryCorrect++;
                }

                if (urgencyOk)
                {
                    urgencyCorrect++;
                }
                else
                {
                    urgencyFailures.Add(
                        $"{testCase.Name} | " +
                        $"Expected: {testCase.ExpectedUrgency} | " +
                        $"Actual: {result.Urgency}");
                }

                if (escalationOk)
                {
                    escalationCorrect++;
                }
                else if (actualEscalation)
                {
                    falseEscalations++;

                    escalationFailures.Add(
                        $"{testCase.Name} | " +
                        $"Expected: false | " +
                        $"Actual: true | " +
                        $"Category: {result.Category} | " +
                        $"Urgency: {result.Urgency} | " +
                        $"Confidence: {result.Confidence:P0}");
                }
                else
                {
                    missedEscalations++;

                    escalationFailures.Add(
                        $"{testCase.Name} | " +
                        $"Expected: true | " +
                        $"Actual: false | " +
                        $"Category: {result.Category} | " +
                        $"Urgency: {result.Urgency} | " +
                        $"Confidence: {result.Confidence:P0}");
                }

                Console.WriteLine(
                    $"[{i + 1:00}/{total}] " +
                    $"Category={(categoryOk ? "PASS" : "FAIL")} " +
                    $"Urgency={(urgencyOk ? "PASS" : "FAIL")} " +
                    $"Escalation={(escalationOk ? "PASS" : "FAIL")} " +
                    $"| Expected: {testCase.ExpectedCategory}/{testCase.ExpectedUrgency} " +
                    $"| EscalationExpected: {testCase.ExpectedEscalation} " +
                    $"| Actual: {result.Category}/{result.Urgency} " +
                    $"| Confidence: {result.Confidence:P0} " +
                    $"| HumanReview: {actualEscalation} " +
                    $"| {caseWatch.ElapsedMilliseconds} ms");
            }
           catch (Exception ex)
{
    caseWatch.Stop();

    errorCases++;

    Console.WriteLine(
        $"[{i + 1:00}/{total}] ERROR | " +
        $"{testCase.Name} | {ex}");

    if (ex.InnerException != null)
    {
        Console.WriteLine(
            $"INNER ERROR: {ex.InnerException}");
    }
}
        }

        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine(" Evaluation Summary");
        Console.WriteLine("========================================");

        Console.WriteLine(
            $"Successful Cases      : {successfulCases}/{total}");

        Console.WriteLine(
            $"Provider Errors       : {errorCases}");

        if (successfulCases > 0)
        {
            Console.WriteLine(
                $"Category Accuracy     : {(double)categoryCorrect / successfulCases:P2}");

            Console.WriteLine(
                $"Urgency Accuracy      : {(double)urgencyCorrect / successfulCases:P2}");

            Console.WriteLine(
                $"Escalation Accuracy   : {(double)escalationCorrect / successfulCases:P2}");

            Console.WriteLine(
                $"False Escalations     : {falseEscalations}");

            Console.WriteLine(
                $"Missed Escalations    : {missedEscalations}");

            Console.WriteLine();
            Console.WriteLine("Urgency Failures");
            Console.WriteLine("----------------------------------------");

            if (urgencyFailures.Count == 0)
            {
                Console.WriteLine("None");
            }
            else
            {
                foreach (var failure in urgencyFailures)
                {
                    Console.WriteLine(failure);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Escalation Failures");
            Console.WriteLine("----------------------------------------");

            if (escalationFailures.Count == 0)
            {
                Console.WriteLine("None");
            }
            else
            {
                foreach (var failure in escalationFailures)
                {
                    Console.WriteLine(failure);
                }
            }
        }
        else
        {
            Console.WriteLine("Category Accuracy     : N/A");
            Console.WriteLine("Urgency Accuracy      : N/A");
            Console.WriteLine("Escalation Accuracy   : N/A");
            Console.WriteLine("False Escalations     : N/A");
            Console.WriteLine("Missed Escalations    : N/A");
        }

        Console.WriteLine(
            $"Total Time            : {stopwatch.Elapsed.TotalSeconds:F2} sec");

        Console.WriteLine(
            $"Average Latency       : {stopwatch.Elapsed.TotalMilliseconds / total:F0} ms");

        Console.WriteLine("========================================");
    }
}