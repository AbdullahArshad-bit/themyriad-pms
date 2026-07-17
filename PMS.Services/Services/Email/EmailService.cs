using PMS.Common.Classes;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using static PMS.Common.Classes.Enumeration;

namespace PMS.Services.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        public EmailService(UnitOfWork<PMSEntities> _uow)
        {
            uow = _uow;
        }

        public EmailSender GetEmailSenderById(int id)
        {
            var db = uow.Context;

            //var email = uow.GenericRepository<EmailSender>().Context.Where(x=> x.EmailSenderID == id).FirstOrDefault();
            var email = (from emailSender in db.EmailSenders
                         where emailSender.EmailSenderID == id
                         select emailSender
                       ).FirstOrDefault();
            return email;
        }

        public bool SendEmail(string subject, string body, bool isBodyHtml, string to, int From)
        {
            bool ret = false;
            var password = "";
            var EmailSettings = uow.GenericRepository<EmailSetting>().Table.FirstOrDefault();
            if (EmailSettings.EmailEnabled != true)
            {
                return false;
            }
            var sender = GetEmailSenderById((int)From);
            if (sender == null)
            {
                sender = new EmailSender()
                {

                    FromAddress = Common.mailbody.FromEmail,
                    EmailSenderName = Common.mailbody.EmailMuscatDisplayName,
                    EmailPassword = Common.mailbody.FromEmailMuscatPassword,
                    CC = subject.ToLower() == "booking" ? Common.mailbody.MyriadMuscatBookingCCEmail : ""
                };
            }
            else
            {
                password = Common.Security.StringCipher.Decrypt(sender.EmailPassword);
                if (!string.IsNullOrEmpty(password))
                    sender.EmailPassword = password;
            }
            try
            {
                MailMessage message = new MailMessage();

                message.From = new MailAddress(sender.FromAddress, sender.EmailSenderName /*Common.mailbody.EmailDisplayName*/); // from 
                foreach (var To in to.Split(','))
                {
                    message.To.Add(new MailAddress(To));
                }
                if (!string.IsNullOrEmpty(sender.CC))
                    message.CC.Add(sender.CC);
                if (!string.IsNullOrEmpty(sender.BCC))
                    message.Bcc.Add(sender.BCC);
                message.Subject = subject;
                message.IsBodyHtml = isBodyHtml; //to make message body as html  
                message.Body = body;

                //smtp.Timeout = 25000;

                try
                {
                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Port = EmailSettings.EmailServerPort;//Common.mailbody.EmailSmtpPort;
                        smtp.Host = EmailSettings.EmailServer;//Common.mailbody.EmailSmtpHost;
                        smtp.EnableSsl = EmailSettings.UseSSL;//true;

                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(sender.FromAddress, sender.EmailPassword);// from
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                        smtp.Send(message);

                        ret = true;
                    }
                }
                catch (Exception ex)
                {
                    ret = false;
                    //throw ex;
                }
            }
            catch (Exception ex)
            {
                ret = false;
                //throw ex;
            }
            return ret;
        }

        public bool SendEmailWithAttachment(string subject, string body, bool isBodyHtml, string to, int From,
    byte[] attachmentBytes, string attachmentFileName, string attachmentContentType)
        {
            bool ret = false;
            var password = "";
            var EmailSettings = uow.GenericRepository<EmailSetting>().Table.FirstOrDefault();
            if (EmailSettings.EmailEnabled != true)
            {
                return false;
            }

            var sender = GetEmailSenderById((int)From);
            if (sender == null)
            {
                sender = new EmailSender()
                {
                    FromAddress = Common.mailbody.FromEmail,
                    EmailSenderName = Common.mailbody.EmailMuscatDisplayName,
                    EmailPassword = Common.mailbody.FromEmailMuscatPassword,
                    CC = subject.ToLower() == "booking" ? Common.mailbody.MyriadMuscatBookingCCEmail : ""
                };
            }
            else
            {
                password = Common.Security.StringCipher.Decrypt(sender.EmailPassword);
                if (!string.IsNullOrEmpty(password))
                    sender.EmailPassword = password;
            }

            try
            {
                MailMessage message = new MailMessage();
                message.From = new MailAddress(sender.FromAddress, sender.EmailSenderName);

                foreach (var To in to.Split(','))
                {
                    message.To.Add(new MailAddress(To));
                }

                if (!string.IsNullOrEmpty(sender.CC))
                    message.CC.Add(sender.CC);
                if (!string.IsNullOrEmpty(sender.BCC))
                    message.Bcc.Add(sender.BCC);

                message.Subject = subject;
                message.IsBodyHtml = isBodyHtml;
                message.Body = body;

                // Add attachment if provided
                if (attachmentBytes != null && attachmentBytes.Length > 0)
                {
                    using (var stream = new MemoryStream(attachmentBytes))
                    {
                        var attachment = new Attachment(stream, attachmentFileName, attachmentContentType);
                        message.Attachments.Add(attachment);

                        try
                        {
                            using (SmtpClient smtp = new SmtpClient())
                            {
                                smtp.Port = EmailSettings.EmailServerPort;
                                smtp.Host = EmailSettings.EmailServer;
                                smtp.EnableSsl = EmailSettings.UseSSL;
                                smtp.UseDefaultCredentials = false;
                                smtp.Credentials = new NetworkCredential(sender.FromAddress, sender.EmailPassword);
                                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                                smtp.Send(message);
                                ret = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            ret = false;
                            // Log the exception if needed
                        }
                    }
                }
                else
                {
                    // If no attachment, send as regular email
                    try
                    {
                        using (SmtpClient smtp = new SmtpClient())
                        {
                            smtp.Port = EmailSettings.EmailServerPort;
                            smtp.Host = EmailSettings.EmailServer;
                            smtp.EnableSsl = EmailSettings.UseSSL;
                            smtp.UseDefaultCredentials = false;
                            smtp.Credentials = new NetworkCredential(sender.FromAddress, sender.EmailPassword);
                            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                            smtp.Send(message);
                            ret = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        ret = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = false;
            }

            return ret;
        }

        public void SendEmailWithAttachmentAsync(string subject, string body, bool isBodyHtml, string to, int From,
            byte[] attachmentBytes, string attachmentFileName, string attachmentContentType)
        {
            try
            {
                var thread = new System.Threading.Thread(x =>
                {
                    SendEmailWithAttachment(subject, body, isBodyHtml, to, From, attachmentBytes, attachmentFileName, attachmentContentType);
                }) { IsBackground = true };
                thread.SetApartmentState(System.Threading.ApartmentState.STA);
                thread.Start();
            }
            catch (Exception)
            {
            }
        }

        public bool SendInvoiceAndPaymentEmail(string subject, string body, bool isBodyHtml, string to, int From, int locationId)
        {
            bool ret = false;
            var password = "";
            var EmailSettings = uow.GenericRepository<EmailSetting>().Table.FirstOrDefault();
            if (EmailSettings.EmailEnabled != true)
            {
                return false;
            }

            EmailSender sender;

            if (locationId == (int)LocationEnum.Dubai)
            {
                sender = new EmailSender()
                {
                    FromAddress = Common.mailbody.FromEmail,
                    EmailSenderName = Common.mailbody.EmailDisplayName,
                    EmailPassword = Common.mailbody.FromEmailMuscatPassword,
                    CC = subject.ToLower() == "booking" ? Common.mailbody.MyriadBookingCCEmail : ""
                };
            }
            else
            {
                sender = new EmailSender()
                {
                    FromAddress = Common.mailbody.FromEmail,
                    EmailSenderName = Common.mailbody.EmailMuscatDisplayName,
                    EmailPassword = Common.mailbody.FromEmailMuscatPassword,
                    CC = subject.ToLower() == "booking" ? Common.mailbody.MyriadMuscatBookingCCEmail : ""
                };
            }

            password = Common.Security.StringCipher.Decrypt(sender.EmailPassword);
            if (!string.IsNullOrEmpty(password))
                sender.EmailPassword = password;

            try
            {
                MailMessage message = new MailMessage();
                message.From = new MailAddress(sender.FromAddress, sender.EmailSenderName); // From Address

                foreach (var To in to.Split(','))
                {
                    message.To.Add(new MailAddress(To));
                }

                if (!string.IsNullOrEmpty(sender.CC))
                    message.CC.Add(sender.CC);
                if (!string.IsNullOrEmpty(sender.BCC))
                    message.Bcc.Add(sender.BCC);

                message.Subject = subject;
                message.IsBodyHtml = isBodyHtml;
                message.Body = body;

                try
                {
                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Port = EmailSettings.EmailServerPort;
                        smtp.Host = EmailSettings.EmailServer;
                        smtp.EnableSsl = EmailSettings.UseSSL;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(sender.FromAddress, sender.EmailPassword);
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.Send(message);

                        ret = true;
                    }
                }
                catch (Exception ex)
                {
                    ret = false;
                }
            }
            catch (Exception ex)
            {
                ret = false;
            }

            return ret;
        }



        public string SendEmail(string subject, string body, bool isBodyHtml, string to, int From, bool IsTest = true)
        {
            string ret = "";
            var EmailSettings = uow.GenericRepository<EmailSetting>().Table.FirstOrDefault();
            if (EmailSettings.EmailEnabled != true)
            {
                return "Email Gateway is disabled!";
            }
            var sender = GetEmailSenderById((int)From);
            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress(sender.FromAddress, sender.EmailSenderName /*Common.mailbody.EmailDisplayName*/); // from 
                message.To.Add(new MailAddress(to));
                if (!string.IsNullOrEmpty(sender.CC))
                    message.CC.Add(sender.CC);
                if (!string.IsNullOrEmpty(sender.BCC))
                    message.Bcc.Add(sender.BCC);
                message.Subject = subject;
                message.IsBodyHtml = isBodyHtml; //to make message body as html  
                message.Body = body;
                smtp.Port = EmailSettings.EmailServerPort;//Common.mailbody.EmailSmtpPort;
                smtp.Host = EmailSettings.EmailServer;//Common.mailbody.EmailSmtpHost;
                smtp.EnableSsl = EmailSettings.UseSSL;//true;

                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(sender.FromAddress /*Common.mailbody.FromEmail*/, Common.Security.StringCipher.Decrypt(sender.EmailPassword) /*Common.mailbody.FromEmailPassword*/);// from
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                //smtp.Timeout = 25000;

                try
                {
                    smtp.Send(message);
                    ret = "Email Sent Successfully!";
                }
                catch (Exception ex)
                {
                    ret = ex.Message.ToString();
                    //throw ex;
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message.ToString();
                //throw ex;
            }

            return ret;
        }

        public void SendEmailAsync(string subject, string body, bool isBodyHtml, string to, int From = 0)
        {
            try
            {
                var thread = new System.Threading.Thread(x => { SendEmail(subject, body, isBodyHtml, to, From); }) { IsBackground = true };
                thread.SetApartmentState(System.Threading.ApartmentState.STA);
                thread.Start();
            }
            catch (Exception ex)
            {
                //throw ex;
            }
        }
        public async Task SendEmailTaskAsync(string subject, string body, bool isBodyHtml, string to, int From = 0)
        {
            try
            {
                SendEmail(subject, body, isBodyHtml, to, From);

            }
            catch (Exception ex)
            {
                //throw ex;

            }
        }
    }
}
