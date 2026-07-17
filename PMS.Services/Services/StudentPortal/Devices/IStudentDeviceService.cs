using System.Collections.Generic;

namespace PMS.Services.Services.StudentPortal.Devices
{
    public interface IStudentDeviceService
    {
        bool RegisterDevice(int userId, int personId, string deviceToken, string platform, string deviceId);
        bool UnregisterDevice(int userId, string deviceToken);
        List<string> GetActiveDeviceTokensByPersonId(int personId);
        List<string> GetActiveDeviceTokensByUserId(int userId);
        void DeactivateDeviceToken(string deviceToken);
    }
}
