using System;
using System.Collections.Generic;
using System.Linq;
using PMS.DTO.ViewModels.COAViewModels;
using PMS.EF;
using PMS.Services.Services.AuditLogs;
using PMS.Repository.UnitOfWork;
using PMS.Common.Classes;
using System.Web.Script.Serialization;
using System.Web;
using static PMS.Common.Classes.Enumeration;
using PMS.Services.Services.LocationContext;

namespace PMS.Services.Services.ChartOfAccounts
{
    public class ChartOfAccountsService : IChartOfAccountsService
    {
        private readonly IAuditLogsService auditLogsService;
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly ILocationContextService locationContextService;
        public ChartOfAccountsService(UnitOfWork<PMSEntities> _uow, IAuditLogsService _auditLogsService, ILocationContextService _locationContextService)
        {
            auditLogsService = _auditLogsService;
            uow = _uow;
            locationContextService = _locationContextService;
        }

        public List<COAVM> GetChartOfAccounts()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var db = uow.Context;

            return uow.GenericRepository<ChartOfAccount>().Table
                .Where(a => a.LocationId.HasValue && assignedLocationIds.Contains((int)a.LocationId))
                .Select(a => new COAVM
                {
                    Id = a.Id,
                    Name = a.Name,
                    Alias = a.Alias,
                    CreatedBy = a.CreatedBy,
                    Status = a.Status,
                    Code = a.Code,
                    AccountTypeId = a.AccountTypeId,

                    LocationName = a.Location.LocationName,
                    AccountType = a.AccountType.TypeName
                })
                .ToList();
        }


        public AddCOAVM GetCOAById(int id)
        {
            AddCOAVM result;

            var db = uow.Context;
            result = (from a in db.ChartOfAccounts
                      where a.Id == id
                      select new AddCOAVM
                      {
                          Id = a.Id,
                          Name = a.Name,
                          Alias = a.Alias,
                          CreatedBy = a.CreatedBy,
                          Status = a.Status,
                          Code = a.Code,
                          AccountTypeId = a.AccountTypeId,
                          LocationId = a.LocationId


                      }).FirstOrDefault();

            return result;
        }


        public bool AddCOA(AddCOAVM model)
        {
            //insert Service
            EF.ChartOfAccount tbl = new EF.ChartOfAccount
            {
                Name = model.Name,
                Alias = model.Alias,
                CreatedBy = Common.Globals.User.ID,
                CreatedDate = DateTime.Now,
                Status = model.Status,
                Code = model.Code,
                AccountTypeId = model.AccountTypeId,
                LocationId = model.LocationId
            };
            uow.GenericRepository<EF.ChartOfAccount>().Insert(tbl);
            uow.SaveChanges();
            return true;
        }

        public bool UpdateCOA(AddCOAVM model)
        {
            EF.ChartOfAccount oldtbl = uow.GenericRepository<EF.ChartOfAccount>().GetByIdAsNoTracking(x => x.Id == model.Id);
            EF.ChartOfAccount tbl = uow.GenericRepository<EF.ChartOfAccount>().GetById(model.Id);

            if (tbl != null)
            {
                tbl.Name = model.Name;
                tbl.Alias = model.Alias;
                tbl.Status = model.Status;
                tbl.Code = model.Code;
                tbl.AccountTypeId = model.AccountTypeId;
                tbl.UpdatedDate = DateTime.Now;
                tbl.updatedby = Common.Globals.User.ID;
                tbl.LocationId = model.LocationId;

            }
            uow.SaveChanges();


            //Insert Audit Log
            {
                var difference = Common.Classes.Common.DetailedCompare<EF.ChartOfAccount>(oldtbl, tbl);
                List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Update,
                    ActionId = (int)Enumeration.CorrespondenceAction.UpdateChartOfAccounts,
                    PK = tbl.Id.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "ChartOfAccount",
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    AuditLogDetails = difference
                };
                auditLogsService.AddAuditLog(auditLog);
            }


