using PMS.DTO.ViewModels;
using PMS.Services.Services.Firebase;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMS.Services.Services.Notifications
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly IFirebaseNotificationService firebaseNotificationService;

        public PushNotificationService(IFirebaseNotificationService _firebaseNotificationService)
        {
            firebaseNotificationService = _firebaseNotificationService;
        }

        public async Task SendPushNotificationAsync(string title, string body, int sentByUserId, string sentByUserName, string sentByEmail)
        {
            await firebaseNotificationService.SendSecurityAlertAndLogAsync(title, body, sentByUserId, sentByUserName, sentByEmail);
        }

        public List<PushNotificationLogViewModel> GetPushNotificationLogs(int top = 100)
        {
            return firebaseNotificationService.GetPushNotificationLogs(top);
        }
    }
}
