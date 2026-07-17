using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Common.Classes;
using PMS.DTO.ViewModels.ServiceViewModels;
using PMS.EF;
using PMS.Services.Services.AuditLogs;
using PMS.Repository.UnitOfWork;
using System.Web;
using PMS.Services.Services.LocationContext;

namespace PMS.Services.Services.Service
{
    public class ServicesService : IServicesService
    {
        private readonly IAuditLogsService auditLogsService;
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly ILocationContextService locationContextService;
        public ServicesService(UnitOfWork<PMSEntities> _uow, IAuditLogsService _auditLogsService, ILocationContextService _locationContextService)
        {
            auditLogsService = _auditLogsService;
            uow = _uow;
            locationContextService = _locationContextService;
        }


        public List<ServicesListVM> GetServices()
        {
            var db = uow.Context;
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            List<ServicesListVM> result = (from a in uow.GenericRepository<EF.Service>().Table.Where(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId))
                                           join p in db.ChartOfAccounts
                                           on a.AccountId equals p.Id
                                           into per
                                           from ChartOfAccounts in per.DefaultIfEmpty()

                                           select new ServicesListVM
                                           {
                                             ServiceId = a.ServiceId,
                                             ServiceName = a.ServiceName,
                                             ServiceAmount = a.ServiceAmount,
                                             IsActive = a.IsActive,
                                             Account = a.ChartOfAccount.Name,
                                             ServiceTypeId=a.TypeId,
                                             IsPrePlacementService = a.IsPrePlacementService,
                                             TaxId = a.TaxId,
                                             LocationName = a.Location.LocationName,
                                             LocationId = a.LocationId


                                           }).ToList();
            return result;
        }

        public AddServiceVM GetServicesById(int id)
        {

            AddServiceVM result;

            var db = uow.Context;
            result = (from a in db.Services
                      where a.ServiceId == id
                      select new AddServiceVM
                      {
                          serviceId = a.ServiceId,
                          ServiceName = a.ServiceName,
                          ServiceAmount = a.ServiceAmount,
                          CreatedBy = a.CreatedBy,
                          CreatedDate = a.CreatedDate,
                          IsActive = a.IsActive,
                          AccountId = a.AccountId,
                          IsPrePlacementService = a.IsPrePlacementService,
                          ServiceTypeId=a.TypeId,
                          TaxId = a.TaxId,
                          LocationId = a.LocationId

                      }).FirstOrDefault();

            return result;
        }

        public Dictionary<int, AddServiceVM> GetServicesByIds(IEnumerable<int> ids)
        {
            var idList = ids?.Where(id => id > 0).Distinct().ToList();
            if (idList == null || !idList.Any())
            {
                return new Dictionary<int, AddServiceVM>();
            }

            try
            {
                // Use a separate context for read-only operations to avoid lock contention
                // This is critical when called from within a transaction
                using (var readOnlyContext = new PMSEntities())
                {
                    readOnlyContext.Configuration.AutoDetectChangesEnabled = false;
                    readOnlyContext.Configuration.ValidateOnSaveEnabled = false;
                    
                    // Use AsNoTracking for read-only query to improve performance and avoid tracking overhead
                    var services = readOnlyContext.Services
                        .AsNoTracking()
                        .Where(a => idList.Contains(a.ServiceId))
                        .Select(a => new AddServiceVM
                        {
                            serviceId = a.ServiceId,
                            ServiceName = a.ServiceName,
                            ServiceAmount = a.ServiceAmount,
                            CreatedBy = a.CreatedBy,
                            CreatedDate = a.CreatedDate,
                            IsActive = a.IsActive,
                            AccountId = a.AccountId,
                            IsPrePlacementService = a.IsPrePlacementService,
                            ServiceTypeId = a.TypeId,
                            TaxId = a.TaxId,
                            LocationId = a.LocationId
                        })
                        .ToList();

                    return services.ToDictionary(s => s.serviceId, s => s);
                }
            }
            catch (Exception ex)
            {
                // Fallback: try using the unit of work context (might be slower if in transaction)
                try
                {
                    var db = uow.Context;
                    var services = db.Services
                        .AsNoTracking()
                        .Where(a => idList.Contains(a.ServiceId))
                        .Select(a => new AddServiceVM
                        {
                            serviceId = a.ServiceId,
                            ServiceName = a.ServiceName,
                            ServiceAmount = a.ServiceAmount,
                            CreatedBy = a.CreatedBy,
                            CreatedDate = a.CreatedDate,
                            IsActive = a.IsActive,
                            AccountId = a.AccountId,
                            IsPrePlacementService = a.IsPrePlacementService,
                            ServiceTypeId = a.TypeId,
                            TaxId = a.TaxId,
                            LocationId = a.LocationId
                        })
                        .ToList();

                    return services.ToDictionary(s => s.serviceId, s => s);
                }
                catch
                {
                    // Final fallback: individual lookups (slowest but most reliable)
                    var result = new Dictionary<int, AddServiceVM>();
                    foreach (var id in idList)
                    {
                        try
                        {
                            var service = GetServicesById(id);
                            if (service != null)
                            {
                                result[id] = service;
                            }
                        }
                        catch
                        {
                            // Skip individual failures and continue
                        }
                    }
                    return result;
                }
            }
        }


