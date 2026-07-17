# Automated Booking & Invoicing Implementation Guide

This document outlines the technical implementation details for the newly automated reservation workflow. The goal of this implementation is to eliminate the need for manual invoice creation by tightly coupling the Booking and Invoice generation into a single, atomic database transaction, and seamlessly integrating it with the Thawani Payment Gateway.

## 1. Architectural Overview

The core of the new architecture is the **Atomic Reservation Transaction**. Previously, the `/reservations` API only handled the creation of the guest and the booking record. The system required a separate API call to generate the deposit invoice.

**The New Workflow:**
1. **Guest Creation:** Create or fetch the guest using standard fallback mechanisms for missing location prefixes.
2. **Transaction Scope:** Open a database transaction.
3. **Booking Creation:** Insert the reservation record.
4. **Invoice Generation:** Instantly trigger `CreateDepositInvoiceOnly` using the generated booking details.
5. **Commit/Rollback:** If any step fails (e.g., missing configurations), the entire transaction rolls back, ensuring no orphaned bookings exist without invoices.
6. **Payment Link:** Return the `InvoiceId` to the client, allowing immediate generation of the payment gateway URL.

---

## 2. Core Logic Implementation

The core logic resides in the `ReservationsController.cs` file. We implemented an Entity Framework database transaction to wrap both operations.

> [!NOTE]
> Wrapping the logic in `BeginTransaction()` ensures data integrity. If the invoice fails to generate, the system will not create a "half-booking".

#### `PMSAPI/Controllers/Api/ReservationsController.cs`

```csharp
// Initialize Database Transaction
using (var transaction = uow.Context.Database.BeginTransaction())
{
    try
    {
        // 1. Create the Booking
        var booking = reservationService.AddBooking(vm);
        
        // 2. Prepare Data for Invoice Generation
        var invoiceBookingVm = new BookingVM
        {
            BookingID = booking.BookingID,
            PriceConfigID = booking.PriceConfigID,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate
        };
        
        // 3. Generate Invoice automatically within the same transaction
        int invoiceId = invoicingService.CreateDepositInvoiceOnly(invoiceBookingVm, resolvedPersonId);

        // 4. Commit changes to the database
        transaction.Commit();

        // 5. Map the newly generated InvoiceId to the API Response
        var mapped = MapBooking(booking);
        mapped.InvoiceId = invoiceId;
        
        response.Data = mapped;
    }
    catch (Exception ex)
    {
        // Rollback entire transaction to prevent data corruption
        transaction.Rollback();
        throw;
    }
}
```

---

## 3. Prerequisite Fixes & Configurations

To support this automated flow, several underlying system bugs had to be resolved. These are critical for the transaction to commit successfully:

### A. System User Configuration (`Web.config`)
The `CreateDepositInvoiceOnly` method relies on a system user to "own" the automated invoice creation. If the `OnlineBookingUserEmail` key is missing, the invoice generation fails, triggering a transaction rollback.
**Fix applied:** Added the admin email to `Web.config`.
```xml
<add key="OnlineBookingUserEmail" value="admin@themyriad.com" />
```

### B. JWT Bearer Identity Mapping
External API calls via JWT tokens often lack a user email. Entity Framework's `[Required]` attribute on the `CreatedBy` field blocks inserts if this is empty.
**Fix applied:** Implemented a fallback to explicitly assign `"integration-api"` when the token email is missing.
```csharp
var createdBy = string.IsNullOrWhiteSpace(PMS.Common.Globals.User?.Email) ? "integration-api" : PMS.Common.Globals.User.Email;
```

### C. SQL DateTime Out-of-Range Protection
If `CreatedDate` is not explicitly set, C# defaults to `0001-01-01`. SQL Server `DATETIME` rejects years prior to `1753`.
**Fix applied:** Enforced `CreatedDate = DateTime.Now` on all new booking inserts.

---

## 4. Payment Gateway Integration Enhancements

With the `InvoiceId` now successfully returned to the client, the front-end directly hits the `/payment-link` API. To ensure stability, the Thawani Payment Gateway integration was significantly strengthened:

### Exception Propagation
Previously, `PayGatewayApiHelper.cs` swallowed `WebException` errors (like 400 Bad Request or 401 Unauthorized), resulting in silent failures and `NullReferenceExceptions`. We modified the helper to extract the raw JSON error stream from Thawani and bubble it up to the controller.

### URL Validation
The Thawani API strictly validates callback URLs (`success_url` and `cancel_url`). We implemented an intelligent URL builder that checks if the incoming `returnUrl` is absolute or relative, and securely concatenates it with the `baseRedirectUrl`.

> [!TIP]
> Clients can now send an empty `"returnUrl": ""` in the payload, and the system will automatically inject the standard `/PaymentGateway/response?Respond=` callback URL.

---

## 5. Summary

The API is now fully robust. The integration client can execute a `POST /reservations` request, immediately receive the `invoiceId`, and pass it to `POST /payment-link` to process payments, without any manual intervention from hotel staff.
