using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.AuditLogsViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.LocationContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.Services.Services.AuditLogs
{
   
    public  class AuditLogsService : IAuditLogsService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly ILocationContextService locationContextService;

        public AuditLogsService(UnitOfWork<PMSEntities> _uow, ILocationContextService _locationContextService)
        {
            uow = _uow;
            locationContextService = _locationContextService;
        }
        public void AddAuditLog(EF.AuditLog auditLog)
        {
            auditLog.TimeStamp = DateTime.Now;
            uow.GenericRepository<EF.AuditLog>().Insert(auditLog);
            uow.SaveChanges();

        }
        public void AddAuditLogList(List<EF.AuditLog> auditLog)
        {
            uow.GenericRepository<EF.AuditLog>().BulkInsert(auditLog);
            uow.SaveChanges();

        }
  

        public List<AuditLog> GetAuditLogs()
        {
            return uow.GenericRepository<AuditLog>().GetAll().ToList();
        }



        public List<AuditLog> GetAuditHistoryByPersoId(int PersonId)
        {
            var history = uow.GenericRepository<AuditLog>().Table.Where(x => x.PersonId == PersonId).OrderByDescending(x => x.TimeStamp).ToList();

            return history;
        }

        public AuditLogsResponse GetAuditLogsData(AuditLogsBinding request)
        {
            var query = BuildFilteredAuditLogsQuery(request);
            if (query == null)
            {
                return new AuditLogsResponse
                {
                    AuditLogs = new List<AuditLogsViewModels>(),
                    TotalRecords = 0,
                    RecordsFiltered = 0
                };
            }

            int totalRecords = query.Count();

            int start = string.IsNullOrEmpty(request.start) ? 0 : int.Parse(request.start);
            int length = string.IsNullOrEmpty(request.length) ? 10 : int.Parse(request.length);

            var pagedData = query.OrderByDescending(x => x.TimeStamp).Skip(start).Take(length).ToList();

            return new AuditLogsResponse
            {
                AuditLogs = pagedData,
                TotalRecords = totalRecords,
                RecordsFiltered = totalRecords
            };
        }

        public AuditLogsResponse ExportAuditLogsData(AuditLogsBinding request)
        {
            var query = BuildFilteredAuditLogsQuery(request);
            if (query == null)
            {
                return new AuditLogsResponse
                {
                    AuditLogs = new List<AuditLogsViewModels>(),
                    TotalRecords = 0,
                    RecordsFiltered = 0
                };
            }

            var exportData = query.OrderByDescending(x => x.TimeStamp).ToList();

            return new AuditLogsResponse
            {
                AuditLogs = exportData,
                TotalRecords = exportData.Count,
                RecordsFiltered = exportData.Count
            };
        }

        private IQueryable<AuditLogsViewModels> BuildFilteredAuditLogsQuery(AuditLogsBinding request)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();
            var ToDateAdded = request.ToDate.HasValue ? request.ToDate.Value.AddDays(1) : DateTime.Now.Date.AddDays(1);

            if (assignedLocationIds == null || !assignedLocationIds.Any())
                return null;

            var allowedUserIds = uow.GenericRepository<UserMaster>().Table
                .AsEnumerable()
                .Where(u =>
                    (!string.IsNullOrEmpty(u.AssignedLocation) && u.AssignedLocation != "0" &&
                     u.AssignedLocation.Split(',').Select(s => int.Parse(s.Trim())).Intersect(assignedLocationIds).Any())
                    || (u.LastLocationId.HasValue && assignedLocationIds.Contains(u.LastLocationId.Value)))
                .Select(u => u.ID)
                .ToList();

            var query = from a in uow.Context.AuditLogs
                        join ca in uow.Context.CorrespondenceActions on a.ActionId equals ca.Id
                        join p in uow.Context.People on a.PersonId equals p.PersonID into auditPerson
                        from person in auditPerson.DefaultIfEmpty()
                        where (person != null && assignedLocationIds.Contains((int)person.LocationId))
                           || (person == null && allowedUserIds.Contains(a.UserId))
                        select new AuditLogsViewModels
                        {
                            Id = a.Id,
                            PersonCode = person != null ? person.Code : string.Empty,
                            PersonFullName = person != null ? person.FullName : string.Empty,
                            ActionName = ca.ActionName,
                            AuditType = a.AuditType,
                            Reference = a.Reference,
                            TableName = a.TableName,
                            TimeStamp = a.TimeStamp,
                            UserName = a.UserName,
                            UserId = a.UserId,
                            ActionId = a.ActionId,
                            PK = a.PK
                        };

            if (request.Userid.HasValue && request.Userid.Value > 0)
                query = query.Where(x => x.UserId == request.Userid.Value);

            if (request.ActionId.HasValue && request.ActionId.Value > 0)
                query = query.Where(x => x.ActionId == request.ActionId.Value);

            if (request.AuditTypeId.HasValue)
                query = query.Where(x => x.AuditType == request.AuditTypeId.Value);

            if (request.search != null && !string.IsNullOrWhiteSpace(request.search.value))
            {
                var searchValue = request.search.value.Trim();
                var searchValueLower = searchValue.ToLower();

                int parsedNumber;
                int? parsedInt = int.TryParse(searchValue, out parsedNumber) ? parsedNumber : (int?)null;

                int? auditTypeFromText = null;
                switch (searchValueLower)
                {
                    case "read":
                        auditTypeFromText = 0;
                        break;
                    case "create":
                        auditTypeFromText = 1;
                        break;
                    case "update":
                        auditTypeFromText = 2;
                        break;
                    case "delete":
                        auditTypeFromText = 3;
                        break;
                }

                query = query.Where(x =>
                    (x.PersonCode != null && x.PersonCode.Contains(searchValue)) ||
                    (x.PersonFullName != null && x.PersonFullName.Contains(searchValue)) ||
                    (x.ActionName != null && x.ActionName.Contains(searchValue)) ||
                    (x.Reference != null && x.Reference.Contains(searchValue)) ||
                    (x.TableName != null && x.TableName.Contains(searchValue)) ||
                    (x.UserName != null && x.UserName.Contains(searchValue)) ||
                    (parsedInt.HasValue && (x.UserId == parsedInt.Value || x.AuditType == parsedInt.Value)) ||
                    (auditTypeFromText.HasValue && x.AuditType == auditTypeFromText.Value)
                );
            }

            if (request.FromDate.HasValue && request.ToDate.HasValue)
                query = query.Where(x => x.TimeStamp >= request.FromDate.Value && x.TimeStamp <= ToDateAdded);

            return query;
        }

    }
}
