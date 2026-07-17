using PMS.DTO.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMS.Services.Services.Notifications
{
    public interface IPushNotificationService
    {
        Task SendPushNotificationAsync(string title, string body, int sentByUserId, string sentByUserName, string sentByEmail);
        List<PushNotificationLogViewModel> GetPushNotificationLogs(int top = 100);
    }
}
