using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels.CorrespondenceViewModels
{
    public class EmailSendersListVM
    {
        public int EmailSenderID { get; set; }
        public string EmailSenderName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string FromAddress { get; set; }
        public string EmailSenderPassword { get; set; }
        public string FromName { get; set; }
        public string CC { get; set; }
        public string BCC { get; set; }
        public string ReplyToAddress { get; set; }
        public string EmailSignature { get; set; }
    }

    public class AddEmailSendersVM
    {
        public int EmailSenderID { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string EmailSenderName { get; set; }
        
        [Display(Name = "Description")]
        public string EmailSenderDescription { get; set; }

        [Required]
        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Required, EmailAddress]
        [Display(Name = "From Email")]
        [RegularExpression(@"^[A-Za-z0-9](([_\.\-]?[a-zA-Z0-9]+)*)@([A-Za-z0-9]+)(([\.\-‌​]?[a-zA-Z0-9]+)*)\.([A-Za-z]{2,})$", ErrorMessage = "From Email is not valid")]

        //[DataType(DataType.EmailAddress, ErrorMessage ="E-mail is not valid")]

        public string FromAddress { get; set; }

        [Required]
        [Display(Name = "From Friendly Name")]
        public string FromName { get; set; }

        [Required]
        [Display(Name = "Email Password")]
        public string EmailPassword { get; set; }

        [Required]
        [Compare("EmailPassword")]
        [Display(Name = "Retype Email Password")]
        public string ConfirmEmailPassword { get; set; }
        
        [Display(Name = "CC Addresses")]
        //[RegularExpression(@"^[A-Za-z0-9](([_\.\-]?[a-zA-Z0-9]+)*)@([A-Za-z0-9]+)(([\.\-‌​]?[a-zA-Z0-9]+)*)\.([A-Za-z]{2,})$", ErrorMessage = "CC Addresses is not valid")]
        public string CC { get; set; }

        [Display(Name = "BCC Addresses")]
        //[RegularExpression(@"^[A-Za-z0-9](([_\.\-]?[a-zA-Z0-9]+)*)@([A-Za-z0-9]+)(([\.\-‌​]?[a-zA-Z0-9]+)*)\.([A-Za-z]{2,})$", ErrorMessage = "BCC Addresses is not valid")]
        public string BCC { get; set; }

        [Display(Name = "Reply To Address")]
        [RegularExpression(@"^[A-Za-z0-9](([_\.\-]?[a-zA-Z0-9]+)*)@([A-Za-z0-9]+)(([\.\-‌​]?[a-zA-Z0-9]+)*)\.([A-Za-z]{2,})$", ErrorMessage = "Reply to Address is not valid")]
        public string ReplyToAddress { get; set; }

        [Display(Name = "Signature")]
        public string EmailSignature { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class EmailMessagesListVM
    {
        public int EmailMessageID { get; set; }
        public string EmailMessageName { get; set; }
        public string EmailMessageDescription { get; set; }
        public string EmailMessageSubject { get; set; }
        public string EmailMessageSender { get; set; }
        public int EmailMessageSenderID { get; set; }

        public bool IsActive { get; set; }
        public string EmailMessageBody { get; set; }
        public int ActionId { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }


    }

    public class AddEmailMessageVM
    {
        public int EmailMessageID { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string EmailMessageName { get; set; }
        
        [Display(Name = "Description")]
        public string EmailMessageDescription { get; set; }

        [Required]
        [Display(Name = "Subject")]
        public string EmailMessageSubject { get; set; }

        public List<SelectListVM> EmailMessageSendersList { get; set; }

        [Required]
        [Display(Name = "Sender")]
        public int EmailSenderID { get; set; }

        [Required]
        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Required]
        [Display(Name = "Email Body")]
        public string EmailMessageBody { get; set; }
        [Required]
        [Display(Name = "Action")]
        public int ActionId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public int? LocationId { get; set; }
    }

    public class AddEmailSettingsVM
    {
        public int EmailSettingsID { get; set; }

        [Required]
        [Display(Name = "Email Enabled")]
        public bool EnableEmail { get; set; }

        [Required]
        [Display(Name = "Email Server")]
        public string EmailServer { get; set; }

        [Required]
        [Display(Name = "Email Server Port")]
        public int EmailServerPort { get; set; }

        [Required]
        [Display(Name = "Use SSL")]
        public bool UseSSL { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public int? LocationId { get; set; }
    }

    public class AddEmailSchedulerVM
    {
        public int ID { get; set; }
        public int LocationId { get; set; }
        [Required]
        public string ScheduleName { get; set; }
        [Required]
        public string Type { get; set; }
        [Required]
        public string SubType { get; set; }
        [Required]
        public string TaskName { get; set; }
        [Required]
        public bool Recurrence { get; set; }
        public DateTime? LastRun { get; set; }
        
        public DateTime? NextRun { get; set; }
        [Required]
        public DateTime? ExecutionTime { get; set; }
        public bool IsActive { get; set; }
        [Required]
        public int? EmailSenderID { get; set; }
        public string EmailMessageBody { get; set; }
        [Required]
        public List<string> SendTo { get; set; }
        public string SendToEmails { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public List<SelectListVM> EmailMessageSendersList { get; set; }
        public bool IsSent { get; set; }
        public string EmailSubject { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

    }
    public class EmailSchulerListVM
    {
        public int ID { get; set; }
        public int? LocationId { get; set; }
        public string ScheduleName { get; set; }
        public string LocationName { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public string TaskName { get; set; }
        public bool Recurrence { get; set; }
        public DateTime? LastRun { get; set; }
        public DateTime? NextRun { get; set; }
        public bool IsActive { get; set; }
        public int? EmailSenderID { get; set; }
        public string EmailMessageBody { get; set; }
        public List<string> SendTo { get; set; }
        public string SendToEmails { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string EmailMessageSender { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime? ExecutionTime { get; set; }

    }

}
