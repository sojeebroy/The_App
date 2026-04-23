
namespace The_App.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string toEmail, string toName, string verificationLink);

    }
}
