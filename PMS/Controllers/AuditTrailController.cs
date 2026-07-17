using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using PMS.Classes;
using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels.AuditLogsViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.AuditLogs;


namespace PMS.Controllers
{
    [AuthorizeUser]
    public class AuditTrailController : Controller
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IAuditLogsService auditLogsService;
        public AuditTrailController(UnitOfWork<PMSEntities> _uow, IAuditLogsService _auditLogsService)
        {
            uow = _uow;
            auditLogsService = _auditLogsService;
        }


        [AuthorizeUser(Roles = AppUserRoles.view_AuditTrialList)]
        public ActionResult Index(DateTime? FromDate, DateTime? ToDate, int? Userid, int? ActionId, int? AuditTypeId)
        {
            if (FromDate == null || ToDate == null)
            {
                var today = DateTime.Now.Date;

                ToDate = today;
                FromDate = new DateTime(today.Year - 1, 9, 1);
            }

            ViewBag.FromDate = FromDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.ToDate = ToDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.UserId = new SelectList(uow.GenericRepository<UserMaster>().GetAll(), "ID", "FullName", Userid);
            ViewBag.ActionId = new SelectList(uow.GenericRepository<CorrespondenceAction>().GetAll(), "ID", "ActionName", ActionId);

            return View();
        }

        public JsonResult LoadAuditLogs(AuditLogsBinding request)
        {
            try
            {
                var response = auditLogsService.GetAuditLogsData(request);
                return Json(new
                {
                    draw = request.draw,
                    recordsTotal = response.TotalRecords,
                    recordsFiltered = response.RecordsFiltered,
                    data = response.AuditLogs
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = request.draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<AuditLogsViewModels>(),
                    error = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [AuthorizeUser(Roles = AppUserRoles.view_AuditTrialList)]
        public void ExportAuditLogs(DateTime? FromDate, DateTime? ToDate, int? Userid, int? ActionId, int? AuditTypeId, string searchValue = null)
        {
            var request = new AuditLogsBinding
            {
                FromDate = FromDate,
                ToDate = ToDate,
                Userid = Userid,
                ActionId = ActionId,
                AuditTypeId = AuditTypeId,
                search = new Search { value = searchValue }
            };

            var response = auditLogsService.ExportAuditLogsData(request);
            var data = response.AuditLogs.Select(x => new
            {
                MyriadID = x.PersonCode,
                FullName = x.PersonFullName,
                AuditAction = x.ActionName,
                AuditType = x.AuditTypeText,
                Reference = x.Reference,
                TableName = x.TableName,
                TimeStamp = x.FormattedTimeStamp,
                PerformedBy = x.UserName
            });

            ExcelHelper.ExportToExcel(Response, data, "Audit Trail - PMS");
        }

        public ActionResult GetAuditLogsDetailByid(int id)
        {
            var auditaction = uow.GenericRepository<EF.AuditLog>().Table.Where(x => x.Id == id).FirstOrDefault();
            //ViewBag.AuditDetails = uow.GenericRepository<EF.AuditLogDetail>().Table.Where(x => x.AuditLogId == id).ToList();
            ViewBag.AuditDetails = auditaction.AuditLogDetails.ToList();
            ViewBag.RouteControl = auditaction.CorrespondenceAction;
            ViewBag.PrimaryKey = auditaction.PK;
            return PartialView("~/Views/AuditTrail/_ModifiedFieldsList.cshtml");
        }
    }
}