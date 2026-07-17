using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using PMS.Common.Classes;
using PMS.DTO.ViewModels;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.AuditLogs;
using PMS.Services.Services.StudentPortal.Devices;

namespace PMS.Services.Services.Firebase
{
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        private readonly FirebaseMessaging _messaging;
        private readonly UnitOfWork<PMS.EF.PMSEntities> uow;
        private readonly IAuditLogsService auditLogsService;
        private readonly IStudentDeviceService studentDeviceService;
        private const string ProductionTopic = "SecurityAlert";
        private const string SandboxTopic = "SecurityAlertSandbox";
        private const string ProductionHost = "pms.themyriad.com";
        private const int UatPort = 8020;

        public FirebaseNotificationService(
            UnitOfWork<PMS.EF.PMSEntities> _uow,
            IAuditLogsService _auditLogsService,
            IStudentDeviceService _studentDeviceService)
        {
            uow = _uow;
            auditLogsService = _auditLogsService;
            studentDeviceService = _studentDeviceService;
            var credentialVirtualPath = ConfigurationManager.AppSettings["FirebaseCredentialPath"];

            if (string.IsNullOrWhiteSpace(credentialVirtualPath))
            {
                throw new ConfigurationErrorsException("FirebaseCredentialPath appSetting is missing in Web.config.");
            }

            var credentialPhysicalPath = HostingEnvironment.MapPath(credentialVirtualPath);

            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialPhysicalPath)
                });
            }

            _messaging = FirebaseMessaging.DefaultInstance;
        }

        public async Task SendSecurityAlertAsync(string title, string body)
        {
            var topic = ResolveNotificationTopic();

            var message = new Message
            {
                Topic = topic,

                // 🔹 Notification (display purpose)
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },

                // 🔹 Data payload (IMPORTANT for iOS click handling)
                Data = new Dictionary<string, string>
        {
            { "type", "security_alert" },
            { "screen", "alerts" },
            { "title", title },
            { "body", body }
        },

                // 🔹 iOS (APNS) configuration
                Apns = new ApnsConfig
                {
                    Headers = new Dictionary<string, string>
            {
                { "apns-push-type", "alert" },
                { "apns-priority", "10" }
            },
                    Aps = new Aps
                    {
                        Alert = new ApsAlert
                        {
                            Title = title,
                            Body = body
                        },
                        Sound = "default",
                        Badge = 1,

                        // 👇 Helps with background handling
                        ContentAvailable = true,

                        // 👇 Ensures notification is treated as alert
                        MutableContent = true
                    }
                },

                // 🔹 Android configuration
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        Sound = "default",
                        ChannelId = "default_notification_channel_id",
                        ClickAction = "OPEN_ACTIVITY_1" // optional (depends on app)
                    }
                }
            };

            await _messaging.SendAsync(message);
        }

        public async Task<int> SendToStudentDevicesAsync(
            int personId,
            string title,
            string body,
            string notificationType = "student_notification",
            string screen = "notifications",
            string redirectUrl = null)
        {
            var deviceTokens = studentDeviceService.GetActiveDeviceTokensByPersonId(personId);
            return await SendToDeviceTokensAsync(deviceTokens, title, body, notificationType, screen, redirectUrl);
        }

        public async Task<int> SendToDeviceTokensAsync(
            IEnumerable<string> deviceTokens,
            string title,
            string body,
            string notificationType = "student_notification",
            string screen = "notifications",
            string redirectUrl = null)
        {
            var tokens = deviceTokens?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList() ?? new List<string>();

            if (tokens.Count == 0 || string.IsNullOrWhiteSpace(title))
            {
                return 0;
            }

            var deliveredCount = 0;

            foreach (var token in tokens)
            {
                try
                {
                    var message = BuildDeviceMessage(token, title, body, notificationType, screen, redirectUrl);
                    await _messaging.SendAsync(message);
                    deliveredCount++;
                }
                catch (FirebaseMessagingException ex) when (IsInvalidDeviceToken(ex))
                {
                    studentDeviceService.DeactivateDeviceToken(token);
                }
                catch
                {
                    // Skip failed token delivery and continue with remaining devices.
                }
            }

            return deliveredCount;
        }

        private static Message BuildDeviceMessage(
            string deviceToken,
            string title,
            string body,
            string notificationType,
            string screen,
            string redirectUrl)
        {
            var data = new Dictionary<string, string>
            {
                { "type", notificationType ?? "student_notification" },
                { "screen", screen ?? "notifications" },
                { "title", title },
                { "body", body ?? string.Empty }
            };

            if (!string.IsNullOrWhiteSpace(redirectUrl))
            {
                data["redirectUrl"] = redirectUrl;
            }

            return new Message
            {
                Token = deviceToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data,
                Apns = new ApnsConfig
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "apns-push-type", "alert" },
                        { "apns-priority", "10" }
                    },
                    Aps = new Aps
                    {
                        Alert = new ApsAlert
                        {
                            Title = title,
                            Body = body
                        },
                        Sound = "default",
                        Badge = 1,
                        ContentAvailable = true,
                        MutableContent = true
                    }
                },
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        Sound = "default",
                        ChannelId = "default_notification_channel_id",
                        ClickAction = "OPEN_ACTIVITY_1"
                    }
                }
            };
        }

        private static bool IsInvalidDeviceToken(FirebaseMessagingException ex)
        {
            return ex.MessagingErrorCode == MessagingErrorCode.Unregistered
                || ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument;
        }

        public async Task SendSecurityAlertAndLogAsync(string title, string body, int sentByUserId, string sentByUserName, string sentByEmail)
        {
            await SendSecurityAlertAsync(title, body);

            await SavePushNotificationLog(new PushNotificationLogViewModel
            {
                Title = title,
                MessageBody = body,
                Topic = ResolveNotificationTopic(),
                SentByUserID = sentByUserId,
                SentByUserName = sentByUserName,
                SentByEmail = sentByEmail,
                SentOn = DateTime.Now,
                DeliveredDeviceCount = null
            });
        }

        public async Task SavePushNotificationLog(PushNotificationLogViewModel model)
        {
            var log = new PMS.EF.PushNotificationLog
            {
                Title = model.Title,
                MessageBody = model.MessageBody,
                Topic = model.Topic ?? ResolveNotificationTopic(),
                SentByUserID = model.SentByUserID,
                SentByUserName = model.SentByUserName,
                SentByEmail = model.SentByEmail,
                SentOn = model.SentOn,
                DeliveredDeviceCount = model.DeliveredDeviceCount,
                IsEnable = true
            };

            uow.GenericRepository<PMS.EF.PushNotificationLog>().Insert(log);
            uow.SaveChanges();

            auditLogsService.AddAuditLog(new PMS.EF.AuditLog
            {
                AuditType = (int)Enumeration.AuditType.Create,
                ActionId = (int)Enumeration.CorrespondenceAction.PushNotificationSent,
                PK = log.ID.ToString(),
                UserId = model.SentByUserID,
                TableName = "PushNotificationLog",
                Reference = model.Title,
                UserName = (model.SentByUserName ?? string.Empty) + " - " + (model.SentByEmail ?? string.Empty),
                PersonId = null
            });

            await Task.CompletedTask;
        }

        public List<PushNotificationLogViewModel> GetPushNotificationLogs(int top = 100)
        {
            if (top <= 0)
            {
                top = 100;
            }

            return uow.GenericRepository<PMS.EF.PushNotificationLog>().Table
                .Where(x => x.IsEnable)
                .OrderByDescending(x => x.SentOn)
                .Take(top)
                .Select(x => new PushNotificationLogViewModel
                {
                    ID = x.ID,
                    Title = x.Title,
                    MessageBody = x.MessageBody,
                    Topic = x.Topic,
                    SentByUserID = x.SentByUserID,
                    SentByUserName = x.SentByUserName,
                    SentByEmail = x.SentByEmail,
                    SentOn = x.SentOn,
                    DeliveredDeviceCount = x.DeliveredDeviceCount
                }).ToList();
        }

        public string ResolveNotificationTopic()
        {
            var requestUrl = HttpContext.Current?.Request?.Url;
            if (requestUrl == null)
            {
                return SandboxTopic;
            }

            var host = requestUrl.Host?.ToLowerInvariant() ?? string.Empty;
            var port = requestUrl.Port;

            // If it's using the UAT port, it should always be the Sandbox topic
            if (port == UatPort)
            {
                return SandboxTopic;
            }

            // Production check: Host must match pms.themyriad.com
            if (host == ProductionHost)
            {
                return ProductionTopic;
            }

            // Default to Sandbox for all other cases (localhost, test domains, etc.)
            return SandboxTopic;
        }

    }
}


