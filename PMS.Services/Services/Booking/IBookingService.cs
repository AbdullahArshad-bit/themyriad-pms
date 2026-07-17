using PMS.DTO;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using PMS.DTO.ViewModels.BookingViewModels;
using PMS.EF;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.Booking
{
    public interface IBookingService
    {

        List<BookingListVM> GetBookings(int personId = 0);
        BookingsResponse GetPMSBookings(BookingsBinding request);
        BookingsResponse GetPMSExportBookings(string QueryBy, string query = null, DateTime? FromDate = null, DateTime? ToDate = null);


        List<SelectListVM> GetPriceConfigurations();
        List<SelectListVM> GetWebsitePriceConfigurations();
        CommitmentDetailVM GetPriceConfigDetailByID(int id);
        GuestCountVm GetPlacementDetailID(int id);
        bool CheckOutGuest(GuestDetailVm headCountVM);
        bool SavePersonGuest(GuestCountVm vm);
        EF.Booking GetBookingByID(int id);
        EF.Booking AddBooking(AddBookingVM bookingVM);
        EF.Booking AddImportBooking(AddBookingVM bookingVM);
        EF.Booking UpdateBooking(AddBookingVM bookingVM);
        //HeeadCountVM UpdateHeadCount(HeeadCountVM heeadCountVM);
        bool DeleteBooking(int id);
        bool CancelBooking(int id);
        //api IServices
        ApiResponse<List<BookingListVM>> GetBooking(int Id);
        UploadReceiptVM GetBookingData(int personid, int bookingid);
        bool AddReceipt(UploadReceiptVM uploadReceiptVM);
        bool Checkupload(int Personid, int bookingid);
        UploadReceiptVM GetReceipt(int bookingid);

        IQueryable<EF.Booking> GetBookingQueryable();

    }
}
