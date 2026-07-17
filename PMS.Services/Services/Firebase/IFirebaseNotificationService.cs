using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.DTO.ViewModels;

namespace PMS.Services.Services.Firebase
{
    public interface IFirebaseNotificationService
    {
        Task SendSecurityAlertAsync(string title, string body);
        Task SavePushNotificationLog(PushNotificationLogViewModel model);
        Task SendSecurityAlertAndLogAsync(string title, string body, int sentByUserId, string sentByUserName, string sentByEmail);
        Task<int> SendToStudentDevicesAsync(int personId, string title, string body, string notificationType = "student_notification", string screen = "notifications", string redirectUrl = null);
        Task<int> SendToDeviceTokensAsync(IEnumerable<string> deviceTokens, string title, string body, string notificationType = "student_notification", string screen = "notifications", string redirectUrl = null);
        List<PushNotificationLogViewModel> GetPushNotificationLogs(int top = 100);
        string ResolveNotificationTopic();
    }
}


