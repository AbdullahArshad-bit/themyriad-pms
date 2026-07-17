using PMS.DTO.ViewModels;
using PMS.Services.Services.Ticket;
using PMS.Services.Services.TicketGroup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Controllers
{
    public class TicketGroupController : Controller
    {
        private readonly ITicketGroupService ticketGroupService;
        private readonly ITicketService ticketService; 
        public TicketGroupController(ITicketGroupService _ticketGroupService,ITicketService _ticketService)
        {
            ticketGroupService = _ticketGroupService;
            ticketService = _ticketService;
        }
        // GET: TicketGroup
        public ActionResult Add(int? GroupId)
        {
            var model = new List<TicketGroupVm>();
            if (GroupId != null)
            {
                 model = ticketGroupService.GetAll(GroupId);
                
            }
            ViewBag.GroupId = new SelectList(ticketService.GetActiveGroup(), "Id", "Name");
            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];
            return View(model);
        }
        [HttpPost]
        public ActionResult Add(List<TicketGroupVm> model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (ticketGroupService.SaveTicketGroup(model))
                    {
                        TempData["success"] = "saved successfully.";
                    }
                    else
                    {
                        TempData["error"] = "Error : Unable to save at this moment.";
                    }
                }
                else
                {
                    TempData["error"] = "Error : Model error.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }
    }
}