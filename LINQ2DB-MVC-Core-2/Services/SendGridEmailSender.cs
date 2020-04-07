using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace LINQ2DB_MVC_Core_2.Services
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly string msApiKey;
        private readonly string msFromEmailName;
        private readonly string msFromEmailAddr;
        public SendGridEmailSender(IConfiguration configuration)
        {
            msApiKey = configuration["Authentication:Email:SendGridKey"] ?? "";
            msFromEmailName = configuration["Authentication:Email:FromEmailName"] ?? "";
            msFromEmailAddr = configuration["Authentication:Email:FromEmailAddr"] ?? "";
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Send e-mail through SendGrid
            var client = new SendGridClient(msApiKey);
            var from = new EmailAddress(msFromEmailAddr, msFromEmailName);
            var to = new EmailAddress(email);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlMessage);
            var response = await client.SendEmailAsync(msg);
        }
    }
}
