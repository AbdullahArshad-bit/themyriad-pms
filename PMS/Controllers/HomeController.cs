 using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.OAuth2PlatformClient;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels.DashboardViewModel;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data.SqlClient;
using Intuit.Ipp.Core;
using Intuit.Ipp.Security;
using Intuit.Ipp.Security;
using PMS.DTO.ViewModels.PersonManageViewModels;
using iTextSharp.text;
using PMS.DTO.ViewModels;
using NPOI.SS.Formula.Functions;
using PMS.DTO.ViewModels.ApiViewModels;
using iTextSharp.tool.xml.html.head;
using System.Web.UI.WebControls;
using System.Web.Services.Description;
using PMS.Services.Services.LocationContext;
using Newtonsoft.Json;
using Intuit.Ipp.Exception;
using PMS.DTO;
using System.Net;
using PMS.Services.Services.Reporting;
using PMS.DTO.ViewModels.ReportingViewModels;

namespace PMS.Controllers
{

    public class HomeController : BaseController
    {
        public static string clientid = ConfigurationManager.AppSettings["clientid"];
        public static string clientsecret = ConfigurationManager.AppSettings["clientsecret"];
        public static string redirectUrl = ConfigurationManager.AppSettings["redirectUrl"];
        public static string environment = ConfigurationManager.AppSettings["appEnvironment"];
        public static string realmIds = ConfigurationManager.AppSettings["realmId"];
        //public string consumerKey = ConfigurationManager.AppSettings["ABM0Kl35A9bCrINVaSgL9lSHmepoJ5QYMskssi1lhHsaR3qpgz"];
        //public string consumerSecret = ConfigurationManager.AppSettings["wcGmPa5t35JS7isIIy6dyMbmqFHiwqDZfVa7IgGX"];
        public string accessToken = ConfigurationManager.AppSettings["YourAccessToken"];
        public string accessTokenSecret = ConfigurationManager.AppSettings["YourAccessTokenSecret"];
        public static string tokenEndpoint = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";
        public static OAuth2Client auth2Client = new OAuth2Client(clientid, clientsecret, redirectUrl, environment);

        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IReportingService reportingService;
        private readonly ILocationContextService locationContextService;

        public HomeController(UnitOfWork<PMSEntities> _uow, IReportingService _reportingService, ILocationContextService _locationContextService)
        {
            uow = _uow;
            reportingService = _reportingService;
            locationContextService = _locationContextService;
        }

        public ActionResult Index()
        {
            return View();
        }
        [AuthorizeUser(Roles = AppUserRoles.view_dashboard)]
        public ActionResult Dashboard()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();
            var assignedLocationids = assignedLocationIds?.FirstOrDefault() ?? 0;

            var model = uow.Context.Database.SqlQuery<Dashboard>(
                "EXEC GetDashboardStat @LocationId",
                new SqlParameter("@LocationId", (object)assignedLocationids ?? DBNull.Value)
            ).FirstOrDefault() ?? new Dashboard();

            double occupancy = 0;
            if (model.TotalBedSpaces.HasValue && model.TotalBedSpaces.Value > 0 && model.TotalBedSpaceOccupied.HasValue)
            {
                occupancy = (double)model.TotalBedSpaceOccupied.Value / model.TotalBedSpaces.Value * 100;
            }
            Dashboard dashboard = new Dashboard
            {
                LocationId = model.LocationId,
                TotalPaymentsThisMonth = model.TotalPaymentsThisMonth,
                TotalPaymentsThisYear = model.TotalPaymentsThisYear,
                TotalBedSpaces = model.TotalBedSpaces,
                TotalBedSpaceOccupied = model.TotalBedSpaceOccupied,
                ReservedButNotCheckedin = model.ReservedButNotCheckedin,
                BookingsNotAssignedPlacement = model.BookingsNotAssignedPlacement,
                TodaysCheckedIn = model.TodaysCheckedIn,
                TodaysCheckedOut = model.TodaysCheckedOut,
                ToBeCheckedinToday = model.ToBeCheckedinToday,
                ToBeCheckedOutToday = model.ToBeCheckedOutToday,
                ContractsToBeSigned = model.ContractsToBeSigned,
                ContractsToBeGenerated = model.ContractsToBeGenerated,
                TotalBedSpaceOccupancyPercent = Math.Round((double)occupancy, 2)
            };

