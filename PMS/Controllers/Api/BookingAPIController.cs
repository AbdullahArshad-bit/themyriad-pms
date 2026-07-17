using AutoMapper;
using Newtonsoft.Json;
using PMS.Common;
using PMS.DTO;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.EF;
using PMS.Filters;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.Setup;
using PMS.Services.Services.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Cors;
using PMS.Common.Classes;
using PMS.DTO.ViewModels.SetupViewModels;
using System.Net.Mail;


namespace PMS.Controllers.Api
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [BasicAuthenticationApi]
    public class BookingAPIController : ApiController
    {

        private UnitOfWork<PMSEntities> uow;
        private ISetupService setupService;
        private IEmailService emailService;
        public BookingAPIController(UnitOfWork<PMSEntities> _uow, ISetupService _setupService, IEmailService _emailService)
        {
            uow = _uow;
            setupService = _setupService;
            emailService = _emailService;
        }

        [HttpGet]
        [Route("api/GetLocations")]
        public DTO.ApiResponse<List<DTO.ViewModels.SetupViewModels.GetLocation>> GetLocations(string culture)
        {
            try
            {
                var data = uow.GenericRepository<Location>().Table.Where(x => x.IsEnable == true).Select(x => new
                {
                    x.LocationID,
                    LocationName = culture.Contains("Ar-") ? x.Ar_LocationName : x.LocationName,
                    x.Prefix
                }).ToList();
                List<DTO.ViewModels.SetupViewModels.GetLocation> locationdata = new List<DTO.ViewModels.SetupViewModels.GetLocation>();
                foreach (var item in data)
                {
                    DTO.ViewModels.SetupViewModels.GetLocation location = new DTO.ViewModels.SetupViewModels.GetLocation()
                    {
                        LocationID = item.LocationID,
                        LocationName = item.LocationName,
                        Prefix = item.Prefix

                    };
                    locationdata.Add(location);


                }
                return new ApiResponse<List<DTO.ViewModels.SetupViewModels.GetLocation>>
                {
                    Success = true,
                    Code = Convert.ToInt32(HttpStatusCode.OK),
                    Message = "Record Fetched Successfully!",
                    Data = locationdata
                };
            }

            catch (Exception ex)
            {
                return new ApiResponse<List<DTO.ViewModels.SetupViewModels.GetLocation>>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,
                    Data = null
                };
            }
        }

        [HttpGet]
        [Route("Booking/Search")]
        public ApiResponse<BookingSearchVM> GetRoomTypesByLocationId(string currentCulture, string location, string roomType, string duration, string university = null)
        {
            try
            {
                var searchModel = setupService.GetBookingSearches(location, roomType, duration, university, currentCulture);

                if (searchModel.SearchResult != null)
                {
                    return new ApiResponse<BookingSearchVM>
                    {
                        Success = true,
                        Code = Convert.ToInt32(HttpStatusCode.OK),
                        Message = "Data Retrived Successfully.",
                        Data = searchModel
                    };

                }
                else
                {
                    return new ApiResponse<BookingSearchVM>
                    {
                        Success = true,
                        Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                        Message = "Unable to Retrived Data.",
                        Data = null
                    };
                }
            }

            catch (Exception ex)
            {
                return new ApiResponse<BookingSearchVM>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,
                    Data = null
                };
            }
        }

        [HttpGet]
        [Route("Booking/RoomDetails")]
        public ApiResponse<BookingSearchVM> GetRoomDetails(string currentCulture, string location)
        {
            try
            {
                var searchModel = setupService.GetRoomsDetail(location, currentCulture);

                if (searchModel.SearchResult != null)
                {
                    return new ApiResponse<BookingSearchVM>
                    {
                        Success = true,
                        Code = Convert.ToInt32(HttpStatusCode.OK),
                        Message = "Data Retrived Successfully.",
                        Data = searchModel
                    };

                }
                else
                {
                    return new ApiResponse<BookingSearchVM>
                    {
                        Success = true,
                        Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                        Message = "Unable to Retrived Data.",
                        Data = null
                    };
                }
            }

            catch (Exception ex)
            {
                return new ApiResponse<BookingSearchVM>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,
                    Data = null
                };
            }
        }

        [HttpGet]
        [Route("api/GetRoomTypesByLocationId")]
        public DTO.ApiResponse<List<DTO.ViewModels.SetupViewModels.AddRoomTypeVM>> GetRoomTypesByLocationId(int LocationId, string culture)
        {
            try
            {
                var data = setupService.GetRoomTypesForAPI().Where(x => x.LocationId == LocationId).ToList();
                List<DTO.ViewModels.SetupViewModels.AddRoomTypeVM> roomtypesdata = new List<DTO.ViewModels.SetupViewModels.AddRoomTypeVM>();
                foreach (var item in data)
                {
                    DTO.ViewModels.SetupViewModels.AddRoomTypeVM location = new DTO.ViewModels.SetupViewModels.AddRoomTypeVM()
                    {
                        RoomTypeID = item.RoomTypeID,
                        RoomName = culture.StartsWith("en-") ? item.RoomName : item.Ar_RoomName,
                        LocationId = item.LocationId

                    };
                    roomtypesdata.Add(location);
                }

                return new ApiResponse<List<DTO.ViewModels.SetupViewModels.AddRoomTypeVM>>
                {
                    Success = true,
                    Code = Convert.ToInt32(HttpStatusCode.OK),
                    Message = "Record Fetched Successfully!",
                    Data = roomtypesdata
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<DTO.ViewModels.SetupViewModels.AddRoomTypeVM>>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,
                    Data = null
                };
            }
        }

        [HttpGet]
        [Route("api/GetTermsRoomTypeID")]
        public DTO.ApiResponse<List<DTO.ViewModels.SetupViewModels.TermsDropdown>> GetTermsRoomTypeID(int RoomTypeId, string culture)
        {
            try
            {
                var data = setupService.GetPriceConfigs().Where(x => x.RoomTypeID == RoomTypeId && x.Term.IsPublished == true && x.Term.UniversityId == null).ToList();
                List<DTO.ViewModels.SetupViewModels.TermsDropdown> roomtypesdata = new List<DTO.ViewModels.SetupViewModels.TermsDropdown>();
                foreach (var item in data)
                {
                    DTO.ViewModels.SetupViewModels.TermsDropdown location = new DTO.ViewModels.SetupViewModels.TermsDropdown()
                    {
                        TermName = culture.StartsWith("en-") ? item.Term.TermName : item.Term.AR_TermName,
                        TermDescription = culture.StartsWith("en-") ? item.Term.TermDescription : item.Term.Ar_TermDescription,
                        TermID = item.TermID,

                    };
                    if (roomtypesdata.Any(x => x.TermName == location.TermName) != true)
                        roomtypesdata.Add(location);
                }

                return new ApiResponse<List<DTO.ViewModels.SetupViewModels.TermsDropdown>>
                {
                    Success = true,
                    Code = Convert.ToInt32(HttpStatusCode.OK),
                    Message = "Record Fetched Successfully!",
                    Data = roomtypesdata
                };
            }

            catch (Exception ex)
            {
                return new ApiResponse<List<DTO.ViewModels.SetupViewModels.TermsDropdown>>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,
                    Data = null
                };
            }
        }

        [HttpGet]
        [Route("api/location/getall")]
        public ApiResponse<LocationPageApiResponse> GetAllLocation(int LocationId)
        {
            Globals.BaseUrl = Helper.GetBaseUrl(Request);
            try
            {
                var data = setupService.GetLocations().Where(x => x.LocationID == LocationId).FirstOrDefault();
                var roomtypes = setupService.GetRoomTypesForAPI().Where(x => x.LocationId == LocationId).ToList();
                var roomtypeFeatures = setupService.GetAllRoomTypeFeatures();

                LocationPageApiResponse response = new LocationPageApiResponse();
                response.RoomFeatures = roomtypeFeatures;
                response.RoomTypes = roomtypes;


                Globals.BaseUrl = Helper.GetBaseUrl(Request);
                return new ApiResponse<LocationPageApiResponse>
                {
                    Success = true,
                    Code = Convert.ToInt32(HttpStatusCode.OK),
                    Message = "",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<LocationPageApiResponse>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,
                    Data = null
                };
            }
        }

        [HttpGet]
        [Route("api/location/Get")]
        public DTO.ApiResponse<EF.RoomType> GetImagesById(int RoomTypeId)
        {
            var data = setupService.GetRoomTypeByID(RoomTypeId);

            return new ApiResponse<EF.RoomType>
            {
                Success = true,
                Code = Convert.ToInt32(HttpStatusCode.OK),
                Message = "",
                Data = data
            };
        }

        [HttpPost]
        [Route("api/BookNow")]
        public ApiResponse<object> Booking([FromBody] BookingVM bookingVM)
        {

            try
            {

                var booking = setupService.AddNewBooking(bookingVM);
                if (booking.BookingID > 0)
                {
                    var data = new { BookingNumber = booking.BookingNumber };
                    return new ApiResponse<object>()
                    {
                        Success = true,
                        Code = Convert.ToInt32(HttpStatusCode.OK),
                        Message = "Booking successfully generated.",
                        Data = data
                    };

                }
                else
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                        Message = "",

                    };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,

                };
            }
        }

        [HttpGet]
        [Route("api/booking/exists")]
        public ApiResponse<IsAlreadyBookedResponse> CheckBookingExists([FromUri] string email, int LocationId)
        {
            try
            {
                bool status = false;
                status = setupService.IsAlreadyBooked(email, LocationId);
                return new ApiResponse<IsAlreadyBookedResponse>
                {
                    Success = true,
                    Code = Convert.ToInt32(HttpStatusCode.OK),
                    Message = "",
                    Data = new IsAlreadyBookedResponse
                    {
                        Status = status
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<IsAlreadyBookedResponse>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,
                    Data = null
                };
            }
        }

        [HttpGet]
        [Route("api/booking/GetRoomTypePriceDetilByPriceId")]
        public ApiResponse<V_RoomTypePriceDetail> GetRoomTypePriceDetail(int RoomTypePriceId)
        {
            try
            {
                var data = setupService.roomTypePriceDetail(RoomTypePriceId);

                return new ApiResponse<V_RoomTypePriceDetail>
                {
                    Success = true,
                    Code = Convert.ToInt32(HttpStatusCode.OK),
                    Message = "",
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<V_RoomTypePriceDetail>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,
                    Data = null
                };
            }
        }

        [HttpGet]
        [Route("api/booking/GetPaymentGatewaybyLocationId")]
        public ApiResponse<LocationSettingsVM> GetPaymentGateway(int LocationId/*, string id*/)
        {
            try
            {
                var data = setupService.paymentGateway(LocationId/*,id*/);

                // Map to DTO
                var dto = new LocationSettingsVM
                {
                    LocationId = data.LocationId,
                    PaymentGateWay = data.PaymentGateWay
                };

                return new ApiResponse<LocationSettingsVM>
                {
                    Success = true,
                    Code = Convert.ToInt32(HttpStatusCode.OK),
                    Message = "",
                    Data = dto
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<LocationSettingsVM>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,
                    Data = null
                };
            }
        }

        [HttpGet]
        [Route("api/booking/GetUniversitiesByLocId")]
        public ApiResponse<List<UniversitiesVM>> GetUniversitiesByLocId(int Id, string culture, string university = null)
        {
            try
            {
                var data = setupService.GetUniversityListByLoactionIdAPI(Id, culture, university);

                return new ApiResponse<List<UniversitiesVM>>
                {
                    Success = true,
                    Code = Convert.ToInt32(HttpStatusCode.OK),
                    Message = "",
                    Data = data
                };
            }

            catch (Exception ex)
            {
                return new ApiResponse<List<UniversitiesVM>>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,
                    Data = null
                };
            }
        }

        [HttpGet]
        [Route("api/booking/CheckMyriadIdAndEmail")]
        public ApiResponse<bool> CheckMyriadIdAndEmail(string myriadID, string email)
        {
            try
            {
                var isMatch = setupService.CheckMyriadIdAndEmail(myriadID, email);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Code = (int)HttpStatusCode.OK,
                    Message = "Records checked successfully.",
                    Data = isMatch
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Code = (int)HttpStatusCode.InternalServerError,
                    Message = "Error checking records: " + ex.Message,
                    Data = false
                };
            }
        }

        [HttpPost]
        [Route("api/booking/RefundRequest")]
        public ApiResponse<object> RefundRequest(RefundRequestVM requestVM)
        {
            try
            {
                var refundRequest = setupService.RefundRequest(requestVM);

                return new ApiResponse<object>()
                {
                    Success = true,
                    Code = Convert.ToInt32(HttpStatusCode.OK),
                    Message = "Refund Request successfully generated.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,

                };
            }
        }

        [HttpPost]
        [Route("api/booking/GENERATEVerificationCode")]
        public ApiResponse<object> GenerateVerificationCode(string email, int? LocId)
        {
            try
            {
                // Replicate the working website behaviour for verification emails,
                // including SMTP host/port and From display name based on location.
                var code = GenerateRandomNumber(3);
                var exist = uow.GenericRepository<EmailVerification>().Table.Where(x => x.EmailAddress.ToLower() == email.ToLower()).FirstOrDefault();
                if (exist == null)
                {
                    EmailVerification verification = new EmailVerification();
                    verification.EmailAddress = email.ToLower();
                    verification.VerificationCode = code.ToString();
                    verification.CreatedDate = DateTime.Now;
                    uow.GenericRepository<EmailVerification>().Insert(verification);
                    uow.SaveChanges();
                    SendVerificationEmail("The Myriad Account Verification!", "Your Verification code is: " + verification.VerificationCode, false, email, LocId);

                }
                else
                {
                    exist.VerificationCode = code.ToString();
                    uow.GenericRepository<EmailVerification>().Update(exist);
                    uow.SaveChanges();
                    SendVerificationEmail("The Myriad Account Verification!", "Your Verification code is: " + exist.VerificationCode, false, email, LocId);

                }

                return new ApiResponse<object>
                {
                    Success = true,
                    Code = Convert.ToInt32(HttpStatusCode.OK),
                    Message = "Email Sent Successfully!",
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,
                };
            }
        }

        /// <summary>
        /// Sends the verification email using the same SMTP settings logic
        /// that is used in the website project (Globals-based configuration).
        /// </summary>
        private bool SendVerificationEmail(string subject, string body, bool isBodyHtml, string to, int? locationID = 2)
        {
            bool ret = false;

            // LocationID 1 = Dubai (EmailDisplayName), other = Muscat (EmailMuscatDisplayName)
            var fromAddress = Common.mailbody.FromEmail;
            var fromPassword = Common.mailbody.FromEmailPassword;
            var displayName = (locationID == 1)
                ? Common.mailbody.EmailDisplayName
                : Common.mailbody.EmailMuscatDisplayName;

            try
            {
                using (var message = new MailMessage())
                using (var smtp = new SmtpClient())
                {
                    message.From = new MailAddress(fromAddress, displayName);
                    message.To.Add(new MailAddress(to));
                    message.Subject = subject;
                    message.IsBodyHtml = isBodyHtml;
                    message.Body = body;

                    smtp.Port = Common.mailbody.EmailSmtpPort;
                    smtp.Host = Common.mailbody.EmailSmtpHost;
                    smtp.EnableSsl = true;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(fromAddress, fromPassword);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                    smtp.Send(message);
                    ret = true;
                }
            }
            catch
            {
                ret = false;
            }

            return ret;
        }

        [HttpPost]
        [Route("api/VerifyEmailAddress")]
        public ApiResponse<object> verifyCodeByEmailAddress(string email, string code)
        {
            if (email == null)
            {

                return new ApiResponse<object>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = "Someting Went Wrong Please Try Again!",
                };

            }
            var email1 = uow.GenericRepository<EmailVerification>().Table.Where(x => x.EmailAddress.ToLower() == email.ToLower()).FirstOrDefault();
            if (email1 != null)
            {
                if (email1.VerificationCode == code)
                {
                    email1.Verified = true;
                    email1.VerifiedDate = DateTime.Now;
                    uow.GenericRepository<EmailVerification>().Update(email1);
                    uow.SaveChanges();


                    return new ApiResponse<object>
                    {
                        Success = true,
                        Code = Convert.ToInt32(HttpStatusCode.OK),
                        Message = "Email Verified Successfully!",
                    };
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Code = Convert.ToInt32(HttpStatusCode.NotFound),
                        Message = "Please enter correct verification code!",
                    };

                }

            }
            return new ApiResponse<object>
            {
                Success = false,
                Code = Convert.ToInt32(HttpStatusCode.NotFound),
                Message = "Someting Went Wrong Please Try Again!",
            };

        }

        public static Int64 GenerateRandomNumber(int size)
        {
            Random random = new Random((int)DateTime.Now.Ticks);
            StringBuilder builder = new StringBuilder();
            string s;
            for (int i = 0; i < size; i++)
            {
                s = Convert.ToString(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(s);
            }
            var number = Convert.ToInt64((builder.ToString()));
            if (number < 0)
            {
                number = number * -1;
            }
            return number;
        }

        [HttpGet]
        [Route("api/booking/GetLocationSettingsByLocId")]
        public ApiResponse<LocationSettingsApiVM> GetLocationSettingsByLocId(int Id, string culture)
        {
            try
            {
                var data = setupService.GetLocationSettingByLocationid(Id, culture);

                return new ApiResponse<LocationSettingsApiVM>
                {
                    Success = true,
                    Code = Convert.ToInt32(HttpStatusCode.OK),
                    Message = "",
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<LocationSettingsApiVM>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,
                    Data = null
                };
            }
        }

        [HttpGet]
        [Route("api/booking/GetAvailableRoomsForLandingPage")]
        public ApiResponse<List<GetAvailableRoomsForLandingPage_Result>> GetAvailableRoomsForLandingPage(string university = null, int? LocationId = null)
        {
            try
            {
                if (university == null)
                {
                    university = "";
                }
                var data = setupService.GetAvailableRoomsForLandingPage(LocationId == null ? 16 : (int)LocationId, university);

                return new ApiResponse<List<GetAvailableRoomsForLandingPage_Result>>
                {
                    Success = true,
                    Code = Convert.ToInt32(HttpStatusCode.OK),
                    Message = "",
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<GetAvailableRoomsForLandingPage_Result>>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,
                    Data = null
                };
            }
        }
    }
}