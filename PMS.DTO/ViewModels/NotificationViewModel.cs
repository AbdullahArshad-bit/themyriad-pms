using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels
{
    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string RedirectURL { get; set; }
        public bool IsRead { get; set; }
        public Nullable<int> UserId { get; set; }
        public Nullable<int> PersonId { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public int Counts { get; set; }
        public int TypeId { get; set; }
        public int GroupId { get; set; }
        public string RoleName { get; set; }
    }

    public class PushNotificationLogViewModel
    {
        public long ID { get; set; }
        public string Title { get; set; }
        public string MessageBody { get; set; }
        public string Topic { get; set; }
        public int SentByUserID { get; set; }
        public string SentByUserName { get; set; }
        public string SentByEmail { get; set; }
        public DateTime SentOn { get; set; }
        public int? DeliveredDeviceCount { get; set; }
    }

    public class PushNotificationPageViewModel
    {
        public List<PushNotificationLogViewModel> Logs { get; set; }
    }
}
