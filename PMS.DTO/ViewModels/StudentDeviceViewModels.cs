using System.ComponentModel.DataAnnotations;

namespace PMS.DTO.ViewModels
{
    public class StudentLoginVM : LoginVM
    {
        /// <summary>Firebase Cloud Messaging (FCM) registration token from the mobile app.</summary>
        public string DeviceToken { get; set; }

        /// <summary>Android or iOS.</summary>
        public string Platform { get; set; }

        /// <summary>Optional hardware device identifier from the mobile OS.</summary>
        public string DeviceId { get; set; }
    }

    public class RegisterStudentDeviceVM
    {
        [Required]
        public string DeviceToken { get; set; }

        public string Platform { get; set; }
        public string DeviceId { get; set; }
    }

    public class UnregisterStudentDeviceVM
    {
        public string DeviceToken { get; set; }
    }
}
