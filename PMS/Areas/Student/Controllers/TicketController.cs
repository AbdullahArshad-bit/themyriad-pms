using Newtonsoft.Json;
using PMS.Areas.Student.Classes;
using PMS.Common.Filters;
using PMS.DTO;
using PMS.DTO.ViewModels;
using PMS.Services.Services.Ticket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Areas.Student.Controllers
{
    [AuthorizeUser]
    [AllowUserFilter]
    public class TicketController : Controller
    {
        private readonly ITicketService ticketService;
        public TicketController(ITicketService _ticketService)
        {
            ticketService = _ticketService;
        }
        // GET: Student/Ticket
        public ActionResult Index()
        {
           
            var result = ticketService.GetStudentTickets(PMS.Common.Globals.User.ID);
            ViewBag.success = TempData["success"];
            ViewBag.error = TempData["error"];
            return View(result);
        }
        public ActionResult Add(int? id = 0)
        {
            var model = new TicketViewModel();
            if (id != 0)
            {
                var ticket = ticketService.GetById(id ?? 0);
                if (ticket == null)
                {
                    return HttpNotFound();
                }
                model = ticket.Data;
                model.GroupId = ticket.Data.GroupId;
            }
            ViewBag.GroupId = new SelectList(ticketService.GetActiveGroup(), "Id", "Name", model.GroupId);
            return View(model);
        }
        [HttpPost]
        public ActionResult Add()
        {
            var model = JsonConvert.DeserializeObject<TicketViewModel>(Request.Form["detail"]);
            var response = new ApiResponse<object>();
            if (model.Id == 0)
            {
                model.IssueBy = PMS.Common.Globals.User.PersonId;
                model.CreatedBy = PMS.Common.Globals.User.ID;
                model.CreatedDate = DateTime.Now;
                response = ticketService.addTicket(model, Request.Files);
            }
            else
            {
                var existingFiles = JsonConvert.DeserializeObject<List<MockFiles>>(Request.Form["ExistingFiles"]);
                model.CreatedBy = PMS.Common.Globals.User.ID;
                model.CreatedDate = DateTime.Now;
                response = ticketService.Update(model, Request.Files, existingFiles);
            }
            if (response.Success)
                TempData["success"] = response.Message;
            else
                TempData["error"] = response.Message;

            return Json(new { success = response.Success, data = response.Data, message = response.Message }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult AddDetail()
        {
            var detail = new TicketDetailViewModel
            {
                TicketId = Convert.ToInt32(Request.Form["id"].ToString()),
                Description = Request.Form["description"]
            };
            var files = Request.Form["files"];
            var response = ticketService.AddTicketDetail(Request.Files, detail);
            return Json(new { success = response.Success, data = response.Data, message = response.Message }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetAttachementById(int id)
        {
            var response = ticketService.GetAttachement(id);
            return Json(new { success = response.Success, data = response.Data, message = response.Message }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Comments(int Id)
        {
           var model= new List<CommentViewModel>();
            var response= ticketService.GetAllComments(Id);
            model = response.Data;
            return View(model);
        }
        [HttpPost]
        public ActionResult Comments(CommentViewModel model)
        {
           var response= ticketService.AddComment(model);
           return Json(new { success = response.Success, data = response.Data });
        }

        public ActionResult Delete(int id)
        {


            var response=ticketService.Delete(id);
            if (response.Success)
                TempData["success"] = response.Message;
            else
                TempData["error"] = response.Message;

            return RedirectToAction("Index");
        }

    }
}