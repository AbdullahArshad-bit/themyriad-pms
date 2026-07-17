using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.DTO;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.DTO.ViewModels.PaymentViewModels;
using PMS.EF;


namespace TheMyriad.DTO.DTO_Mapings
{
    public class Mapping
    {
        public static Person MapPerson(BookingVM bookingVM)
        {

            string gender = bookingVM.Title;
            if (gender.ToUpper().Contains("MR"))
            { gender = "Male"; }
            else if (gender == "MRS" || gender == "MS")
            {
                gender = "Female";
            }
            else
            { gender = "Female"; }

            return new Person()
            {
                Title = bookingVM.Title,
                FullName = bookingVM.FullName,
                Email = bookingVM.Email,
                Phone = bookingVM.Phone,
                Gender = gender,
                DOB = bookingVM.DOB,
                Nationality = bookingVM.Nationality,
                IsEnable = true,
                CreatedDate = DateTime.Now,
                CreatedBy = bookingVM.Email,
                UpdatedDate = DateTime.Now,
                UpdatedBy = bookingVM.Email,
                LocationId = bookingVM.LocationID,
                Universiry = bookingVM.University,
                UniversityId = bookingVM.UniversityId
            };
        }

        public static PriceConfig MapPriceConfig(BookingVM bookingVM)
        {
            return new PriceConfig()
            {
                TermID = bookingVM.SelectedCommitment,
                RoomTypeID = bookingVM.RoomTypeID,
                Price = bookingVM.price,
                InitialDeposit = bookingVM.InitialDeposit,
                Currency = bookingVM.Currency,
                IsEnable = true,
                CreatedDate = DateTime.Now,
                CreatedBy = bookingVM.Email,
                UpdatedDate = DateTime.Now,
                UpdatedBy = bookingVM.Email,
                LocationId = bookingVM.LocationID
            };
        }

        public static Booking MapBooking(BookingVM bookingVM, int personID, int priceconfigID)
        {
            if (bookingVM.LocationID <= 0)
                bookingVM.LocationID = null;

            return new Booking()
            {
                PersonID = personID,
                LocationID = bookingVM.LocationID,
                PriceConfigID = priceconfigID,
                CheckInDate = bookingVM.CheckIn,
                CheckOutDate = bookingVM.CheckOut,
                IsCancel = false,
                IsEnable = true,
                CreatedDate = DateTime.Now,
                CreatedBy = bookingVM.Email,
                Channel = bookingVM.Channel,
                PaymentType = bookingVM.PaymentType,
                AccessibilityRequest = bookingVM.AccessibilityRequest,
                TranRef = bookingVM.TranRef,
                CardLastDigits = bookingVM.CardLastDigits,
                Message = bookingVM.Message,
                HearFromCode = bookingVM.HearFromCode,
                Status = false,
                HearFrom = bookingVM.HearFrom,
                HearFromOther = bookingVM.HearFromOther,
                Source=bookingVM.Prefix,
                UniReferenceNo = bookingVM.UniReferenceNo
            };
        }

        public static EmergencyContact MapEmergencyContact(BookingVM bookingVM, int personId)
        {
            return new EmergencyContact
            {
                PassportNumber = bookingVM.PassportNumber,
                FullName = bookingVM.EmergencyFullName,
                Phone = bookingVM.EmergencyPhone,
                Email = bookingVM.EmergencyEmail,
                Relation = bookingVM.Emergencyrelation == "EmergencyOther" ? bookingVM.EmergencyOther : bookingVM.Emergencyrelation,
                PersonID = personId
            };
        }
        public static SpecialRequest MapSpecialRequest(BookingVM bookingVM, int personID)
        {
            return new SpecialRequest
            {
                PreferableFloor = bookingVM.FloorPreference,
                PreferableView = bookingVM.ViewPreference,
                Religions = bookingVM.Religion,
                Nationalities = bookingVM.SNationality,
                Universities = bookingVM.SUniversity,
                Agerange = bookingVM.SAgeRange,
                PersonID = personID
            };
        }

        public static List<PreferenceDetail> MapPreference(BookingVM bookingVM, List<SharedPreference> sharedPreferences)
        {
            List<PreferenceDetail> preferenceDetails = new List<PreferenceDetail>();
            foreach (var pref in sharedPreferences)
            {
                PreferenceDetail preferenceDetail = new PreferenceDetail();
                preferenceDetail.PersonID = 0;
                preferenceDetail.Preference = false;
                preferenceDetail.PrefID = 1;

                preferenceDetails.Add(preferenceDetail);
            }

            return preferenceDetails;

        }