            var list = uow.Context.GetPendingFeeAssesmentThisMonth(assignedLocationids).ToList();
            List<PendingFeeAssesment> pendingFeeAssesments = new List<PendingFeeAssesment>();
            foreach (var item in list)
            {
                PendingFeeAssesment pendingFee = new PendingFeeAssesment
                {
                    PersonID = item.PersonID,
                    code = item.code,
                    FullName = item.FullName,
                    LastPaid = item.LastPaid
                };

                pendingFeeAssesments.Add(pendingFee);
            }
            var lists = uow.Context.GetInvoicesTillNextWeek(assignedLocationids).ToList();
            List<unpaidInvoice> unpaidInvoices = new List<unpaidInvoice>();
            foreach (var item in lists)
            {
                unpaidInvoice unpaid = new unpaidInvoice
                {
                    InvoiceId = item.InvoiceId,
                    StudentId = item.StudentId,
                    Personcode = item.PersonCode,
                    FullName = item.FullName,
                    InvoiceDate = item.InvoiceDate,
                    CheckOut = item.CheckOut,
                    TillDate = item.TillDate,
                    PaidStatus = item.PaidStatus,
                };

                unpaidInvoices.Add(unpaid);
            }

            ViewBag.PendingFee = pendingFeeAssesments;
            ViewBag.unpaidinvoice = unpaidInvoices;
            if (TempData.ContainsKey("ShowMessage") && TempData.ContainsKey("MessageType"))
            {
                ViewBag.MessageType = TempData["MessageType"];
                ViewBag.ShowMessage = TempData["ShowMessage"];
            }
            return View(dashboard);
        }
        [Route("unauthorized")]
        public ActionResult Unauthorized()
        {
            bool aut = HttpContext.User.IsInRole("hr");

            return View();
        }

        [Route("img-upload")]
        public ActionResult UploadImages()
        {
            DTO.ViewModels.ImageUploadVM model = new DTO.ViewModels.ImageUploadVM();
            model.SavedImages = new List<string>();

            string dir = Server.MapPath("~/Assets/Images/Uploads");
            if (System.IO.Directory.Exists(dir))
            {
                var images = System.IO.Directory.EnumerateFiles(dir).ToList();
                foreach (var img in images)
                {
                    string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority;
                    string imgPath = baseUrl + "/Assets/Images/Uploads/" + System.IO.Path.GetFileName(img);
                    model.SavedImages.Add(imgPath);
                }
            }

            return View(model);
        }
        public ActionResult TestBody()
        {
            return View();
        }

        public ActionResult TestBodyArabic()
        {
            return View();
        }
        //public ActionResult Index()
        //{
        //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        //    //Session.Clear();
        //    //Session.Abandon();
        //    Request.GetOwinContext().Authentication.SignOut("Cookies");
        //    return View();
        //}

        /// <summary>
        /// Start Auth flow
        /// </summary>
        public ActionResult InitiateAuth()
        {
            //switch (submitButton)
            //{
            //    case "Connect to QuickBooks":
            List<OidcScopes> scopes = new List<OidcScopes>();
            scopes.Add(OidcScopes.Accounting);
            string authorizeUrl = auth2Client.GetAuthorizationURL(scopes);
            return Redirect(authorizeUrl);

        }

