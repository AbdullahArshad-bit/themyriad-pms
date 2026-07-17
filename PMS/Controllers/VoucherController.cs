using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.PaymentViewModels;
using PMS.EF;
using PMS.Services.Services.Payment;
using PMS.Services.Services.Setup;
using PMS.Services.Services.VoucherSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace PMS.Controllers
{
    public class VoucherController : Controller
    {
        private readonly IVoucherService voucherService;
        private readonly ISetupService setupService;
        public VoucherController(IVoucherService voucherService, ISetupService setupService)
        {
            this.voucherService = voucherService;
            this.setupService = setupService;
        }
        // GET: Voucher
        public ActionResult Index(DateTime? FromDate, DateTime? ToDate)
        {

            if (FromDate == null || ToDate == null)
            {
                FromDate = new DateTime(2021, 9, 1);
                ToDate = DateTime.Now.Date;
            }
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];
            return View();
        }

        [AuthorizeUser(Roles = AppUserRoles.View_Invoicing_Voucher_Detail)]
        public ActionResult InvoicingVoucher(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Voucher voucher = voucherService.GetById(id);

            if (voucher == null)
            {
                return HttpNotFound();
            }

            ViewBag.VoucherDetail = voucherService.GetVoucherDetail(voucher.VoucherId);
            ViewBag.LocationSetting = setupService.GetLocationSettingsByLocationid(voucher.LocationId);

            return View(voucher);
        }

        [AuthorizeUser(Roles = AppUserRoles.View_Payment_Voucher_Detail)]
        public ActionResult PaymentVoucher(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Voucher voucher = voucherService.GetById(id);

            if (voucher == null)
            {
                return HttpNotFound();
            }

            ViewBag.VoucherDetail = voucherService.GetVoucherDetail(voucher.VoucherId);
            ViewBag.LocationSetting = setupService.GetLocationSettingsByLocationid(voucher.LocationId);

            return View(voucher);
        }

        [AuthorizeUser(Roles = AppUserRoles.View_CreditNote_Voucher_Detail)]
        public ActionResult CreditNoteVoucher(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Voucher voucher = voucherService.GetById(id);

            if (voucher == null)
            {
                return HttpNotFound();
            }

            ViewBag.VoucherDetail = voucherService.GetVoucherDetail(voucher.VoucherId);
            ViewBag.LocationSetting = setupService.GetLocationSettingsByLocationid(voucher.LocationId);

            return View(voucher);
        }
    }
}