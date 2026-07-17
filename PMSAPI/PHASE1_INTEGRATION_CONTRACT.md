# Phase 1 Integration Contract

This document describes the implemented `PMSAPI` integration surface for Crito.

## 1. Base URL and authentication

Integration endpoints use:

```
/integration/api/v1
```

`POST /auth/token` is the only unauthenticated endpoint. It uses OAuth 2.0 `client_credentials`; it does not use PMS staff email/password login.

Preferred request (JSON or `application/x-www-form-urlencoded`):

```json
{
  "grant_type": "client_credentials",
  "client_id": "crito",
  "client_secret": "<client secret>"
}
```

The earlier camelCase JSON spelling (`grantType`, `clientId`, `clientSecret`) remains accepted for compatibility. Credentials are checked against `ClientIntegration.Client_ID` and `ClientIntegration.Client_Secret`.

The token response is wrapped in the normal PMS response envelope:

```json
{
  "success": true,
  "code": 200,
  "message": "Token generated.",
  "data": {
    "accessToken": "<jwt>",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "userId": 0,
    "userName": "crito",
    "email": "crito"
  }
}
```

Use the token on every integration endpoint:

```
Authorization: Bearer <jwt>
```

Issued tokens are persisted to `ClientIntegration.Access_Token` and `Access_Token_Expiry`. The configured JWT expiry is currently 60 minutes unless changed in `PMSAPI/Web.config`.

All other endpoints return the same envelope: `success`, `code`, `message`, and `data`.

