using PMS.Services.Services.Booking;
using PMS.StudentApi.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PMS.StudentApi.Controllers.Api
{
    [ApiAuthorize]
    public class BookingController : ApiController
    {
        private readonly IBookingService bookingService;
        public BookingController(IBookingService _bookingService)
        {
            bookingService = _bookingService;
        }
        [HttpGet]
        public HttpResponseMessage GetAll(int id)
        {
            var booking = bookingService.GetBooking(id);
            return Request.CreateResponse((HttpStatusCode)booking.Code, booking);
        }
    }
}
