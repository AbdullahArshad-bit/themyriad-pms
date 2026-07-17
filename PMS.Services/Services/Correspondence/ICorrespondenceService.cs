using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.EF;
using PMS.DTO.ViewModels.CorrespondenceViewModels;
using PMS.DTO.ViewModels;

namespace PMS.Services.Services.Correspondence
{
    public interface ICorrespondenceService
    {
        List<AddEmailSettingsVM> GetEmailSettings();
        AddEmailSettingsVM GetEmailSettingsById(int id);
        EmailSetting AddEmailSettings(AddEmailSettingsVM model);
        EmailSetting UpdateEmailSettings(AddEmailSettingsVM model);
        bool DeleteEmailSettings(int id);


        List<EmailSendersListVM> GetEmailSenders();
        EmailSendersListVM GetEmailSenderById(int id);
        EmailSender AddEmailSender(AddEmailSendersVM model);
        EmailSender UpdateEmailSender(AddEmailSendersVM model);
        bool DeleteEmailSender(int id);

        List<SelectListVM> SendersList();


        List<EmailMessagesListVM> GetEmailMessages();
        EmailMessagesListVM GetEmailMessageById(int id);
        EmailMessagesListVM GetEmailMessagesByActionId(int ActionId, int LocationId);
        EmailMessage AddEmailMessage(AddEmailMessageVM model);
        EmailMessage UpdateEmailMessage(AddEmailMessageVM model);
        bool DeleteEmailMessage(int id);

        List<SelectListVM> GetEmailMessagesDDList();

        List<CorrespondenceAction> GetCorrespondenceActions();
        List<EmailSchulerListVM> GetEmailSchedulers();
        EmailSchulerListVM GetEmailSchedulerById(int id);
        EmailScheduler AddEmailSchedule(AddEmailSchedulerVM model);
        EmailScheduler UpdateEmailSchedule(AddEmailSchedulerVM model);

        bool DeleteEmailSchedule(int id);

        bool ManuallySendEmail(int id);
    }
}
