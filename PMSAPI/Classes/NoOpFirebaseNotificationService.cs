using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.DTO.ViewModels;
using PMS.Services.Services.Firebase;

namespace PMSAPI.Classes
{
    /// <summary>
    /// Integration API does not send push notifications. This stub satisfies DI
    /// without requiring Firebase credentials in PMSAPI Web.config.
    /// </summary>
    public class NoOpFirebaseNotificationService : IFirebaseNotificationService
    {
        public Task SendSecurityAlertAsync(string title, string body) => Task.CompletedTask;

        public Task SavePushNotificationLog(PushNotificationLogViewModel model) => Task.CompletedTask;

        public Task SendSecurityAlertAndLogAsync(string title, string body, int sentByUserId, string sentByUserName, string sentByEmail)
            => Task.CompletedTask;

        public Task<int> SendToStudentDevicesAsync(int personId, string title, string body, string notificationType = "student_notification", string screen = "notifications", string redirectUrl = null)
            => Task.FromResult(0);

        public Task<int> SendToDeviceTokensAsync(IEnumerable<string> deviceTokens, string title, string body, string notificationType = "student_notification", string screen = "notifications", string redirectUrl = null)
            => Task.FromResult(0);

        public List<PushNotificationLogViewModel> GetPushNotificationLogs(int top = 100) => new List<PushNotificationLogViewModel>();

        public string ResolveNotificationTopic() => string.Empty;
    }
}
