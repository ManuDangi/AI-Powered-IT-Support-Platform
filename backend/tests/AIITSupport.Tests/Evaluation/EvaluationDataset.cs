namespace AIITSupport.Tests.Evaluation;

public record EvaluationCase(
    string Name,
    string Title,
    string Description,
    string ExpectedCategory,
    string ExpectedUrgency,
    bool ExpectedEscalation);

public static class EvaluationDataset
{
    public static readonly List<EvaluationCase> Cases =
    [
        // VPN
        new("VPN 01", "Cannot connect to company VPN",
            "I am unable to connect to the company VPN and receive an authentication error.",
            "VPN", "High", false),

        new("VPN 02", "VPN connection drops",
            "My VPN connection disconnects repeatedly while I am working remotely.",
            "VPN", "Medium", false),

        new("VPN 03", "VPN credentials rejected",
            "The company VPN keeps rejecting my valid corporate credentials.",
            "VPN", "High", false),

        new("VPN 04", "VPN client configuration",
            "I need help configuring the company VPN client on my laptop.",
            "VPN", "Medium", false),

        new("VPN 05", "VPN unavailable",
            "The corporate VPN service is completely unavailable and I cannot connect.",
            "VPN", "High", false),

        // Password
        new("Password 01", "Forgot password",
            "I forgot my company account password and need to reset it.",
            "Password", "Medium", false),

        new("Password 02", "Password expired",
            "My corporate password has expired and I cannot sign in.",
            "Password", "High", false),

        new("Password 03", "Password reset request",
            "Please help me reset my employee portal password.",
            "Password", "Medium", false),

        new("Password 04", "Account locked",
            "My account is locked after several incorrect password attempts.",
            "Password", "High", false),

        new("Password 05", "Password change help",
            "I need instructions for changing my corporate password.",
            "Password", "Low", false),

        // Laptop
        new("Laptop 01", "Laptop will not start",
            "My company laptop does not power on when I press the power button.",
            "Laptop", "High", false),

       new("Laptop 02", "Laptop running slowly",
    "My company laptop has become very slow during normal work.",
    "Laptop", "Medium", true),

        new("Laptop 03", "Laptop overheating",
            "My work laptop becomes extremely hot during normal usage.",
            "Laptop", "High", false),

        new("Laptop 04", "Laptop battery issue",
            "The laptop battery drains very quickly and needs frequent charging.",
            "Laptop", "Medium", false),

        new("Laptop 05", "Broken laptop screen",
            "The screen of my company laptop is physically damaged.",
            "Laptop", "High", false),

        // Email
        new("Email 01", "Cannot access company email",
            "I cannot access my company email account even with the correct credentials.",
            "Email", "High", false),

        new("Email 02", "Email not syncing",
            "My company email is not syncing with Outlook.",
            "Email", "Medium", false),

        new("Email 03", "Missing emails",
            "Some expected emails are not appearing in my corporate mailbox.",
            "Email", "Medium", false),

        new("Email 04", "Email setup",
            "I need help configuring my company email on Outlook.",
            "Email", "Low", false),

        new("Email 05", "Mailbox full",
            "My corporate mailbox is full and I cannot send new emails.",
            "Email", "High", false),

        // Software
        new("Software 01", "Need software installed",
            "I need Visual Studio installed on my company laptop.",
            "Software", "Medium", false),

        new("Software 02", "Application crashes",
            "The business application crashes whenever I try to open it.",
            "Software", "High", false),

        new("Software 03", "Software update",
            "I need help updating an approved application to its latest version.",
            "Software", "Low", false),

        new("Software 04", "Application not working",
    "A company application is not working correctly after an update.",
    "Software", "Medium", true),

        new("Software 05", "Install approved tool",
            "Please install an approved development tool on my workstation.",
            "Software", "Low", false),

        // Network
        new("Network 01", "Office network outage",
            "The entire office network is unavailable and employees cannot access internal systems.",
            "Network", "Critical", true),

        new("Network 02", "Internet connection slow",
            "The office internet connection is very slow for multiple employees.",
            "Network", "High", false),

        new("Network 03", "Cannot connect to WiFi",
            "My laptop cannot connect to the company WiFi network.",
            "Network", "High", false),

        new("Network 04", "Network intermittent",
            "The office network disconnects intermittently throughout the day.",
            "Network", "Medium", false),

        new("Network 05", "Network configuration help",
            "I need help configuring the network settings on my workstation.",
            "Network", "Low", false),

        // Access
        new("Access 01", "Application access request",
            "I need access permission for the company's internal HR application.",
            "Access", "Medium", true),

        new("Access 02", "Shared folder access",
            "Please provide me access to the finance department shared folder.",
            "Access", "Medium", true),

        new("Access 03", "Access denied",
            "I receive an access denied message when opening an internal application.",
            "Access", "High", true),

        new("Access 04", "New employee access",
            "A new employee needs access to the standard company applications.",
            "Access", "Medium", true),

        new("Access 05", "Admin access request",
            "I need elevated permissions to perform an approved administrative task.",
            "Access", "High", true),

        // Security
        new("Security 01", "Suspicious login detected",
            "I received an alert about a suspicious login to my company account.",
            "Security", "Critical", true),

        new("Security 02", "Phishing email received",
            "I received a suspicious email asking me to provide my corporate password.",
            "Security", "High", true),

        new("Security 03", "Unknown device login",
            "My account shows a login from an unknown device that I do not recognize.",
            "Security", "Critical", true),

        new("Security 04", "Security alert",
            "The security system reported unusual activity on my workstation.",
            "Security", "High", true),

        new("Security 05", "Lost company device",
            "I lost my company laptop while travelling and need to report it.",
            "Security", "Critical", true),

        // Other
        new("Other 01", "IT support question",
            "I need general information about an IT support process.",
            "Other", "Low", false),

        new("Other 02", "Help desk information",
            "I would like to know how the IT help desk process works.",
            "Other", "Low", false),

        new("Other 03", "General IT question",
            "I have a general technical question that does not fit another support category.",
            "Other", "Low", false),

        new("Other 04", "IT process clarification",
            "Please explain the standard process for requesting technical support.",
            "Other", "Low", false),

       new("Other 05", "General assistance",
    "I need general assistance with an IT-related question.",
    "Other", "Low", true)
    ];
}