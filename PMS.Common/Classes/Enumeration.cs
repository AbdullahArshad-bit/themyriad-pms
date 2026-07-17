using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Common.Classes
{
    public class Enumeration
    {
        public enum CorrespondenceAction
        {
            SendContract = 1,
            SignContract = 2,
            CreateBooking = 3,
            UpdateBooking = 4,
            CreatePlacement = 5,
            UpdatePlacement = 6,
            DeleteBooking = 7,
            CancelBooking = 8,
            CreatePerson = 14,
            UpdatePerson = 15,
            DeletePerson = 16,
            DeletePlacement = 17,
            CheckinPlacement = 18,
            CheckOutPlacement = 19,
            EmailSettings = 20,
            AddEmailSender = 21,
            UpdateEmailSender = 22,
            AddEmailMessage = 23,
            UpdateEmailMessage = 24,
            DeleteEmailSender = 25,
            DeleteEmailMessage = 26,
            UserLoggedIn = 27,
            UpdateServices = 28,
            CreateServices = 29,
            DeleteServices = 30,
            CreateTax = 31,
            UpdateTax = 32,
            DeleteTax = 33,
            CreatePaymentMethod = 34,
            UpdatePaymentMethod = 35,
            DeletePaymentMethod = 36,
            CreateChartOfAccount = 37,
            UpdateChartOfAccounts = 38,
            DeleteChartOfAccounts = 39,
            CreatePayment = 40,
            UpdatePayment = 41,
            DeletePayment = 42,
            CreateInvoice = 43,
            UpdateInvoice = 44,
            DeleteInvoice = 45,
            CreateRoomFeature = 46,
            UpdateRoomFeature = 47,
            DeleteRoomFeature = 48,
            CreateRoomType = 49,
            UpdateRoomType = 50,
            DeleteRoomtype = 51,
            CreateUniversity = 52,
            UpdateUniversity = 53,
            DeleteUniversity = 54,
            AddLocation = 55,
            UpdateLocation = 56,
            DeleteLocation = 57,
            CreateTerm = 58,
            UpdateTerm = 59,
            DeleteTerm = 60,
            UpdatePriceConfig = 61,
            DeletePriceConfig = 62,
            UpdateProject = 63,
            DeleteProject = 64,
            UpdateBuilding = 65,
            DeleteBuilding = 66,
            UpdateFloor = 67,
            DeleteFloor = 68,
            UpdateRoom = 69,
            DeleteRoom = 70,
            UpdateBedSpace = 71,
            DeleteBedSpace = 72,
            UpdateUserMaster = 73,
            ChangeUserPassword = 74,
            SwapBedSpace = 75,
            UploadPersonDocument = 76,
            DeletePersonDocument = 77,
            CreateUserMaster = 78,
            Ticket = 80,
            TicketCreate = 81,
            TicketUpdate = 82,
            Feedback = 83,
            FeedbackEmail = 84,
            TransportBookingEmail = 85,
            TransportCancelEmail = 86,
            ReferralEmail = 87,
            GenerateBookingWithBankTransfer = 88,
            GenerateBookingWithCreditCard = 89,
            GenerateRefundRequest = 90,
            SendRefundRequestToStudent = 91,
            PreCheckInDocumentation = 92,
            SignDocumentation = 93,
            SendContractForDubai = 94,
            SignContractForDubai = 95,
            ReissueCard = 96,
            CreateVoucher = 97,
            UpdateVoucher = 98,
            CancelContract = 99,
            SendReceiptEmail = 100,
            UpdateRoleRights = 101,
            PushNotificationSent = 102


        }
        public enum InspectionAction
        {
            AcceptInspection = 9,
            GenerateInspection = 10, // 0x0000000A
            DeleteInspection = 11, // 0x0000000B
            UpdateInspection = 12, // 0x0000000C
            CompleteInspection = 13 // 0x0000000D
        }
        public enum AuditType
        {
            Read = 0,
            Create = 1,
            Update = 2,
            Delete = 3
        }
        public enum ServiceTypes
        {
            RentalCharges = 1,
            Transportation = 3,
            Deposit = 2,
            Other = 4
        }
        public enum AccountTypes
        {
            Cash = 1,
            Checking = 2,
            Expense = 3,
            Income = 4,
            NA = 5,
            OtherAssets = 6,
            OtherLiability = 7,
            Assets = 8,
            Liabilities = 9
        }
        public enum AssessmentServiceTypesOrder
        {
            RentalCharges = 257

        }
        public enum InvoiceTypes
        {
            Rental = 1,
            Deposit = 2,
            Miscellaneous = 3,
            Refund = 4
        }

        //in InvoiceTypeLookup added types of payment
        public enum PaymentLookup
        {
            Payment = 5,
            PaymentRefund = 6
        }

        public enum LocationEnum
        {
            Muscat = 16,
            Dubai = 17
        }
        public enum COA
        {
            Cleaning = 22
        }
        public enum Status
        {
            Pending = 1,
            Started = 2,
            Approved = 3
        }
        public enum SystemConfigurationType
        {
            ThwaniOnlinePayment = 1
        }
        public enum TicketStatus
        {
            Open = 1,
            Pending = 2,
            Resolved = 3,
            Closed = 4,
            WaitingOnCustomer = 5
        }
        public enum TicketPeriority
        {
            Low = 1,
            Pending = 2,
            High = 3,
        }
        public enum Tickets
        {
            AllTickets = 0,
            TicketIRaised = 1,
            TicketIMentionIn = 2
        }
        public enum TicketType
        {
            IssueByResident = 1,
            IssueByStaff = 2,
            NonResident = 3
        }
        public enum Group
        {
            Biiling = 1,
            IT = 2,
            Maintenance = 3,
            Other = 4
        }
        public enum NotifiactionType
        {
            Admin = 1,
            Student = 2,
            Group = 3
        }
        public enum TicketGroupRoles
        {
            Head = 1,
            Manager = 2,
            Member = 3
        }
        public enum JobsCategories
        {
            TicketSLA = 1
        }
        public enum SubscriptionsStatusLookup
        {
            Pending = 1,
            Active = 2,
            Suspended = 3,
            Ended = 4
        }
        public enum BookingRptStatus
        {
            Allocated = 1,
            Pending = 2,
            Cancelled = 3
        }
        public enum RoomInventoryRptStatus
        {
            Allocated = 1,
            CheckedIn = 2,
            Vacant = 3
        }
        public enum TransportationBooking
        {
            Booked = 1,
            Cancel = 2
        }
        public enum FrequencyStatusLookup
        {
            Daily = 1,
            Monthly = 2,
            Weekly = 3
        }

    }

    ///Sync Category
    public enum SyncCategory
    {
        Ticket = 1
    }
    public enum SyncType
    {
        Status = 1,
        DueDate = 2
    }
    public enum JobActions
    {
        HeadOfDepartment = 1,
        Manager = 2,
        Memeber = 3
    }
    public enum SeatStatus
    {
        Open = 1,
        OnHold = 2,
        Book = 3
    }
    public enum VehicleTypeLookup
    {
        Bus = 1
    }
    public enum VoucherType
    {
        Invoice,
        Payment,
        RevOrRefInvoice,
        RefundPayment,
        CreditNote
    }

    public enum CrdNoteTypeLookup
    {
        Refund = 1,
        Gift = 2,
        ReferralGift = 3,
        AdvancePayment = 4,
    }

    public enum PaymentGateway
    {
        TMM = 1,
        TMD = 2
    }
    public enum PaymentMethodType
    {
        BankTransfer = 1,
        CreditCard = 2
    }
    public enum PaymentTypeEnum
    {
        TMDBankTransferCBD = 26,
        TMDPayGateway = 37
    }
}
