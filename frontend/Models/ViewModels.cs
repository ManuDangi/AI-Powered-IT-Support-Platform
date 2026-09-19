using System.ComponentModel.DataAnnotations;

namespace AIITSupport.Web.Models;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}

public class RegisterViewModel
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}

public class CreateTicketViewModel
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}

public class ActionCommentViewModel
{
    public int TicketId { get; set; }
    public string? Comments { get; set; }
    public string? Reason { get; set; }
}
