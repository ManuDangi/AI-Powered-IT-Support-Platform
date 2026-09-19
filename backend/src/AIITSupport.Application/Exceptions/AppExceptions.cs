namespace AIITSupport.Application.Exceptions;

// Thrown when the AI provider itself fails (timeout, network, 5xx, rate limit)
// after retries are exhausted. Callers should route the ticket to a safe
// failure state instead of crashing the request.
public class AIProviderException : Exception
{
    public AIProviderException(string message, Exception? inner = null) : base(message, inner) { }
}

// Thrown when the AI responded successfully but the JSON did not match the
// expected structured shape (missing fields, confidence out of range, etc.).
// This is treated the same as a provider failure by the workflow: no
// unstructured / unvalidated AI output is ever trusted downstream.
public class AIResponseInvalidException : Exception
{
    public AIResponseInvalidException(string message) : base(message) { }
}

public class InvalidTicketStateException : Exception
{
    public InvalidTicketStateException(string message) : base(message) { }
}
