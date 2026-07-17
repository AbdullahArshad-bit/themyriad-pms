using Ninject;
using PMS.Services.Services.Email;
using PMS.Services.Services.Account;
using PMS.Services.Services.News;
using PMS.Services.Services.StudentPortal.MovieNights;
using PMS.Services.Services.Setup;
using PMS.Services.Services.StudentPortal.Devices;
using PMS.Services.Services.StudentPortal.Transportation;
using PMS.Services.Services.UserManage;
using PMS.Services.Services.Person;
using PMS.Services.Services.Booking;
using PMS.Services.Services.BedSpacePlace;
using PMS.Services.Services.Correspondence;
using PMS.Services.Services.Contracts;
using PMS.Services.Services.Inspection;
using PMS.Services.Services.Service;
using PMS.Services.Services.Tax;
using PMS.Services.Services.Tex;
using PMS.Services.Services.PaymentTypes;
using PMS.Services.Services.ChartOfAccounts;
using PMS.Services.Services.AuditLogs;
using PMS.Services.Services.Reporting;
using PMS.Services.Services.Invoicings;
using PMS.Services.Services.CreditNote;
using PMS.Services.Services.Notifications;
using PMS.Services.Services.Payment;
using PMS.Services.Services.PaymentGateway;
using PMS.Services.Services.Ticket;
using PMS.Services.Services.TicketGroup;
using PMS.Services.Services.Sync;
using PMS.Services.Services.Jobs;
using PMS.Services.Services.JobConfuguration;
using PMS.Services.Services.Schedule;
using PMS.Services.Services.VehicleRoutes;
using PMS.Services.Services.BusStop;

using PMS.Services.Services.Vehicle;
using PMS.Services.Services.VehiclePrice;
using PMS.Services.Services.VehicleSubscription;
using PMS.Services.Services.Feedback;
using PMS.Services.Services.EmailSchedule;
using PMS.Services.Services.TTLockIntegration;
using PMS.Services.Services.TTLockRequestHandler;
using PMS.Services.Services.FeeAssessment;
using PMS.Services.Services.VoucherSystem;
using PMS.Services.Services.LocationContext;
using PMS.Services.Services.Firebase;
using PMS.Services.Services.Integration;

namespace PMS.Services
{
    public static class ServiceRegistration
    {
        public static IKernel GlobalKernel { get; private set; }
        public static void BindAll(IKernel kernel)
        {
            kernel.Bind<IEmailService>().To<EmailService>();
            kernel.Bind<IEmailScheduleServices>().To<EmailScheduleService>();
            kernel.Bind<IAccountService>().To<AccountService>();
            kernel.Bind<INewsService>().To<NewsService>();
            kernel.Bind<IMovieNightsAdmin>().To<MovieNightsAdmin>();
            kernel.Bind<ITransportationService>().To<TransportationService>();
            kernel.Bind<ISetupService>().To<SetupService>();
            kernel.Bind<IUserManageService>().To<UserManageService>();
            kernel.Bind<IPersonService>().To<PersonService>();
            kernel.Bind<IBookingService>().To<BookingService>();
            kernel.Bind<IBedSpacePlacementService>().To<BedSpacePlacementService>();
            kernel.Bind<ICorrespondenceService>().To<CorrespondenceService>();
            kernel.Bind<IContractManageService>().To<ContractManageService>();
            kernel.Bind<IInspectionService>().To<InspectionServices>();
            kernel.Bind<IServicesService>().To<ServicesService>();
            kernel.Bind<ITaxService>().To<TexService>();
            kernel.Bind<IPaymentTypesService>().To<PaymentTypesService>();
            kernel.Bind<IChartOfAccountsService>().To<ChartOfAccountsService>();
            kernel.Bind<IAuditLogsService>().To<AuditLogsService>();
            kernel.Bind<IReportingService>().To<ReportingService>();
            kernel.Bind<IInvoicingService>().To<InvoicingService>();
            kernel.Bind<ICreditNoteService>().To<CreditNoteService>();
            kernel.Bind<INotificationService>().To<NotificationService>();
            kernel.Bind<IPaymentService>().To<PaymentService>();
            kernel.Bind<IPaymentGatewayService>().To<PaymentGatewayService>();
            kernel.Bind<IPaymentGatewayFactory>().To<PaymentGatewayFactory>();
            kernel.Bind<ThwaniPaymentGateway>().ToSelf();
            kernel.Bind<NetIntPaymentGateway>().ToSelf();
            kernel.Bind<ITicketService>().To<TicketService>();
            kernel.Bind<ITicketGroupService>().To<TicketGroupService>();
            kernel.Bind<ISyncService>().To<SyncService>();
            kernel.Bind<IJobService>().To<JobService>();
            kernel.Bind<IJobConfigurationService>().To<JobConfigurationService>();
            kernel.Bind<IScheduleService>().To<ScheduleService>();
            kernel.Bind<IVehicleRoutesService>().To<VehicleRoutesService>();
            kernel.Bind<IBusStopService>().To<BusStopService>();
            kernel.Bind<IVehicleService>().To<VehicleService>();
            kernel.Bind<IVehiclePriceService>().To<VehiclePriceService>();
            kernel.Bind<IVehicleSubscriptionService>().To<VehicleSubscriptionService>();
            kernel.Bind<IFeedbackService>().To<FeedbackService>();
            kernel.Bind<ITTLockAuth>().To<TTLockAuth>();
            kernel.Bind<ITTLockRequestHandler>().To<TTLockRequestHandler>();
            kernel.Bind<IFeeAssessmentJobService>().To<FeeAssessmentJobService>();
            kernel.Bind<IVoucherService>().To<VoucherService>();
            kernel.Bind<ILocationContextService>().To<LocationContextService>();
            kernel.Bind<IPushNotificationService>().To<PushNotificationService>();
            //kernel.Bind<IPaymentGateway, NetIntPaymentGateway>();
            kernel.Bind<IFirebaseNotificationService>().To<FirebaseNotificationService>();
            kernel.Bind<IStudentDeviceService>().To<StudentDeviceService>();
            kernel.Bind<IIntegrationAuthService>().To<IntegrationAuthService>();
            kernel.Bind<IWebhookService>().To<WebhookService>();
            GlobalKernel = kernel;

        }
    }
}