## 2. Implemented endpoint catalogue

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/properties` | List properties available to the integration user |
| GET | `/properties/{propertyId}` | Get one property |
| GET | `/properties/{propertyId}/unit-groups` | List room/unit groups |
| GET | `/properties/{propertyId}/unit-groups/{unitGroupId}` | Get one unit group |
| GET | `/properties/{propertyId}/units` | List physical units and bed spaces |
| GET | `/properties/{propertyId}/units/{unitId}` | Get one physical unit |
| GET | `/properties/{propertyId}/availability` | Availability by unit group; requires `fromDate` and `toDate`, optional `unitGroupId` |
| GET | `/properties/{propertyId}/guests/{guestId}` | Get a guest |
| POST | `/properties/{propertyId}/guests` | Create or match a guest |
| PUT | `/properties/{propertyId}/guests/{guestId}` | Update a guest |
| POST | `/properties/{propertyId}/reservations` | Create a reservation |
| GET | `/properties/{propertyId}/reservations/{reservationId}` | Get a reservation |
| GET | `/properties/{propertyId}/reservations` | List reservations with filters and pagination |
| PUT | `/properties/{propertyId}/reservations/{reservationId}/assign-unit` | Assign a unit/bed space |
| PUT | `/properties/{propertyId}/reservations/{reservationId}/check-in` | Check in a reservation |
| PUT | `/properties/{propertyId}/reservations/{reservationId}/check-out` | Check out a reservation |
| PUT | `/properties/{propertyId}/reservations/{reservationId}/cancel` | Cancel a reservation |
| GET | `/properties/{propertyId}/reservations/{reservationId}/folio` | Get reservation charges and payments |
| POST | `/properties/{propertyId}/reservations/{reservationId}/payment-link` | Generate a payment URL for an existing invoice |
| GET | `/properties/{propertyId}/payments/{reference}` | Look up a payment by gateway reference |
| GET | `/properties/{propertyId}/webhooks` | List active webhook subscriptions |
| POST | `/properties/{propertyId}/webhooks` | Create a webhook subscription |
| DELETE | `/properties/{propertyId}/webhooks/{subscriptionId}` | Disable a webhook subscription |

## 3. Reservation listing contract

`GET /properties/{propertyId}/reservations` supports:

- `fromDate`: inclusive check-in lower bound, ISO-8601 date/time.
- `toDate`: inclusive check-in upper bound, ISO-8601 date/time.
- `status`: `active` or `cancelled`; `canceled` is accepted as an alias.
- `modifiedSince`: inclusive timestamp; matches records created or updated on/after the value.
- `page`: one-based page number, default `1`.
- `pageSize`: default `20`, maximum `200`.

`fromDate` and `toDate` filter by reservation check-in date. Invalid ranges return a validation error. Results are ordered newest-created first.

Response shape:

```json
{
  "total": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1,
  "items": []
}
```

## 4. Payment links and invoice IDs

`POST /properties/{propertyId}/reservations/{reservationId}/payment-link` accepts:

```json
{
  "invoiceId": 12345,
  "returnUrl": "https://client.example/payment-result"
}
```

The endpoint does **not** create an invoice. The invoice must already exist and must belong to the reservation guest and property. If it does not, the API returns a not-found response.

Crito obtains the invoice ID from:

```
GET /properties/{propertyId}/reservations/{reservationId}/folio
```

The returned `data.charges` entries contain the invoice `id`, display `value` (invoice code and amount), and `amount`. If a reservation has no invoice/charge, it must first be invoiced in PMS; there is no Phase 1 invoice-creation endpoint.

## 5. Webhooks

Create a subscription:

```json
{
  "url": "https://client.example/webhooks/pms",
  "secret": "<shared secret>",
  "events": ["reservation.created"]
}
```

If `events` is omitted or empty, the subscription receives all supported events:

- `reservation.created`
- `reservation.assigned`
- `reservation.checked_in`
- `reservation.checked_out`
- `reservation.cancelled`
- `payment.link_created`
- `payment.completed`
- `payment.failed`

Every delivery is a `POST` with this envelope:

```json
{
  "eventType": "reservation.created",
  "eventId": "unique-id",
  "timestamp": "2026-07-16T12:00:00Z",
  "locationId": 10,
  "data": {}
}
```

Headers:

- `X-PMS-Event`: event name.
- `X-PMS-Signature`: lowercase HMAC-SHA256 of the exact request body, only when a subscription secret is configured.

Payloads dispatched by the implemented lifecycle actions:

| Event | Trigger | `data` |
|---|---|---|
| `reservation.created` | Reservation creation succeeds | Reservation representation: ID, confirmation number, guest, dates, property, status, etc. |
| `reservation.assigned` | Unit assignment succeeds | `reservationId`, `propertyId`, `assignment` |
| `reservation.checked_in` | Check-in succeeds | `placementId`, `reservationId`, `checkInTime` |
| `reservation.checked_out` | Check-out succeeds | `placementId`, `reservationId`, `checkOutTime` |
| `reservation.cancelled` | Cancellation succeeds | `reservationId`, `status`, `reason`, `cancelledAt` |
| `payment.link_created` | Payment URL generation succeeds | `reservationId`, `invoiceId`, `url` |
| `payment.failed` | Payment URL generation fails | `reservationId`, `invoiceId`, `message` |
| `payment.completed` | Payment-status lookup returns a paid transaction | `reservationId` when matched, `invoiceId`, `invoiceCode`, `reference`, `amount`, `paidAt` |

Reservation create, assign, check-in, check-out, and cancel actions dispatch their corresponding reservation events. Payment-link success/failure dispatches the payment-link events. A paid result from the payment-status endpoint dispatches `payment.completed`.

Webhook delivery is best-effort: failures are recorded in `IntegrationWebhookDispatchLog` and do not fail the originating PMS action. Consumers should de-duplicate deliveries using `eventId`.

## 6. Verification notes

The contract above was checked against the current controller routes, request models, `ClientIntegration` authentication service, JWT authorization filter, reservation query filters, payment-link validation, and webhook dispatch calls in the repository.

Before client sign-off, validate against the deployed environment as well: database connectivity, the configured client secret, gateway configuration, webhook callback reachability, and the deployed JWT issuer/key settings must match the integration environment.

## 7. Verification against the ten client points

| # | Client point | Verified status in current code |
|---:|---|---|
| 1 | System-to-system client authentication | **Implemented.** `POST /auth/token` validates `client_id`, `client_secret`, and `client_credentials` against `ClientIntegration`; it does not use `LoginVM`. |
| 2 | Guest creation for reservations | **Implemented.** `POST /properties/{propertyId}/guests` creates or matches a guest. Reservation creation also accepts either `personId` or inline `guest` details and creates/matches the guest when `personId` is absent. |
| 3 | Typed Swagger response models | **Implemented.** Primary resources and the remaining lifecycle/status operations now use named response models in Swagger, including properties, unit groups, units, availability, guests, reservations, folio, payment links/status, reservation actions, webhook deletion, and token issuance. |
| 4 | Reservation date/status/pagination filters | **Implemented.** `fromDate`, `toDate`, `status`, `modifiedSince`, `page`, and `pageSize` are supported and validated. |
| 5 | Webhook names, payloads, and lifecycle dispatch | **Implemented.** The supported events, envelope, signature, payloads, and dispatch triggers are documented in Section 5. |
| 6 | `unitId` for assignment | **Implemented.** The public `AssignUnitRequest` field is `unitId`; it is mapped internally to the PMS bed-space identifier. |
| 7 | Mandatory availability dates | **Implemented.** Both `fromDate` and `toDate` are required, and `toDate` cannot precede `fromDate`. Response fields are documented in Section 2 and the `AvailabilityResponse` model. |
| 8 | Payment link when invoice is absent | **Implemented and documented.** The endpoint does not create invoices; Crito obtains an existing invoice ID from the reservation folio. |
| 9 | Crito-owned hotel locks | **Implemented.** Integration check-in calls `CheckInPlacement(..., skipLockIntegration: true)` and check-out calls `CheckOutPlacementAsync(..., skipLockIntegration: true)`, so these API actions do not invoke the existing Myriad lock integration. |
| 10 | Swagger product title | **Implemented.** Swagger is configured with the title `Odyssey Integration API`. |

## 8. Swagger response model details

The following named models are visible to Swagger for the primary read/create operations:

- `PropertyResponse` and `List<PropertyResponse>`
- `UnitGroupResponse` and `List<UnitGroupResponse>`
- `UnitResponse` and `List<UnitResponse>` (including nested `BedSpaceResponse`)
- `AvailabilityResponse` and `List<AvailabilityResponse>`
- `GuestResponse`
- `ReservationResponse` and `ReservationListResponse`
- `FolioResponse` with typed `charges` (`InvoicingVM[]`) and `payments` (`PaymentVM[]`)
- `PaymentLinkResponse` (`paymentUrl`, `transactionReference`); the current gateway adapter may leave `transactionReference` null.
- `WebhookSubscriptionDto` and `List<WebhookSubscriptionDto>`

The common envelope remains `ApiResponse<T>` with `success`, `code`, `message`, and `data`. The common envelope remains `ApiResponse<T>` with the named `T` model shown by Swagger for each operation.

## 9. Reservation creation example

A reservation can be created without a pre-existing guest ID:

```json
{
  "guest": {
    "fullName": "Alex Guest",
    "email": "alex@example.com",
    "phone": "+49123456789",
    "gender": "Male",
    "nationality": "German"
  },
  "priceConfigId": 1001,
  "checkInDate": "2026-08-01T15:00:00Z",
  "checkOutDate": "2026-08-31T10:00:00Z",
  "specialRequests": "Late arrival"
}
```

If `personId` is supplied, it takes priority. If only `guest` is supplied, PMS matches an existing guest by email within the property or creates a new guest profile.

## 10. Availability response example

`GET /properties/{propertyId}/availability?fromDate=2026-08-01&toDate=2026-08-31` returns rows shaped as:

```json
{
  "unitGroupId": 12,
  "unitGroupName": "Studio",
  "availableCount": 8,
  "priceFrom": 950.00,
  "currency": null,
  "fromDate": "2026-08-01T00:00:00Z",
  "toDate": "2026-08-31T00:00:00Z"
}
```

`currency` may be `null` when the configured PMS price does not provide one.