            return true;
        }

        public bool DeleteAccount(int id)
        {
            var oldtbl = uow.GenericRepository<EF.ChartOfAccount>().Table.Select(x => new { x.Id, x.Name, x.Alias, x.CreatedBy, x.Status, x.Code, x.CreatedDate, x.AccountTypeId }).Where(x => x.Id == id).FirstOrDefault();
            EF.ChartOfAccount tbl = uow.GenericRepository<EF.ChartOfAccount>().GetById(id);
            var oldobj = new JavaScriptSerializer().Serialize(oldtbl);
            if (tbl != null)
            {
                uow.GenericRepository<EF.ChartOfAccount>().Delete(tbl);
                uow.SaveChanges();

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.ChartOfAccount>(tbl, tbl);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {

                        OldValue = oldobj,
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteChartOfAccounts,
                        PK = tbl.Id.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "ChartOfAccount",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return true;
            }
            else
            {
                throw new Exception("Account not found to delete.");
            }
        }

        public List<COAVM> GetAccountsByServiceType(int serviceTypeId)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            List<int> allowedAccountTypes = new List<int>();

            switch (serviceTypeId)
            {
                case 1:
                case 3:
                case 4:
                    allowedAccountTypes.Add((int)AccountTypes.Income);
                    break;
                case 2:
                    allowedAccountTypes.Add((int)AccountTypes.OtherLiability);
                    allowedAccountTypes.Add((int)AccountTypes.Liabilities);
                    break;
                default:
                    return GetChartOfAccounts();
            }

            return uow.GenericRepository<ChartOfAccount>().Table
                .Where(a => a.LocationId.HasValue && assignedLocationIds.Contains((int)a.LocationId) && a.Status == true &&
                           allowedAccountTypes.Contains(a.AccountTypeId))
                .Select(a => new COAVM
                {
                    Id = a.Id,
                    Name = a.Name,
                    Status = a.Status
                })
                .ToList();
        }

        public List<COAVM> GetAssetAccounts()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();
            return uow.GenericRepository<ChartOfAccount>().Table
                .Where(a => a.LocationId.HasValue && assignedLocationIds.Contains((int)a.LocationId) && a.Status == true &&
                       a.AccountTypeId == (int)AccountTypes.Assets)
                .Select(a => new COAVM
                {
                    Id = a.Id,
                    Name = a.Name,
                    Status = a.Status
                })
                .ToList();
        }

        public List<COAVM> GetReceivableAccounts()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();


            var allowedAccountTypes = new List<int> {
        (int)AccountTypes.OtherAssets,
        (int)AccountTypes.Assets
    };

            return uow.GenericRepository<ChartOfAccount>().Table
                .Where(a => a.LocationId.HasValue && assignedLocationIds.Contains((int)a.LocationId) && a.Status == true &&
                       allowedAccountTypes.Contains(a.AccountTypeId))
                .Select(a => new COAVM
                {
                    Id = a.Id,
                    Name = a.Name,
                    Status = a.Status
                })
                .ToList();
        }

        public List<COAVM> GetPayableAccounts()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var allowedAccountTypes = new List<int> {
        (int)AccountTypes.OtherLiability,
        (int)AccountTypes.Liabilities
    };

            return uow.GenericRepository<ChartOfAccount>().Table
                .Where(a => a.LocationId.HasValue && assignedLocationIds.Contains((int)a.LocationId) && a.Status == true &&
                       allowedAccountTypes.Contains(a.AccountTypeId))
                .Select(a => new COAVM
                {
                    Id = a.Id,
                    Name = a.Name,
                    Status = a.Status
                })
                .ToList();
        }
        public List<COAVM> GetLiablitiesAccounts()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var allowedAccountTypes = new List<int> {
        (int)AccountTypes.Liabilities
    };

            return uow.GenericRepository<ChartOfAccount>().Table
                .Where(a => a.LocationId.HasValue && assignedLocationIds.Contains((int)a.LocationId) && a.Status == true &&
                       allowedAccountTypes.Contains(a.AccountTypeId))
                .Select(a => new COAVM
                {
                    Id = a.Id,
                    Name = a.Name,
                    Status = a.Status
                })
                .ToList();
        }

        public List<COAVM> GetDiscountAccounts()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var allowedAccountTypes = new List<int> {
        (int)AccountTypes.Expense
    };

            return uow.GenericRepository<ChartOfAccount>().Table
                .Where(a => a.LocationId.HasValue && assignedLocationIds.Contains((int)a.LocationId) && a.Status == true &&
                       allowedAccountTypes.Contains(a.AccountTypeId))
                .Select(a => new COAVM
                {
                    Id = a.Id,
                    Name = a.Name,
                    Status = a.Status
                })
                .ToList();
        }
    }
}




