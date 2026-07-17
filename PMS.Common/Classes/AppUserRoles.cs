using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Common.Classes
{
    public class AppUserRoles
    {
        //dashboard
        public const string view_dashboard = "view_dashboard";

        //news management
        public const string view_news = "view_news";
        public const string add_news = "add_news";

        public const string view_news_detail = "view_news_detail";
        public const string add_news_detail = "add_news_detail";
        public const string delete_news_detail = "delete_news_detail";

        //movie nights
        public const string view_movies = "view_movies";
        public const string add_movie = "add_movie";
        public const string delete_movies = "delete_movies";

        public const string view_movie_vote_campaign = "view_movie_vote_campaign";
        public const string add_movie_vote_campaign = "add_movie_vote_campaign";
        public const string delete_movie_vote_campaign = "delete_movie_vote_campaign";

        public const string view_movie_vote_campaign_detail = "view_movie_vote_campaign_detail";
        public const string add_movie_vote_campaign_detail = "add_movie_vote_campaign_detail";
        public const string delete_movie_vote_campaign_detail = "delete_movie_vote_campaign_detail";



        public const string view_movie_shows = "view_movie_shows";
        public const string add_movie_shows = "add_movie_shows";
        public const string delete_movie_shows = "delete_movie_shows";


        //transportation
        public const string view_vehicles = "view_vehicles";
        public const string add_vehicles = "add_vehicles";
        public const string edit_vehicles = "edit_vehicles";
        public const string delete_vehicles = "delete_vehicles";
        public const string view_seats = "view_seats";
        public const string add_seats = "add_seats";
        public const string edit_seats = "edit_seats";
        public const string delete_seats = "delete_seats";
        public const string view_stops = "view_stops";
        public const string add_stops = "add_stops";
        public const string edit_stops = "edit_stops";
        public const string delete_stops = "delete_stops";
        public const string view_routes = "view_routes";
        public const string add_routes = "add_routes";
        public const string edit_routes = "edit_routes";
        public const string delete_routes = "delete_routes";
        public const string view_schedules = "view_schedules";
        public const string add_schedules = "add_schedules";
        public const string edit_schedules = "edit_schedules";
        public const string delete_schedules = "delete_schedules";
        public const string view_prices = "view_prices";
        public const string add_prices = "add_prices";
        public const string edit_prices = "edit_prices";
        public const string delete_prices = "delete_prices";
        public const string view_subscription = "view_subscription";
        public const string add_subscription = "view_subscription";
        public const string edit_subscription = "edit_subscription";
        public const string delete_subscription = "delete_subscription";
        public const string approve_subscription = "approve_subscription";
        public const string suspend_subscription = "suspend_subscription";
        public const string end_subscription = "end_subscription";

        public const string transportation = "transportation";

        //user manage
        public const string view_users = "view_users";
        public const string add_users = "add_users";
        public const string delete_users = "delete_users";

        public const string view_roles = "view_roles";
        public const string add_roles = "add_roles";
        public const string delete_roles = "delete_roles";

        public const string manage_role_rights = "manage_role_rights";

        //setup
        public const string living_areas = "living_areas";

        public const string view_room_feaure = "view_room_feaure";
        public const string add_room_feature = "add_room_feature";
        public const string delete_room_feature = "delete_room_feature";

        public const string view_room_types = "view_room_types";
        public const string add_room_types = "add_room_types";
        public const string delete_room_types = "delete_room_types";
        public const string upload_room_typeImages = "upload_room_typeImages";

        public const string view_terms = "view_terms";
        public const string add_terms = "add_terms";
        public const string delete_terms = "delete_terms";

        public const string view_price_config = "view_price_config";
        public const string add_price_config = "add_price_config";
        public const string delete_price_config = "delete_price_config";

        public const string View_UniversitesList = "View_UniversitesList";
        public const string add_Universites = "add_Universites";
        public const string delete_Universites = "delete_Universites";

        //Accounts Module
        public const string view_Services = "view_Services";
        public const string add_acc_Services = "add_acc_Services";
        public const string delete_acc_Services = "delete_acc_Services";

        public const string view_acc_TaxType = "view_acc_TaxType";
        public const string add_acc_TaxType = "add_acc_TaxType";
        public const string delete_acc_TaxType = "delete_acc_TaxType";

        public const string view_acc_PaymentMethod = "view_acc_PaymentMethod";
        public const string add_acc_PaymentMethod = "add_acc_PaymentMethod";
        public const string delete_acc_PaymentMethod = "delete_acc_PaymentMethod";

        public const string view_acc_COA = "view_acc_COA";
        public const string add_acc_COA = "add_acc_COA";
        public const string delete_acc_COA = "delete_acc_COA";

        public const string view_acc_Invoice = "view_acc_Invoice";
        public const string view_acc_InvoiceDetail = "view_acc_InvoiceDetail";
        public const string add_acc_Invoice = "add_acc_Invoice";
        public const string delete_acc_Invoice = "delete_acc_Invoice";
        public const string Approve_acc_Invoice = "Approve_acc_Invoice";
        public const string Edit_AfterApprove_acc_Invoice = "Edit_AfterApprove_acc_Invoice";
        public const string Edit_Invoice_Amount = "Edit_Invoice_Amount";
        public const string Add_Discount = "Add_Discount";
        public const string Reverse_Invoice = "Reverse_Invoice";

        public const string View_CheckedOut_Residents = "View_CheckedOut_Residents";

        public const string view_acc_PaymentTransection = "view_acc_PaymentTransection";
        public const string view_acc_PaymentTransectionDetail = "view_acc_PaymentTransectionDetail";
        public const string view_acc_PartnetLedger = "view_acc_PartnetLedger";
        public const string Add_acc_PaymentTransection = "Add_acc_PaymentTransection";
        public const string Edit_acc_PaymentTransection = "Edit_acc_PaymentTransection";
        public const string Edit_AfterApprove_acc_Payment = "Edit_AfterApprove_acc_Payment";
        public const string Approve_acc_PaymentTransection = "Approve_acc_PaymentTransection";
        public const string View_Payment_Link = "View_Payment_Link";
        public const string View_Deposit_Invoices = "View_Deposit_Invoices";
        public const string Refund_Deposit_Invoices = "Refund_Deposit_Invoices";
        public const string Payment_Cloned_Invoices = "Payment_Cloned_Invoices";
        public const string view_UpComing_Invoices = "view_UpComing_Invoices";
        public const string View_Refund_Payments = "View_Refund_Payments";
        public const string Pay_Refund = "Pay_Refund";
        public const string Edit_Refund_Payment = "Edit_Refund_Payment";


        //credit Note
        public const string edit_acc_CreditNote = "edit_acc_CreditNote";
        public const string add_acc_CreditNote = "add_acc_CreditNote";
        public const string view_acc_CreditNote = "view_acc_CreditNote";
        public const string view_Referral_CreditNotes = "view_Referral_CreditNotes";
        public const string approve_acc_CreditNote = "approve_acc_CreditNote";
        public const string edit_after_approve_CreditNote = "edit_after_approve_CreditNote";



        //Contract Managment
        public const string view_contracts = "view_contracts";
        public const string Add_contracts = "Add_contracts";
        public const string Delete_contracts = "Delete_contracts";
        public const string Cancel_contracts = "Cancel_contracts";

        public const string view_contractTypes = "view_contractTypes";
        public const string Add_contractTypes = "Add_contractTypes";
        public const string Delete_contractTypes = "Delete_contractTypes";

        public const string view_GenerateStudentContracts = "view_GenerateStudentContracts";
        public const string view_StudentContracts = "view_StudentContracts";
        public const string view_old_contracts = "view_old_contracts";

        //Correspondence 
        public const string View_EmailSettings = "View_EmailSettings";
        public const string Update_EmailSettings = "Update_EmailSettings";

        public const string View_EmailSenders = "View_EmailSenders";
        public const string Export_EmailSenders = "Export_EmailSenders";
        public const string Add_EmailSenders = "Add_EmailSenders";
        public const string Delete_EmailSenders = "Delete_EmailSenders";
        public const string TestEmailSendersSettings = "TestEmailSendersSettings";

        public const string View_EmailMessages = "View_EmailMessages";
        public const string Add_EmailMessages = "Add_EmailMessages";
        public const string Export_EmailMessages = "Export_EmailMessages";
        public const string Delete_EmailMessages = "Delete_EmailMessages";
        public const string View_EmailSchedulers = "View_EmailSchedulers";
        public const string Add_EmailScheduler = "Add_EmailScheduler";
        public const string Edit_EmailScheduler = "Edit_EmailScheduler";
        public const string Delete_EmailScheduler = "Delete_EmailScheduler";
        public const string Push_Notification = "Push_Notification";


        //Booking
        public const string View_Profile = "View_Profile";
        public const string Add_Profile = "Add_Profile";
        public const string Delete_Profile = "Delete_Profile";
        public const string Import_ProfileExcel = "Import_ProfileExcel";
        public const string View_PersonRelationshipHistory = "View_PersonRelationshipHistory";
        public const string Import_ProfileWithBookingExcel = "Import_ProfileWithBookingExcel";
        public const string Import_ProfileFormat = "Import_ProfileFormat";
        public const string Run_A_Fee_Assessment = "Run_A_Fee_Assessment";


        public const string View_BookingList = "View_BookingList";
        public const string Add_Booking = "Add_Booking";
        public const string Delete_Booking = "Delete_Booking";
        public const string Cancel_Booking = "Cancel_Booking";
        public const string View_BankTransfer_Receipt = "View_BankTransfer_Receipt";


        public const string View_BedSpacePlacement = "View_BedSpacePlacement";
        public const string Add_BedSpacePlacement = "Add_BedSpacePlacement";
        public const string Delete_BedSpacePlacement = "Delete_BedSpacePlacement";
        public const string Checkin_BedSpacePlacement = "Checkin_BedSpacePlacement";
        public const string CheckOut_BedSpacePlacement = "CheckOut_BedSpacePlacement";
        public const string Migrate_BedSpacePlacement = "Migrate_BedSpacePlacement";
        public const string create_resident_Acount = "create_resident_Acount";
        public const string Generate_Contract = "Generate_Contract";
        public const string View_Feedback = "View_Feedback";
        public const string ReIssue_Card = "ReIssue_Card";
        public const string Update_Placement_Dates = "Update_Placement_Dates";

        //PersonDocument
        public const string View_PersonDocument = "View_PersonDocument";
        public const string Add_PersonDocument = "Add_PersonDocument";
        public const string Delete_PersonDocument = "Delete_PersonDocument";

        //Inspection
        public const string view_InspectionList = "view_InspectionList";
        public const string add_Inspection = "add_Inspection";
        public const string delete_Inspection = "delete_Inspection";

        public const string view_InspectionFieldsList = "view_InspectionFieldsList";
        public const string add_InspectionFields = "add_InspectionFields";
        public const string delete_InspectionFields = "delete_InspectionFields";

        public const string view_InspectionRatingList = "view_InspectionRatingList";
        public const string add_InspectionRating = "add_InspectionRating";
        public const string delete_InspectionRating = "delete_InspectionRating";

        public const string view_GeneratedInspectionsList = "view_GeneratedInspectionsList";

        //Audit Trail
        public const string view_AuditTrialList = "view_AuditTrialList";

        //Reports
        public const string View_Customize_Detailed_Reports = "View_Customize_Detailed_Reports";
        public const string View_ServiceDetail_Report = "View_ServiceDetail_Report";
        public const string View_PaymentDetail_Report = "View_PaymentDetail_Report";
        public const string View_Resident_Trail_Balance_Report = "View_Resident_Trail_Balance_Report";
        public const string View_Resident_Detail_Trail_Balance_Report = "View_Resident_Detail_Trail_Balance_Report";

        public const string View_RevenueDetail_Report = "View_RevenueDetail_Report";
        public const string View_ComplaintHistory_Report = "View_ComplaintHistory_Report";
        public const string View_RoomInventory_Report = "View_RoomInventory_Report";
        public const string View_Booking_Report = "View_Booking_Report";
        public const string View_TransportationBooking_Report = "View_TransportationBooking_Report";
        public const string Cancel_Seat = "Cancel_Seat";
        public const string View_AccountLiability_Report = "View_AccountLiability_Report";
        public const string Liability_Balance_Report = "Liability_Balance_Report";
        public const string view_End_Of_Shift_Report = "view_End_Of_Shift_Report";
        public const string Export_Booking_Report = "Export_Booking_Report";
        public const string Tax_Detail_Report = "Tax_Detail_Report";
        public const string View_History_Forcast_Report = "View_History_Forcast_Report";
        public const string View_Manager_Daily_Report = "View_Manager_Daily_Report";
        public const string View_Swapped_Report = "View_Swapped_Report";
        public const string View_Ageing_Report = "View_Ageing_Report";
        public const string View_Contracts_Report = "View_Contracts_Report";
        public const string View_Accounting_Vouchers_Report = "View_Accounting_Vouchers_Report";
        public const string View_InHouseByUniversity_Report = "View_InHouseByUniversity_Report";
        public const string View_Voucher_Report = "View_Voucher_Report";
        public const string View_Invoicing_Voucher_Detail = "View_Invoicing_Voucher_Detail";
        public const string View_Payment_Voucher_Detail = "View_Payment_Voucher_Detail";
        public const string View_CreditNote_Voucher_Detail = "View_CreditNote_Voucher_Detail";
        public const string View_Occupancy_Forecast_Report = "View_Occupancy_Forecast_Report";


        //Tickets
        public const string view_ticketList = "view_TicketList";
        public const string add_Ticket = "add_Ticket";
        public const string delete_Ticket = "delete_Ticket";
        public const string update_TicketStatus = "update_TicketStatus";
        public const string update_TicketComments = "update_TicketComments";
        public const string see_All_Tickets = "see_All_Tickets";
        public const string see_Relevent_Ticket = "see_Relevent_Ticket";

        //Jobs Configuration
        public const string add_JobConfiguration = "add_JobConfiguration";

        //API Documentation
        public const string View_Api_Documentation = "View_Api_Documentation";

    }
}
