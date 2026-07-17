using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using PMS.Common.Classes;
using PMS.DTO.ViewModels.TaxViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.AuditLogs;
using PMS.Services.Services.LocationContext;
using PMS.Services.Services.Tax;

namespace PMS.Services.Services.Tex
{
    public class TexService : ITaxService
    {
        private readonly IAuditLogsService auditLogsService;
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly ILocationContextService locationContextService;
        public TexService(UnitOfWork<PMSEntities> _uow, IAuditLogsService _auditLogsService, ILocationContextService _locationContextService)
        {
            auditLogsService = _auditLogsService;
            uow = _uow;
            locationContextService = _locationContextService;
        }

        public List<TaxVM> GetTax()
        {
            var db = uow.Context;
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            List<TaxVM> result = (from a in uow.GenericRepository<EF.Tax>().Table.Where(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId))
                                  join p in db.ChartOfAccounts
                                          on a.AccountId equals p.Id
                                          into per
                                  from ChartOfAccounts in per.DefaultIfEmpty()

                                  select new TaxVM
                                  {
                                      TaxId = a.TaxId,
                                      TaxName = a.TaxName,
                                      TaxPercentage = a.TaxPercentage,
                                      IsActive = a.IsActive,
                                      Code = a.Code,
                                      Account = a.ChartOfAccount.Name,
                                      LocationName = a.Location.LocationName

                                  }).ToList();
            return result;
        }

        public AddTaxVM GetTaxById(int id)
        {
            AddTaxVM result;

            var db = uow.Context;
            result = (from a in db.Taxes
                      where a.TaxId == id
                      select new AddTaxVM
                      {
                          TaxId = a.TaxId,
                          TaxName = a.TaxName,
                          TaxPercentage = a.TaxPercentage,
                          CreatedBy = a.CreatedBy,
                          CreatedDate = a.CreatedDate,
                          IsActive = a.IsActive,
                          Code = a.Code,
                          AccountId = a.AccountId,
                          LocationId = a.LocationId

                      }).FirstOrDefault();

            return result;
        }


        public bool AddTax(AddTaxVM model)
        {
            //EF.Tax tbl = new EF.Tax
            //{
            //    TaxName = model.TaxName,
            //    TaxPercentage = model.TaxPercentage,
            //    CreatedDate = DateTime.Now,
            //    CreatedBy = model.CreatedBy,
            //    IsEnable = true,
            //    IsActive = model.IsActive,
            //    Code = model.Code,
            //    AccountId = model.AccountId
            //};
            var configuration = new MapperConfiguration(cfg =>
            {

                cfg.CreateMap<AddTaxVM, EF.Tax>().BeforeMap((s, d) => d.IsEnable = true); ;
            });
            var mapper = new Mapper(configuration);
            var dest = mapper.Map<AddTaxVM, EF.Tax>(model);
            uow.GenericRepository<EF.Tax>().Insert(dest);
            uow.SaveChanges();
            return true;
        }

        public bool UpdateTax(AddTaxVM model)
        {
            EF.Tax oldtbl = uow.GenericRepository<EF.Tax>().GetByIdAsNoTracking(x => x.TaxId == model.TaxId);
            EF.Tax tbl = uow.GenericRepository<EF.Tax>().GetById(model.TaxId);

            if (tbl != null)
            {
                tbl.TaxName = model.TaxName;
                tbl.TaxPercentage = model.TaxPercentage;
                tbl.IsActive = model.IsActive;
                tbl.UpdatedDate = DateTime.Now;
                tbl.UpdatedBy = model.UpdatedBy;
                tbl.Code = model.Code;
                tbl.AccountId = model.AccountId;
                tbl.LocationId = model.LocationId;
            }

            uow.SaveChanges();

            //Insert Audit Log
            {
                var difference = Common.Classes.Common.DetailedCompare<EF.Tax>(oldtbl, tbl);
                List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Update,
                    ActionId = (int)Enumeration.CorrespondenceAction.UpdateTax,
                    PK = tbl.TaxId.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "Tax",
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    AuditLogDetails = difference
                };
                auditLogsService.AddAuditLog(auditLog);
            }



            return true;
        }

        public bool DeleteTax(int id)
        {
            EF.Tax oldtbl = uow.GenericRepository<EF.Tax>().GetByIdAsNoTracking(x => x.TaxId == id);
            EF.Tax tbl = uow.GenericRepository<EF.Tax>().GetById(id);

            if (tbl != null)
            {
                tbl.IsEnable = false;
                tbl.IsActive = false;
                uow.GenericRepository<EF.Tax>().Update(tbl);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Tax>(oldtbl, tbl);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteTax,
                        PK = tbl.TaxId.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Tax",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }

                return true;
            }
            else
            {
                throw new Exception("Tax not found to delete.");
            }
        }

        public List<EF.Tax> GetAll()
        {
            var db = uow.Context;
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            return uow.GenericRepository<EF.Tax>().Table.Where(x => x.IsActive == true && x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId)).ToList();
        }

    }
}
