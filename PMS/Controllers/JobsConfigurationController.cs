using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels;
using PMS.Services.Services.JobConfuguration;
using PMS.Services.Services.Ticket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Controllers
{
    
    public class JobsConfigurationController : Controller
    {
        ITicketService ticketService;
        IJobConfigurationService jobConfigurationService;

        // GET: JobsConfiguration
        public JobsConfigurationController(ITicketService _ticketService,IJobConfigurationService _jobConfigurationService)
        {
            ticketService = _ticketService;
            jobConfigurationService = _jobConfigurationService;
        }
        public ActionResult Index()
        {
            return View();
        }
        [AuthorizeUser(Roles = AppUserRoles.add_JobConfiguration)]
        public ActionResult Add()
        {
            return View();
        }
        public JsonResult GetbyId(int CategoryId)
        {
            var response = jobConfigurationService.GetById(CategoryId);
            return Json(new { data = response }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult Add(List<JobConfigurationVM> jobList)
        {
            var response = jobConfigurationService.Add(jobList);
            return Json(new { data = response }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetActiveStatus()
        {
            var response = ticketService.GetActiveStatus();
            return Json(new { data = response }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetActiveJobAction()
        {
            var response = jobConfigurationService.GetActiveJobAction();
            return Json(new { data = response }, JsonRequestBehavior.AllowGet);
        }
    }
}