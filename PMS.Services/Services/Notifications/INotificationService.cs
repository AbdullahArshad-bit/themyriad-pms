using PMS.DTO.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.Notifications
{
    public interface INotificationService
    {
         Task<bool> SendNotification(int? UserId, int? PersonId, string Type, string Subject, string Description, string RedirectURL,string CreatedBy);
        List<NotificationViewModel> SPGetNotification(string Type, int Id);
        bool UpdateNotification(int Id,int Type);
        List<NotificationViewModel>GetAllNotification(int Id,int Type);
        void SendNotificationasync(NotificationViewModel model);
    }
}
