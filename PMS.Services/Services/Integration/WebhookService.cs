using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PMS.EF;
using PMS.Repository.UnitOfWork;

namespace PMS.Services.Services.Integration
{
    public static class WebhookEventTypes
    {
        public const string ReservationCreated = "reservation.created";
        public const string ReservationUpdated = "reservation.updated";
        public const string ReservationAssigned = "reservation.assigned";
        public const string ReservationCheckedIn = "reservation.checked_in";
        public const string ReservationCheckedOut = "reservation.checked_out";
        public const string ReservationCancelled = "reservation.cancelled";
        public const string PaymentLinkCreated = "payment.link_created";
        public const string PaymentCompleted = "payment.completed";
        public const string PaymentFailed = "payment.failed";
    }

    public class WebhookSubscriptionDto
    {
        public int Id { get; set; }
        public int LocationId { get; set; }
        public string Url { get; set; }
        public string Secret { get; set; }
        public string Events { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
    }

    public class CreateWebhookSubscriptionRequest
    {
        public string Url { get; set; }
        public string Secret { get; set; }
        public string[] Events { get; set; }
    }

    public interface IWebhookService
    {
        List<WebhookSubscriptionDto> GetSubscriptions(int locationId);
        WebhookSubscriptionDto CreateSubscription(int locationId, CreateWebhookSubscriptionRequest request, string createdBy);
        bool DeleteSubscription(int locationId, int subscriptionId);
        Task DispatchAsync(int locationId, string eventType, object payload);
    }

    /// <summary>
    /// Outbound webhook publisher for Crito. Subscriptions and dispatch logs are stored
    /// in IntegrationWebhook* tables (see PMSAPI/Sql/IntegrationWebhooks.sql).
    /// </summary>
    public class WebhookService : IWebhookService
    {
        private static readonly HttpClient HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        private readonly UnitOfWork<PMSEntities> uow;

        public WebhookService(UnitOfWork<PMSEntities> _uow)
        {
            uow = _uow;
        }

        public List<WebhookSubscriptionDto> GetSubscriptions(int locationId)
        {
            EnsureTables();
            var sql = @"
SELECT Id, LocationId, Url, Secret, Events, IsActive, CreatedDate, CreatedBy
FROM IntegrationWebhookSubscription
WHERE LocationId = @locationId AND IsActive = 1
ORDER BY Id DESC";

            return uow.Context.Database.SqlQuery<WebhookSubscriptionDto>(
                sql,
                new SqlParameter("@locationId", locationId)).ToList();
        }

        public WebhookSubscriptionDto CreateSubscription(int locationId, CreateWebhookSubscriptionRequest request, string createdBy)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
            {
                throw new ArgumentException("Url is required.");
            }

            EnsureTables();

            var events = request.Events == null || request.Events.Length == 0
                ? string.Join(",", new[]
                {
                    WebhookEventTypes.ReservationCreated,
                    WebhookEventTypes.ReservationAssigned,
                    WebhookEventTypes.ReservationCheckedIn,
                    WebhookEventTypes.ReservationCheckedOut,
                    WebhookEventTypes.ReservationCancelled,
                    WebhookEventTypes.PaymentLinkCreated,
                    WebhookEventTypes.PaymentCompleted,
                    WebhookEventTypes.PaymentFailed
                })
                : string.Join(",", request.Events.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()));

            var createdDate = DateTime.UtcNow;
            var sql = @"
INSERT INTO IntegrationWebhookSubscription (LocationId, Url, Secret, Events, IsActive, CreatedDate, CreatedBy)
OUTPUT INSERTED.Id, INSERTED.LocationId, INSERTED.Url, INSERTED.Secret, INSERTED.Events, INSERTED.IsActive, INSERTED.CreatedDate, INSERTED.CreatedBy
VALUES (@locationId, @url, @secret, @events, 1, @createdDate, @createdBy)";

            return uow.Context.Database.SqlQuery<WebhookSubscriptionDto>(
                sql,
                new SqlParameter("@locationId", locationId),
                new SqlParameter("@url", request.Url.Trim()),
                new SqlParameter("@secret", (object)request.Secret ?? DBNull.Value),
                new SqlParameter("@events", events),
                new SqlParameter("@createdDate", createdDate),
                new SqlParameter("@createdBy", (object)createdBy ?? DBNull.Value)).FirstOrDefault();
        }