        public bool AddService(AddServiceVM model)
        {
            //insert Service
            EF.Service tbl = new EF.Service
            {
                ServiceName = model.ServiceName,
                ServiceAmount = model.ServiceAmount,
                CreatedDate = DateTime.Now,
                CreatedBy = model.CreatedBy,
                IsEnable = true,
                IsActive = model.IsActive,
                AccountId = model.AccountId,
                TypeId=model.ServiceTypeId,
                IsPrePlacementService = model.IsPrePlacementService,
                TaxId = model.TaxId,
                LocationId = model.LocationId
            };
            uow.GenericRepository<EF.Service>().Insert(tbl);
            uow.SaveChanges();
            return true;
        }

        public bool UpdateService(AddServiceVM model)
        {
            EF.Service oldtbl = uow.GenericRepository<EF.Service>().GetByIdAsNoTracking(x => x.ServiceId == model.serviceId);
            EF.Service tbl = uow.GenericRepository<EF.Service>().GetById(model.serviceId);

            if (tbl != null)
            {
                tbl.AccountId = model.AccountId;
                tbl.ServiceName = model.ServiceName;
                tbl.ServiceAmount = model.ServiceAmount;
                tbl.IsActive = model.IsActive;
                tbl.UpdatedDate = DateTime.Now;
                tbl.UpdatedBy = model.UpdatedBy;
                tbl.TypeId = model.ServiceTypeId;
                tbl.IsPrePlacementService = model.IsPrePlacementService;
                tbl.TaxId = model.TaxId;
                tbl.LocationId = model.LocationId;

            }

            uow.SaveChanges();

            //Insert Audit Log
            {
                var difference = Common.Classes.Common.DetailedCompare<EF.Service>(oldtbl, tbl);
                List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Update,
                    ActionId = (int)Enumeration.CorrespondenceAction.UpdateServices,
                    PK = tbl.ServiceId.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "Service",
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    AuditLogDetails = difference
                };
                auditLogsService.AddAuditLog(auditLog);
            }



            return true;
        }

        public bool DeleteService(int id)
        {
            EF.Service oldtbl = uow.GenericRepository<EF.Service>().GetByIdAsNoTracking(x => x.ServiceId == id);
            EF.Service tbl = uow.GenericRepository<EF.Service>().GetById(id);

            if (tbl != null)
            {
                tbl.IsEnable = false;

                uow.GenericRepository<EF.Service>().Update(tbl);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Service>(oldtbl, tbl);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteServices,
                        PK = tbl.ServiceId.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Service",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }

                return true;
            }
            else
            {
                throw new Exception("service not found to delete.");
            }
        }

        public List<ServiceTypes> GetServiceType()
        {
            var types = uow.GenericRepository<EF.ServiceTypeLookup>().Table.Where(x => x.Status == true).Select(x => new ServiceTypes
            {
                Id = x.Id,
                Name = x.Name
            }).ToList();
            return types;
        }
    }
}