        public static Payment MapPayment(BookingVM bookingVM, int bookingID)
        {
            return new Payment()
            {
                PaymentMethodID = bookingVM.PaymentMethodID,
                BookingID = bookingID,
                Amount = bookingVM.Amount,
                Currency = bookingVM.Currency,
                CardLastDigits = bookingVM.CardLastDigits,
                TranRef = bookingVM.TranRef,
                Message = bookingVM.Message,
                IsEnable = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };
        }

        public static BillingAddress MapBillingAddress(BookingVM bookingVM, int paymentID)
        {

            return new BillingAddress()
            {
                PaymentID = paymentID,
                FirstName = bookingVM.FullName,
                LastName = null,
                Address = bookingVM.BillingAddress.Address,
                City = bookingVM.BillingAddress.City,
                Country = bookingVM.BillingAddress.Country

            };
        }

        #region ----- Invoice -----
        public static InvoiceViewModel MapInvoice(Invoicing entity)
        {
            var _returnModel = new InvoiceViewModel();
            _returnModel.Id = entity.Id;
            _returnModel.Code = entity.Code;
            _returnModel.Location = entity.Location.LocationName;
            _returnModel.MyriadID = entity.Person.Code;
            _returnModel.FullName = entity.Person.FullName;
            _returnModel.InvoiceDate = entity.InvoiceDate;
            _returnModel.Remarks = entity.Remarks;
            _returnModel.NetAmount = entity.NetAmount;
            _returnModel.CreatedDate = entity.CreatedDate;
            _returnModel.Status = entity.IsApproved;
            _returnModel.isPaid = entity.IsPaid;
            _returnModel.TotalDiscountAmount = entity.TotalDiscountAmount;
            _returnModel.CreatedBy = entity.UserMaster.FullName;
            _returnModel.ApprovedBy = entity.UserMaster1 == null ? "" : entity.UserMaster1.FullName;

            var invoiceDetails = entity.InvoicingDetails.FirstOrDefault();
            // Take the first and last records from entity.InvoicingDetails based on FromDate
            var firstRecord = entity.InvoicingDetails.OrderBy(x => x.FromDate).FirstOrDefault();
            var lastRecord = entity.InvoicingDetails.OrderByDescending(x => x.FromDate).FirstOrDefault();

            //_returnModel.FromDate = firstRecord?.FromDate?.ToString("dd/MM/yyyy") ?? "";
            //_returnModel.ToDate = lastRecord?.ToDate?.ToString("dd/MM/yyyy") ?? "";

            return _returnModel;
        }
        public static List<InvoiceViewModel> MapInvoiceList(List<Invoicing> entities)
        {
            var _returnModel = new List<InvoiceViewModel>();
            foreach (var item in entities)
            {
                _returnModel.Add(MapInvoice(item));
            }
            return _returnModel;
        }
        #endregion

        #region ------ Payments -----
        public static PaymentVM MapPayment(StudentLedger entity)
        {
            var __returnModel = new PaymentVM();
            __returnModel.Id = entity.Id;
            __returnModel.TransactionCode = entity.Code;
            __returnModel.Location = entity.Location.LocationName;
            __returnModel.MyriadID =  entity.Person.Code;
            __returnModel.FullName =  entity.Person.FullName;
            __returnModel.PaymentDate = entity.PaymentDate;
            __returnModel.Remarks = entity.Remarks;
            __returnModel.CreditAmount = entity.CreditAmount ?? 0;
            __returnModel.PaymentName = entity.PaymentTypeName;
            __returnModel.IsApproved = entity.IsApproved;
            __returnModel.CreatedBy = entity.UserMaster.FullName;
            __returnModel.ApprovedBy = entity.UserMaster1 == null ? "" : entity.UserMaster1.FullName;
            __returnModel.CreditNoteId = entity.CreditNoteId;
            return __returnModel;
        }
        public static List<PaymentVM> MapPaymentList(List<StudentLedger> entities)
        {
            var _returnModel = new List<PaymentVM>();
            foreach (var item in entities)
            {
                _returnModel.Add(MapPayment(item));
            }
            return _returnModel;
        }
        #endregion

    }
}
