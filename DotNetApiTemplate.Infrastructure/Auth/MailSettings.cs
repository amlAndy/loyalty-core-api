namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class MailSettings
{
    public string? DevelopmentOverrideRecipient { get; set; }
    public string PasswordResetSubject { get; set; } = "Password reset request";
    public string PasswordResetBodyTemplate { get; set; } = "{0}, This is a password reset email. Please ignore this if you have not requested a reset. Use this link to continue: {1}";
    public string PasswordResetApplicationName { get; set; } = "Unitas Wholesale";
    public string PasswordResetSupportEmail { get; set; } = string.Empty;
    public string PasswordResetSupportTeamName { get; set; } = "IT Team";
    public string PrimaryColor { get; set; } = "#4A2683";
    public string FrontendBaseUrl { get; set; } = string.Empty;
    public string SmtpServer { get; set; } = "smtps://smtp.office365.com:587";
    public string SmtpUserName { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string? FromName { get; set; }
}
