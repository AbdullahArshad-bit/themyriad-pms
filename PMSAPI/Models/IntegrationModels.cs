using System;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.PaymentViewModels;

namespace PMSAPI.Models
{
    /// <summary>
    /// Create-reservation request from an integration client.
    /// Either <c>PersonId</c> (an existing guest) or <c>Guest</c> inline details must be provided.
    /// If both are supplied, <c>PersonId</c> takes priority.
    /// </summary>
    public class CreateReservationRequest
    {
        /// <summary>Existing guest ID from a previous POST /guests call. Optional if Guest details are provided.</summary>
        public int? PersonId { get; set; }
        /// <summary>Inline guest details. Used to create/match a guest when PersonId is not supplied.</summary>
        public CreateGuestRequest Guest { get; set; }
        public int PriceConfigId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public string SpecialRequests { get; set; }
    }

    /// <summary>Guest profile for creation via POST /properties/{propertyId}/guests.</summary>
    public class CreateGuestRequest
    {
        /// <summary>Full name of the guest. Required.</summary>
        public string FullName { get; set; }
        /// <summary>Email address. Required.</summary>
        public string Email { get; set; }
        /// <summary>Phone number. Required.</summary>
        public string Phone { get; set; }
        /// <summary>"Male" or "Female". Defaults to "Male" if not provided.</summary>
        public string Gender { get; set; }
        /// <summary>Date of birth. Optional.</summary>
        public DateTime? DateOfBirth { get; set; }
        /// <summary>Nationality. Optional.</summary>
        public string Nationality { get; set; }
        /// <summary>Passport or ID number. Optional.</summary>
        public string PassportNumber { get; set; }
        /// <summary>WhatsApp number. Optional.</summary>
        public string WhatsappNumber { get; set; }
    }

    /// <summary>Assign a physical unit (bed space) to a reservation.</summary>
    public class AssignUnitRequest
    {
        /// <summary>The bed space ID returned by GET /units BedSpaces[].id.</summary>
        public int UnitId { get; set; }
        public DateTime? MoveIn { get; set; }
        public DateTime? MoveOut { get; set; }
        public string Notes { get; set; }
    }

    public class CheckInRequest
    {
        public DateTime? CheckInTime { get; set; }
        public string CardNumber { get; set; }
        public string EncoderNumber { get; set; }
    }

    public class CheckOutRequest
    {
        public DateTime? CheckOutTime { get; set; }
        public string Notes { get; set; }
    }

    public class CancelReservationRequest
    {
        public string Reason { get; set; }
    }

    /// <summary>Partial guest profile update. Null fields are left unchanged.</summary>
    public class UpdateGuestRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string SecondaryEmail { get; set; }
        public string Phone { get; set; }
        public string WhatsappNumber { get; set; }
        public string Nationality { get; set; }
        public string PassportNumber { get; set; }
    }

    public class CreatePaymentLinkRequest
    {
        public int InvoiceId { get; set; }
        public string ReturnUrl { get; set; }
    }

    // ─── Response Models ───────────────────────────────────────────────────────

    /// <summary>Availability response returned by GET /properties/{propertyId}/availability</summary>
    public class AvailabilityResponse
    {
        public int UnitGroupId { get; set; }
        public string UnitGroupName { get; set; }
        public int AvailableCount { get; set; }
        public decimal? PriceFrom { get; set; }
        public string Currency { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public class PropertyResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Prefix { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class BedSpaceResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsOccupied { get; set; }
    }

    public class UnitResponse
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public int? FloorId { get; set; }
        public string RoomSize { get; set; }
        public string Gender { get; set; }
        public int? UnitGroupId { get; set; }
        public string UnitGroupName { get; set; }
        public bool IsEnabled { get; set; }
        public System.Collections.Generic.List<BedSpaceResponse> BedSpaces { get; set; }
    }

    public class RoomTypeFeatureResponse
    {
        public int Id { get; set; }
        public int? AllRoomFeatureId { get; set; }
        public string Name { get; set; }
    }

    public class PriceConfigResponse
    {
        public int Id { get; set; }
        public int? TermId { get; set; }
        public decimal? Price { get; set; }
        public decimal? InitialDeposit { get; set; }
        public string Currency { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class UnitGroupResponse
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Area { get; set; }
        public string BedSpace { get; set; }
        public decimal? ActualPrice { get; set; }
        public bool IsEnabled { get; set; }
        public System.Collections.Generic.List<RoomTypeFeatureResponse> Features { get; set; }
        public System.Collections.Generic.List<PriceConfigResponse> PriceConfigs { get; set; }
    }

    public class GuestResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string SecondaryEmail { get; set; }
        public string Phone { get; set; }
        public string WhatsappNumber { get; set; }
        public string Gender { get; set; }
        public string Nationality { get; set; }
        public string PassportNumber { get; set; }
        public string ResidentId { get; set; }
        public string Code { get; set; }
        public int? PropertyId { get; set; }
    }

    public class ReservationResponse
    {
        public int Id { get; set; }
        public string ConfirmationNumber { get; set; }
        public int? PropertyId { get; set; }
        public int GuestId { get; set; }
        public string GuestName { get; set; }
        public int? PriceConfigId { get; set; }
        public string UnitGroupName { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public string Channel { get; set; }
        public string Source { get; set; }
        public bool? IsCancelled { get; set; }
        public string Status { get; set; }
        public int? InvoiceId { get; set; }
    }

    public class ReservationListResponse
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public System.Collections.Generic.List<ReservationResponse> Items { get; set; }
    }

    public class FolioResponse
    {
        public int ReservationId { get; set; }
        public int GuestId { get; set; }
        public int? PropertyId { get; set; }
        public System.Collections.Generic.List<InvoicingVM> Charges { get; set; }
        public System.Collections.Generic.List<PaymentVM> Payments { get; set; }
    }

    public class PaymentLinkResponse
    {
        public string PaymentUrl { get; set; }
        public string TransactionReference { get; set; }
    }

    public class ReservationActionResponse
    {
        public int ReservationId { get; set; }
        public int? PropertyId { get; set; }
        public int? PlacementId { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public object Assignment { get; set; }
    }

    public class PaymentStatusResponse
    {
        public string ReferenceNo { get; set; }
        public decimal Amount { get; set; }
        public string InvoiceCode { get; set; }
        public string Date { get; set; }
        public string Card { get; set; }
        public string PaymentCode { get; set; }
    }

    public class WebhookSubscriptionDeleteResponse
    {
        public int SubscriptionId { get; set; }
    }

    public class AuthTokenRequest
    {
        public string grant_type { get; set; }
        public string client_id { get; set; }
        public string client_secret { get; set; }

        public string grantType { get { return grant_type; } set { grant_type = value; } }
        public string clientId { get { return client_id; } set { client_id = value; } }
        public string clientSecret { get { return client_secret; } set { client_secret = value; } }
    }
}
