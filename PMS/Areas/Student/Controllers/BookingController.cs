using PMS.Areas.Student.Classes;
using PMS.Common.Filters;
using PMS.Services.Services.Booking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Areas.Student.Controllers
{
    [AuthorizeUser]
    [AllowUserFilter]
   
    public class BookingController : Controller
    {
       private readonly IBookingService bookingService;
        public BookingController(IBookingService _bookingService)
        {
            bookingService = _bookingService;
        }
        // GET: Student/Booking
        
        public ActionResult BookingList()
        {
            ViewBag.Bookings = bookingService.GetBookings(PMS.Common.Globals.User.PersonId);
            return View();
        }
    }
}