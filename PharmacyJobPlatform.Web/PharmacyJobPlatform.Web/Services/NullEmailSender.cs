namespace PharmacyJobPlatform.Web.Services
{
    public class NullEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            return Task.CompletedTask;
        }
    }
}
