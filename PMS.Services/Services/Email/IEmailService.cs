using PMS.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.Email
{
    public interface IEmailService
    {
        bool SendEmail(string subject, string body, bool isBodyHtml, string to, int From);
        string SendEmail(string subject, string body, bool isBodyHtml, string to, int From, bool IsTest = true);
        void SendEmailAsync(string subject, string body, bool isBodyHtml, string to, int From = 1);

        EmailSender GetEmailSenderById(int id);
        Task SendEmailTaskAsync(string subject, string body, bool isBodyHtml, string to, int From = 0);

        bool SendInvoiceAndPaymentEmail(string subject, string body, bool isBodyHtml, string to, int From, int locationId);

        bool SendEmailWithAttachment(string subject, string body, bool isBodyHtml, string to, int From,
       byte[] attachmentBytes, string attachmentFileName, string attachmentContentType);

        void SendEmailWithAttachmentAsync(string subject, string body, bool isBodyHtml, string to, int From,
       byte[] attachmentBytes, string attachmentFileName, string attachmentContentType);
    }
}