        public bool DeleteSubscription(int locationId, int subscriptionId)
        {
            EnsureTables();
            var sql = @"
UPDATE IntegrationWebhookSubscription
SET IsActive = 0
WHERE Id = @id AND LocationId = @locationId";

            var rows = uow.Context.Database.ExecuteSqlCommand(
                sql,
                new SqlParameter("@id", subscriptionId),
                new SqlParameter("@locationId", locationId));

            return rows > 0;
        }

        public async Task DispatchAsync(int locationId, string eventType, object payload)
        {
            try
            {
                EnsureTables();

                var subscriptions = GetSubscriptions(locationId)
                    .Where(s => SubscriptionCoversEvent(s.Events, eventType))
                    .ToList();

                if (subscriptions.Count == 0)
                {
                    return;
                }

                var envelope = new
                {
                    eventType,
                    eventId = Guid.NewGuid().ToString("N"),
                    timestamp = DateTime.UtcNow,
                    locationId,
                    data = payload
                };

                var json = JsonConvert.SerializeObject(envelope);

                foreach (var sub in subscriptions)
                {
                    await DeliverAsync(sub, eventType, json);
                }
            }
            catch
            {
                // Webhook delivery must never break the primary API action.
            }
        }

        private async Task DeliverAsync(WebhookSubscriptionDto subscription, string eventType, string json)
        {
            int? responseCode = null;
            var success = false;
            string error = null;

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url))
                {
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    request.Headers.Add("X-PMS-Event", eventType);

                    if (!string.IsNullOrWhiteSpace(subscription.Secret))
                    {
                        request.Headers.Add("X-PMS-Signature", ComputeSignature(json, subscription.Secret));
                    }

                    var response = await HttpClient.SendAsync(request);
                    responseCode = (int)response.StatusCode;
                    success = response.IsSuccessStatusCode;
                    if (!success)
                    {
                        error = await response.Content.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.GetBaseException().Message;
            }

            try
            {
                var logSql = @"
INSERT INTO IntegrationWebhookDispatchLog
(SubscriptionId, EventType, Payload, ResponseCode, Success, AttemptedAt, ErrorMessage)
VALUES (@subscriptionId, @eventType, @payload, @responseCode, @success, @attemptedAt, @errorMessage)";

                uow.Context.Database.ExecuteSqlCommand(
                    logSql,
                    new SqlParameter("@subscriptionId", subscription.Id),
                    new SqlParameter("@eventType", eventType),
                    new SqlParameter("@payload", (object)json ?? DBNull.Value),
                    new SqlParameter("@responseCode", (object)responseCode ?? DBNull.Value),
                    new SqlParameter("@success", success),
                    new SqlParameter("@attemptedAt", DateTime.UtcNow),
                    new SqlParameter("@errorMessage", (object)error ?? DBNull.Value));
            }
            catch
            {
                // Ignore log write failures.
            }
        }

        private static bool SubscriptionCoversEvent(string eventsCsv, string eventType)
        {
            if (string.IsNullOrWhiteSpace(eventsCsv))
            {
                return true;
            }

            return eventsCsv.Split(',')
                .Select(e => e.Trim())
                .Any(e => string.Equals(e, eventType, StringComparison.OrdinalIgnoreCase) || e == "*");
        }

        private static string ComputeSignature(string payload, string secret)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private void EnsureTables()
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.IntegrationWebhookSubscription', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntegrationWebhookSubscription
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        LocationId INT NOT NULL,
        Url NVARCHAR(1000) NOT NULL,
        Secret NVARCHAR(200) NULL,
        Events NVARCHAR(1000) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_IntegrationWebhookSubscription_IsActive DEFAULT(1),
        CreatedDate DATETIME NOT NULL,
        CreatedBy NVARCHAR(100) NULL
    );
END

IF OBJECT_ID(N'dbo.IntegrationWebhookDispatchLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntegrationWebhookDispatchLog
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SubscriptionId INT NOT NULL,
        EventType NVARCHAR(100) NOT NULL,
        Payload NVARCHAR(MAX) NULL,
        ResponseCode INT NULL,
        Success BIT NOT NULL,
        AttemptedAt DATETIME NOT NULL,
        ErrorMessage NVARCHAR(MAX) NULL
    );
END";

            uow.Context.Database.ExecuteSqlCommand(sql);
        }
    }
}