        /// <summary>
        /// QBO API Request
        /// </summary>
        public async Task<ActionResult> ApiCallService(AddPersonVM personvm)
        {
            var client = uow.GenericRepository<ClientIntegration>().Table
                .FirstOrDefault(x => x.Client_Name == "QuickBooks");

            DateTime date = client?.Last_Journal_Entry ?? DateTime.Today.AddDays(-1);

            if (client == null)
            {
                // Handle the case when the client object is null
                return RedirectToAction("InitiateAuth", "Home");
            }

            if (DateTime.Parse(client.Access_Token_Expiry) <= DateTime.Now)
            {
                if (client.Refresh_Token == null)
                {
                    // Handle the case when the refresh token is null
                    return RedirectToAction("InitiateAuth", "Home");
                }

                var tokenClient = new TokenClient(tokenEndpoint, clientid, clientsecret);
                TokenResponse response = await tokenClient.RequestRefreshTokenAsync(client.Refresh_Token);
                if (response.IsError)
                {
                    return RedirectToAction("InitiateAuth", "Home");
                }
                else
                {
                    client.Access_Token = response.AccessToken;
                    client.Access_Token_Expiry = DateTime.Now.AddSeconds(response.AccessTokenExpiresIn).ToString();
                    uow.GenericRepository<ClientIntegration>().Update(client);
                    uow.SaveChanges();
                }
            }

            string realmId = realmIds.ToString();

            try
            {
                var principal = User as ClaimsPrincipal;
                OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(client.Access_Token);
                ServiceContext serviceContext = new ServiceContext(realmId, IntuitServicesType.QBO, oauthValidator);
                serviceContext.IppConfiguration.MinorVersion.Qbo = "65";
                DataService dataService = new DataService(serviceContext);

                if (personvm == null)
                {
                    JournalEntry journalEntryRequest = CreateJournalEntry(serviceContext, date);
                    JournalEntry journalEntryResponse = dataService.Add<JournalEntry>(journalEntryRequest);
                }
                else
                {

                    //List<Account> createAccountRequest = CreateAccount(serviceContext, date);
                    TaxAgency taxAgencyObject = new TaxAgency(); // Get the TaxAgency object that you want to delete
                    string name = taxAgencyObject.DisplayName;
                    string taxPercentage = taxAgencyObject.SalesTaxCountry;

                    DeleteTaxAgency(serviceContext, taxAgencyObject);

                    //       var invoices = uow.GenericRepository<EF.Invoicing>().Table
                    //.Where(x => DbFunctions.TruncateTime(x.CreatedDate) == DateTime.Today && x.IsApproved == true)
                    //.ToList();

                    //       foreach (var invoice in invoices)
                    //       {
                    //           string customerId = invoice.StudentId.ToString(); // Convert the customer ID to string
                    //           string itemName = invoice.Code; // Replace with the actual item name from the invoice
                    //           decimal amount = invoice.NetAmount; // Replace with the actual amount from the invoice

                    //           // Retrieve the customer name based on the customer ID
                    //           int customerIdInt;
                    //           bool isValidCustomerId = int.TryParse(customerId, out customerIdInt);
                    //           if (isValidCustomerId)
                    //           {
                    //               var customer = uow.GenericRepository<EF.Person>().Table.FirstOrDefault(p => p.PersonID == customerIdInt);

                    //               Invoice invoiceRequest = CreateInvoice(serviceContext, customerId, itemName, amount);
                    //               Invoice createdInvoice = dataService.Add(invoiceRequest);
                    //           }
                    //           else
                    //           {
                    //               // Handle the case when customerId is not a valid integer
                    //           }


                    //           // Perform any necessary operations with the createdInvoice object
                    //       }






                    //List<Customer> createCustomerRequest = CreateCustomer(serviceContext, date);
                    //Batch batch = dataService.CreateNewBatch();
                    //Random rnd = new Random();

                    //foreach (Customer customer in createCustomerRequest)
                    //{
                    //    int num = rnd.Next();
                    //    batch.Add(customer, "Bid" + num, OperationEnum.create);
                    //}
                    //batch.Execute();
                }

                client.Last_Journal_Entry = DateTime.Now;
                uow.GenericRepository<ClientIntegration>().Update(client);
                uow.SaveChanges();

                TempData["MessageType"] = "success";
                TempData["ShowMessage"] = "Journal entries sent successfully";

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["MessageType"] = "error";
                TempData["ShowMessage"] = "QBO API call failed! Error message: " + ex.Message + ex.InnerException?.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Use the Index page of App controller to get all endpoints from discovery url
        /// </summary>
        /// 
        //        public static Invoice CreateInvoice(ServiceContext serviceContext, string customerId, string itemName, decimal amount)
        //        {

        //            // Create a new invoice object
        //            Invoice invoice = new Invoice();

        //            // Set the necessary properties for the invoice
        //            invoice.CustomerRef = new ReferenceType { Value = customerId };
        //            invoice.Line = new Line[]
        //            {
        //      new Line
        //{
        //    DetailType = LineDetailTypeEnum.SalesItemLineDetail,
        //    Amount = 100.0m,
        //    AnyIntuitObject = new SalesItemLineDetail
        //    {
        //        ItemRef = new ReferenceType { name = "Services", Value = "1" }
        //    }
        //}

        //            };

        //            // Create the invoice in QuickBooks
        //            DataService dataService = new DataService(serviceContext);
        //            //Invoice createdInvoice = dataService.Add(invoice);

        //            return invoice;
        //        }

        public void DeleteTaxAgency(ServiceContext serviceContext, TaxAgency taxAgency)
        {
            var dataService = new DataService(serviceContext);

            // Delete the TaxAgency using the provided TaxAgency object
            dataService.Delete(taxAgency);
        }



        private string GetAccountSubTypeForAccountType(AccountTypeEnum accountType)
        {
            switch (accountType)
            {
                case AccountTypeEnum.OtherCurrentAsset:
                    return "OtherCurrentAsset";

                case AccountTypeEnum.Expense:
                    return "Expense";

                case AccountTypeEnum.Income:
                    return "SalesOfProductIncome";

                case AccountTypeEnum.OtherAsset:
                    return "OtherAsset";

                case AccountTypeEnum.OtherCurrentLiability:
                    return "CurrentLiabilities"; // Replace with the correct enumeration value for other current liabilities subtype

                default:
                    return "";
            }
        }



        //private List<Account> CreateAccount(ServiceContext serviceContext, DateTime date)
        //{
        //    var today = DateTime.Today;
        //    var res = uow.GenericRepository<EF.ChartOfAccount>().Table
        //        .Where(x => DbFunctions.TruncateTime(x.CreatedDate) == today || x.CreatedDate < today)
        //        .ToList();

        //    List<Account> createAccountRequest = new List<Account>();

        //    DataService dataService = new DataService(serviceContext);

        //    foreach (var item in res)
        //    {
        //        Account account = new Account();
        //        account.Name = item.Name;

        //        if (!string.IsNullOrEmpty(item.AccountType.QuickBooksAccountType))
        //        {
        //            if (Enum.TryParse<AccountTypeEnum>(item.AccountType.QuickBooksAccountType, out AccountTypeEnum accountType))
        //            {
        //                account.AccountType = accountType;
        //            }
        //            else
        //            {
        //                account.AccountType = AccountTypeEnum.OtherAsset;
        //            }
        //        }
        //        else
        //        {
        //            account.AccountType = AccountTypeEnum.OtherAsset;
        //        }

        //        // Assign an appropriate value to the AccountSubType property based on the AccountType
        //        account.AccountSubType = GetAccountSubTypeForAccountType(account.AccountType);

        //        // Create the account in QuickBooks
        //        Account createdAccount = dataService.Add(account);

        //        // Save the QuickBooks account ID in the ChartOfAccount table
        //        item.QuickBooksAccountId = createdAccount.Id; // Assuming there is a QuickBooksAccountId property in the ChartOfAccount entity

        //        // Add the account to the createAccountRequest list
        //        createAccountRequest.Add(createdAccount);
        //    }

        //    // Save the changes to the database
        //    uow.SaveChanges();

        //    return createAccountRequest;
        //}

        private List<Customer> CreateCustomer(ServiceContext context, DateTime date)
        {
            var res = uow.GenericRepository<EF.Person>().Table
                .Where(x => DbFunctions.TruncateTime(x.CreatedDate) == DateTime.Today)
                .ToList();
            List<Customer> customlist = new List<Customer>();


            foreach (var item in res)
            {
                Customer customer = new Customer();
                customer.DisplayName = item.FullName;
                customer.Title = item.Title;
                customer.PrimaryEmailAddr = new Intuit.Ipp.Data.EmailAddress { Address = item.Email };
                customer.PrimaryPhone = new Intuit.Ipp.Data.TelephoneNumber { FreeFormNumber = Convert.ToString(item.Phone) };

                customlist.Add(customer);
            }


            return customlist;

        }

        public JournalEntry CreateJournalEntry(ServiceContext context, DateTime date)
        {

            var res = uow.GenericRepository<EF.StudentLedger>().Table
                .Where(x => DbFunctions.TruncateTime(x.CreatedDate) == DateTime.Today && x.CreatedDate > date
                && x.Invoicing.CreatedDate > date && x.Invoicing.IsApproved == true && x.IsApproved == true)
                .ToList();


            //Create JournalEntry Request
            JournalEntry journalEntry = new JournalEntry();
            journalEntry.Adjustment = true;
            journalEntry.AdjustmentSpecified = true;
            journalEntry.DocNumber = "TheMyriad" + Guid.NewGuid().ToString("N").Substring(0, 5);
            journalEntry.TxnDate = DateTime.UtcNow.Date;
            journalEntry.TxnDateSpecified = true;

            // creating lines for a JournalEntry
            List<Line> lineList = new List<Line>();
            foreach (var item in res)
            {
                // Create debit line
                Line debitLine = new Line();
                debitLine.Description = item.Code;
                if (item.DebitAmount != null)
                {
                    debitLine.Amount = Convert.ToDecimal(item.DebitAmount);
                }
                else
                {
                    debitLine.Amount = Convert.ToDecimal(item.CreditAmount);

                }
                debitLine.AmountSpecified = true;
                debitLine.DetailType = LineDetailTypeEnum.JournalEntryLineDetail;
                debitLine.DetailTypeSpecified = true;

                //Add JE debit line
                JournalEntryLineDetail journalEntryLineDetail = new JournalEntryLineDetail();
                journalEntryLineDetail.PostingType = PostingTypeEnum.Debit;
                journalEntryLineDetail.PostingTypeSpecified = true;

                QueryService<Account> querySvc = new QueryService<Account>(context);
                Account existingAccount = querySvc.ExecuteIdsQuery($"select * from Account where Name in ('{item.Invoicing.InvoicingDetails.Select(x => x.Service.ChartOfAccount.Name).FirstOrDefault()}','{item.PaymentTypeName}')").FirstOrDefault();
                if (item.DebitAmount == null)
                {

                    journalEntryLineDetail.AccountRef = new ReferenceType() { name = existingAccount.Name, Value = existingAccount.Id };

                }
                else
                {
                    journalEntryLineDetail.Entity = new EntityTypeRef { Type = EntityTypeEnum.Customer, EntityRef = new ReferenceType { name = "Musharaf Test", Value = "58" } };
                    journalEntryLineDetail.AccountRef = new ReferenceType() { name = "Suspense Account", Value = "100" };

                }
                debitLine.AnyIntuitObject = journalEntryLineDetail;
                lineList.Add(debitLine);

                #region Create CreditCard Account
                // Create Credit Card line

                Line creditLine = new Line();
                creditLine.Description = item.Code;
                if (item.CreditAmount != null)
                {
                    creditLine.Amount = Convert.ToDecimal(item.CreditAmount);
                }
                else
                {
                    creditLine.Amount = Convert.ToDecimal(item.DebitAmount);
                }
                creditLine.AmountSpecified = true;
                creditLine.DetailType = LineDetailTypeEnum.JournalEntryLineDetail;
                creditLine.DetailTypeSpecified = true;

                //Find or create account

                #endregion

                //Add JE credit line
                JournalEntryLineDetail journalEntryLineDetailCredit = new JournalEntryLineDetail();
                journalEntryLineDetailCredit.PostingType = PostingTypeEnum.Credit;
                journalEntryLineDetailCredit.PostingTypeSpecified = true;
                if (item.CreditAmount == null)
                {

                    journalEntryLineDetailCredit.AccountRef = new ReferenceType() { name = existingAccount.Name, Value = existingAccount.Id };

                }
                else
                {
                    journalEntryLineDetailCredit.Entity = new EntityTypeRef { Type = EntityTypeEnum.Customer, EntityRef = new ReferenceType { name = "Musharaf Test", Value = "58" } };
                    journalEntryLineDetailCredit.AccountRef = new ReferenceType() { name = "Suspense Account", Value = "100" };

                }
                creditLine.AnyIntuitObject = journalEntryLineDetailCredit;
                lineList.Add(creditLine);
            }

            // Added both Debit & Credit Lines to journal
            journalEntry.Line = lineList.ToArray();

            //Return the journal request
            return journalEntry;

        }


        //[HttpGet]
        //[Route("Home/GetBuildingStats")]
        //public ApiResponse<List<RoomInventoryStats>> GetBuildingStats(DateTime? Today)
        //{
        //    try
        //    {
        //        if (Today == null)
        //        {
        //            Today = DateTime.Now.Date;
        //        }

        //        var statsModel = reportingService.GetBuildingStatsReport(Today);

        //        if (statsModel != null)
        //        {
        //            return new ApiResponse<List<RoomInventoryStats>>
        //            {
        //                Success = true,
        //                Code = 200, // HTTP status code for success
        //                Message = "Data Retrieved Successfully.",
        //                Data = statsModel
        //            };
        //        }
        //        else
        //        {
        //            return new ApiResponse<List<RoomInventoryStats>>
        //            {
        //                Success = true,
        //                Code = 500, // Internal Server Error
        //                Message = "Unable to Retrieve Data.",
        //                ListData = null
        //            };
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse<List<RoomInventoryStats>>
        //        {
        //            Success = false,
        //            Code = 500, // Internal Server Error
        //            Message = ex.Message,
        //            ListData = null
        //        };
        //    }
        //}
        //[HttpGet]
        //public JsonResult GetBuildingData()
        //{
        //    DateTime Today = DateTime.Now.Date;
        //    var statsModel = reportingService.GetBuildingStatsReport(Today);
        //    return Json(statsModel, JsonRequestBehavior.AllowGet);
        //}
        [HttpGet]
        public JsonResult GetBuildingData()
        {
            DateTime Today = DateTime.Now.Date;
            var statsModel = reportingService.GetBuildingStatsReport(Today);
            var response = new
            {
                InHouseTodayData = statsModel.RoomStats.Select(s => new
                {
                    BuildingName = s.BuildingName,
                    RoomTypeName = s.RoomTypeName,
                    RoomsCount = s.TotalBedSpaces,
                    TotalInHouse = s.TotalInHouse
                }).ToList(),

                VacancyData = statsModel.RoomStats.Select(s => new
                {
                    BuildingName = s.BuildingName,
                    RoomTypeName = s.RoomTypeName,
                    RoomsCount = s.TotalBedSpaces,
                    Vacancy = s.Vacancy
                }).ToList(),

                ClosuresData = statsModel.RoomStats.Select(s => new
                {
                    BuildingName = s.BuildingName,
                    RoomTypeName = s.RoomTypeName,
                    RoomsCount = s.TotalBedSpaces,
                    MaintenanceBedSpaces = s.MaintenanceBedSpaces
                }).ToList(),

                VacancyRoomData = statsModel.VacancyStats.Select(s => new
                {
                    RoomName = s.RoomName,
                    EmptyRoomCount = s.EmptyRoomCount
                }).ToList(),

                CheckInsData = statsModel.RoomStats.Select(s => new
                {
                    BuildingName = s.BuildingName,
                    CheckInCount = s.CheckInCount,
                    NoShow = s.NoShow
                }).ToList(),

                CheckOutsData = statsModel.RoomStats.Select(s => new
                {
                    BuildingName = s.BuildingName,
                    CheckedOut = s.CheckedOut,
                    DueForCheckout = s.DueForCheckout
                }).ToList(),

            };

            return Json(response, JsonRequestBehavior.AllowGet);
        }

    }
}