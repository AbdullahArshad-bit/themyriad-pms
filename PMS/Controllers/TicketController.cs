using Newtonsoft.Json;
using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO;
using PMS.DTO.ViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.Person;
using PMS.Services.Services.Setup;
using PMS.Services.Services.Ticket;
using PMS.Services.Services.UserManage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace PMS.Controllers
{
    [AuthorizeUser]
    public class TicketController : Controller
    {
        private readonly UnitOfWork<PMSEntities> uow;

        IPersonService personService;
        ISetupService setupService;
        IUserManageService userService;
        ITicketService ticketService;
        public TicketController(UnitOfWork<PMSEntities> _uow, IPersonService _personService, ISetupService _setupService, IUserManageService _userService, ITicketService _ticketService)
        {
            uow = _uow;
            personService = _personService;
            setupService = _setupService;
            userService = _userService;
            ticketService = _ticketService;
        }
        // GET: Ticket
        [AuthorizeUser(Roles = AppUserRoles.view_ticketList)]
        public ActionResult Index(int? statusId=0, int? ticketId=0)
        {
            ViewBag.StatusId = new SelectList(ticketService.GetActiveStatus(), "Id", "Name");
            ViewBag.Status = new SelectList(ticketService.GetActiveStatus(), "Id", "Name", statusId);
            ViewBag.TicketId = ticketId;
            ViewBag.PeriorityId = new SelectList(ticketService.GetActivePeriority(), "Id", "Name");
            ViewBag.AssignTo = new SelectList(userService.GetActiveUsers().Where(x => x.IsStudent != true).ToList(), "ID", "FullName");
            ViewBag.success = TempData["success"];
            ViewBag.error = TempData["error"];
            var result = ticketService.GetTickets(statusId, ticketId);
            return View(result);
        }
        [AuthorizeUser(Roles = AppUserRoles.add_Ticket)]
        public ActionResult Add(int? id = 0)
        {
            var model = new TicketViewModel();
            if (id == 0)
            {

                ViewBag.StatusId = new SelectList(ticketService.GetActiveStatus(), "Id", "Name");
                ViewBag.PeriorityId = new SelectList(ticketService.GetActivePeriority(), "Id", "Name");
                //ViewBag.AssignTo = new SelectList("");
                ViewBag.AssignTo = new SelectList(userService.GetActiveUsers().Where(x => x.IsStudent != true).ToList(), "ID", "FullName");
                ViewBag.IssueBy = new SelectList("");
                ViewBag.IssueByStaff = new SelectList("");
                ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
                ViewBag.GroupId = new SelectList(ticketService.GetActiveGroup(), "Id", "Name");
            }
            else
            {

                var ticket = ticketService.GetById(id ?? 0);
                if (ticket == null)
                {
                    return HttpNotFound();
                }
                ViewBag.StatusId = new SelectList(ticketService.GetActiveStatus(), "Id", "Name", ticket.Data.StatusId);
                ViewBag.PeriorityId = new SelectList(ticketService.GetActivePeriority(), "Id", "Name", ticket.Data.PeriorityId);
                ViewBag.AssignTo = new SelectList(userService.GetActiveUsers().Where(x => x.IsStudent != true).ToList(), "ID", "FullName", ticket.Data.AssignTo);
                ViewBag.IssueBy = new SelectList(personService.GetPersonsReservedCurrently()
                    .Where(x => x.LocationId == ticket.Data.LocationId).
                    Select(x => new { x.PersonID, FullName = x.Code + " - " + x.FullName }).ToList()
                    , "PersonId", "FullName", ticket.Data.IssueBy);
                ViewBag.IssueByStaff = new SelectList(userService.GetActiveUsers().Where(x => x.IsStudent != true).ToList(), "ID", "FullName",ticket.Data.IssueByStaff);
                ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", ticket.Data.LocationId);
                ViewBag.GroupId = new SelectList(ticketService.GetActiveGroup(), "Id", "Name",ticket.Data.GroupId);
                model = ticket.Data;
            }
            return View(model);
        }
        public JsonResult GetResidanceByLocation(int LocationId)
        {
            var persons = personService.GetPersonsReservedCurrently().Where(x => x.LocationId == LocationId).Select(x => new { x.PersonID, FullName = x.Code + " - " + x.FullName }).ToList();
            var code = ticketService.GetMaxCode(LocationId);
            var users = userService.GetActiveUsers().Where(x => x.IsStudent != true).Select(x => new { x.ID,x.FullName });
            return Json(new { Success = true, Persons = persons, Code = code,Users= users } ,JsonRequestBehavior.AllowGet);
        }

        [AuthorizeUser(Roles = AppUserRoles.add_Ticket)]
        [HttpPost]
        public ActionResult Add()
        {
            var model = JsonConvert.DeserializeObject<TicketViewModel>(Request.Form["detail"]);
            var response = new ApiResponse<object>();
            if (model.Id == 0)
            {
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
        [AuthorizeUser(Roles = AppUserRoles.update_TicketComments)]
        public ActionResult Comments(int Id)
        {
            var model = new List<CommentViewModel>();
            var response = ticketService.GetAllComments(Id);
            model = response.Data;
            return View(model);
        }
        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.update_TicketComments)]
        public ActionResult Comments(CommentViewModel model)
        {
            var response = ticketService.AddComment(model);
            return Json(new { success = response.Success, data = response.Data });
        }
        public JsonResult GetTicketDetail(int Id)
        {
            var response = ticketService.GetDetailById(Id);
            return Json(new { success = response.Success, data = response.Data, message = response.Message }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.update_TicketStatus)]
        public ActionResult UpdateStatus(TicketViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Id > 0)
                {
                    ticketService.UpdateStatus(model);
                    TempData["success"] = "Ticket Status updated successfully.";
                }
                else
                {
                    TempData["error"] = "Something went wrong. Ticket not saved.";
                }
            }

            return RedirectToAction("Index");
        }
        [AuthorizeUser(Roles = AppUserRoles.delete_Ticket)]
        public ActionResult Delete(int id)
        {
            var response = ticketService.Delete(id);
            if (response.Success)
                TempData["success"] = response.Message;
            else
                TempData["error"] = response.Message;

            return RedirectToAction("Index");
        }
        public JsonResult GetStaffByGroup(int GroupId=0)
        {
            var response = ticketService.GetUserByGroupId(GroupId);
            return Json(new { Success = true, data = response }, JsonRequestBehavior.AllowGet);
        }

    }
}