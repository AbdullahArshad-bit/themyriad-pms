using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.DTO.ViewModels.PaymentViewModels;
using PMS.EF;
using PMS.Services.Services.AuditLogs;
using PMS.Repository.UnitOfWork;
using PMS.Common.Classes;
using PMS.DTO.ViewModels;
using System.Web;
using PMS.Services.Services.LocationContext;

namespace PMS.Services.Services.PaymentTypes
{
    public class PaymentTypesService : IPaymentTypesService

    {

        private readonly IAuditLogsService auditLogsService;
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly ILocationContextService locationContextService;
        public PaymentTypesService(UnitOfWork<PMSEntities> _uow, IAuditLogsService _auditLogsService, ILocationContextService _locationContextService)
        {
            auditLogsService = _auditLogsService;
            uow = _uow;
            locationContextService = _locationContextService;
        }


        public List<PaymentListVM> GetPayment()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            List<PaymentListVM> result = (from a in uow.GenericRepository<EF.PaymentType>().Table.Where(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId) && x.KeyCode == null)
                                           select new PaymentListVM
                                           {
                                               PaymentId = a.PaymentId,
                                               PaymentName = a.PayementName,
                                               IsActive = a.IsActive,
                                               Code = a.Code,
                                               AccountName = a.ChartOfAccount.Name
                                           }).ToList();
            return result;
        }

        public AddPaymentTypeVM GetPaymentById(int id)
        {
            AddPaymentTypeVM result;

            var db = uow.Context;
            result = (from a in db.PaymentTypes
                      where a.PaymentId == id
                      select new AddPaymentTypeVM
                      {
                          LocationId = a.LocationId??0,
                          PaymentId = a.PaymentId,
                          PaymentName = a.PayementName,
                          CreatedBy = a.CreatedBy,
                          CreatedDate = a.CreatedDate,
                          IsActive = a.IsActive,
                          Code= a.Code,
                          AccountId = a.AccountId??0

                      }).FirstOrDefault();

            return result;
        }


        public bool AddPayment(AddPaymentTypeVM model)
        {
            //insert Service
            EF.PaymentType tbl = new EF.PaymentType
            {
                LocationId = model.LocationId,
                PayementName = model.PaymentName,
                CreatedDate = DateTime.Now,
                CreatedBy = model.CreatedBy,
                IsEnable = true,
                IsActive = model.IsActive,
                Code =model.Code,
                AccountId = model.AccountId
               
            };
            uow.GenericRepository<EF.PaymentType>().Insert(tbl);
            uow.SaveChanges();
            return true;
        }

        public bool UpdatePayment(AddPaymentTypeVM model)
        {
            EF.PaymentType oldtbl = uow.GenericRepository<EF.PaymentType>().GetByIdAsNoTracking(x => x.PaymentId == model.PaymentId);
            EF.PaymentType tbl = uow.GenericRepository<EF.PaymentType>().GetById(model.PaymentId);

            if (tbl != null)
            {
                tbl.LocationId = model.LocationId;
                tbl.PayementName = model.PaymentName;
                tbl.Code = model.Code;
                tbl.AccountId = model.AccountId;
                tbl.IsActive = model.IsActive;
                tbl.UpdatedDate = DateTime.Now;
                tbl.UpdateBy = model.UpdatedBy;
            }

            uow.SaveChanges();

            //Insert Audit Log
            {
                var difference = Common.Classes.Common.DetailedCompare<EF.PaymentType>(oldtbl, tbl);
                List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Update,
                    ActionId = (int)Enumeration.CorrespondenceAction.UpdatePaymentMethod,
                    PK = tbl.PaymentId.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "PaymentType",
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    AuditLogDetails = difference
                };
                auditLogsService.AddAuditLog(auditLog);
            }


            return true;
        }

        public bool DeletePayment(int id)
        {
            EF.PaymentType oldtbl = uow.GenericRepository<EF.PaymentType>().GetByIdAsNoTracking(x => x.PaymentId == id);
            EF.PaymentType tbl = uow.GenericRepository<EF.PaymentType>().GetById(id);

            if (tbl != null)
            {
                tbl.IsEnable = false;

                uow.GenericRepository<EF.PaymentType>().Update(tbl);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.PaymentType>(oldtbl, tbl);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeletePaymentMethod,
                        PK = tbl.PaymentId.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "PaymentType",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return true;
            }
            else
            {
                throw new Exception("Payment not found to delete.");
            }
        }
        public List<PaymentListVM> GetKeyCodePayment()
        {
            List<PaymentListVM> result = (from a in uow.GenericRepository<EF.PaymentType>().Table.Where(x => x.IsEnable == true)
                                          select new PaymentListVM
                                          {
                                              PaymentId = a.PaymentId,
                                              PaymentName = a.PayementName,
                                              IsActive = a.IsActive,
                                              Code = a.Code
                                          }).ToList();
            return result;
        }
        public OutputInvoicingVM GetInvoiceCode(int? id)
        {
            var res = uow.GenericRepository<EF.StudentLedger>().Table.Where(x => x.Id == id).Select(x => x.InvoiceId).FirstOrDefault();
            var res1 = uow.GenericRepository<EF.Invoicing>().Table.Where(x => x.Id == res).Select(x => new OutputInvoicingVM
            {
                Code=x.Code,
                Id=x.Id
            }).FirstOrDefault();
            return res1;
        }

    }
}
