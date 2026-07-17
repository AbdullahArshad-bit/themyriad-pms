using PMS.DTO;
using PMS.Services.Services.Integration;
using System;
using PMSAPI.Models;
using System.Web.Http;

namespace PMSAPI.Controllers.Api
{
    /// <summary>
    /// Manage outbound webhook subscriptions per property (Crito callback URLs).
    /// </summary>
    [RoutePrefix("integration/api/v1/properties")]
    public class WebhooksController : IntegrationApiController
    {
        private readonly IWebhookService webhookService;

        public WebhooksController(IWebhookService _webhookService)
        {
            webhookService = _webhookService;
        }

        [HttpGet]
        [Route("{propertyId:int}/webhooks")]
        public ApiResponse<System.Collections.Generic.List<WebhookSubscriptionDto>> List(int propertyId)
        {
            try
            {
                return Success(webhookService.GetSubscriptions(propertyId));
            }
            catch (Exception ex)
            {
                return Fail<System.Collections.Generic.List<WebhookSubscriptionDto>>(ex.GetBaseException().Message);
            }
        }

        [HttpPost]
        [Route("{propertyId:int}/webhooks")]
        public ApiResponse<WebhookSubscriptionDto> Create(int propertyId, CreateWebhookSubscriptionRequest model)
        {
            try
            {
                var created = webhookService.CreateSubscription(propertyId, model, "integration-api");
                return Success(created, "Webhook subscription created.");
            }
            catch (Exception ex)
            {
                return Fail<WebhookSubscriptionDto>(ex.GetBaseException().Message);
            }
        }

        [HttpDelete]
        [Route("{propertyId:int}/webhooks/{subscriptionId:int}")]
        public ApiResponse<WebhookSubscriptionDeleteResponse> Delete(int propertyId, int subscriptionId)
        {
            try
            {
                var ok = webhookService.DeleteSubscription(propertyId, subscriptionId);
                if (!ok)
                {
                    return NotFound<WebhookSubscriptionDeleteResponse>("Webhook subscription not found.");
                }
                return Success(new WebhookSubscriptionDeleteResponse { SubscriptionId = subscriptionId }, "Webhook subscription deleted.");
            }
            catch (Exception ex)
            {
                return Fail<WebhookSubscriptionDeleteResponse>(ex.GetBaseException().Message);
            }
        }
    }
}
