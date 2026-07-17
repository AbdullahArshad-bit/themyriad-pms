using PMS.Common.Classes;
using PMS.DTO.ViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static PMS.Common.Classes.Enumeration;

namespace PMS.Services.Services.Sync
{
    public class SyncService : ISyncService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        public SyncService(UnitOfWork<PMSEntities> _uow)
        {
            uow = _uow;
        }
        public async Task<bool> AddSync(SyncViewModel model)
        {
            try
            {
             
                var jobsSeting = uow.GenericRepository<JobConfiguration>().Table.Where(x => x.CategoryId == model.SyncCategoryId).ToList();
                foreach (var item in jobsSeting)
                {
                     
                        if (model.PropertyValue==item.PropertyValue)
                        {
                        var Registersync = new RegisterSync
                            {
                                SyncCategory = model.SyncCategoryId,
                                SyncType = model.SyncTypeId,
                                EnitityId = model.EnitityId,
                                CreatedDate = model.CreatedOn,
                                CreatedBy = model.CreatedBy

                            };


                        var Sync = new EF.Sync
                        {
                            PropertyValue = item.PropertyValue,
                            Scope = model.Scope,
                            FirstExecutionOn = model.CreatedOn.AddHours(Convert.ToDouble(item.FirstExecutionOn)),
                            FirstJobActionId = item.FirstExecutionActionId,
                            Status = true,
                            CreatedOn = model.CreatedOn,
                            IsExecuted=false
                        };
                            Sync.NextExecutionOn = Sync.FirstExecutionOn.Value.AddHours(Convert.ToDouble(item.SecondExecutionOn));
                            Sync.NextJobActionId = item.SecondExecutionActionId;
                            Registersync.Syncs.Add(Sync);
                            uow.GenericRepository<EF.RegisterSync>().Insert(Registersync);
                        }
                    }

                uow.SaveChanges();

                return true;

            }catch(Exception ex)
            {
                return false;
            }
        }

        public async Task<List<SyncViewModel>> GetAllSync()
        { 

            
                var db = uow.Context;
            var result = (from registersync in db.RegisterSyncs
                          join sync in db.Syncs on registersync.Id equals sync.SyncId
                          where  sync.Status == true  && (sync.IsExecuted==false || sync.IsExecuted ==null)
                          select new SyncViewModel
                          {
                              SyncId = sync.Id,
                              EnitityId=registersync.EnitityId,
                              SyncTypeId=registersync.SyncType,
                              SyncCategoryId=registersync.SyncCategory,
                              PropertyValue=sync.PropertyValue,
                              FirstExecution=sync.FirstExecutionOn,
                              LastUsedOn=sync.LastUsedOn,
                              NextExcecutionOn=sync.NextExecutionOn,
                              NextExecutionActionId=sync.NextJobActionId,
                              FirstExecutionActionId=sync.FirstJobActionId,
                              Scope=sync.Scope
                          }
                        ).ToList();

            return result;
        }

        public async Task<bool> UpdateSyncByEntity(int syncId)
        {
            var sync = uow.GenericRepository<EF.Sync>().Table.Where(x => x.Id == syncId)
                .FirstOrDefault();

            sync.LastUsedOn = DateTime.Now;

            if (sync.LastUsedOn >= sync.NextExecutionOn)
                sync.IsExecuted = true;

            uow.SaveChanges();
            return true;
        }

        public async Task<bool> AddTicketSync(EF.Ticket ticket,int SyncTypeId=1)
        {
            if (ticket != null)
            {
                var previouse = uow.GenericRepository<EF.Sync>().Table.Where(x => x.RegisterSync.SyncCategory == (int)SyncCategory.Ticket
                 && x.RegisterSync.SyncType == SyncTypeId && x.RegisterSync.EnitityId == ticket.Id);

                var IsExist = false;
                if (SyncTypeId == (int)SyncType.Status)
                    IsExist = previouse.Any(x => x.PropertyValue == ticket.StatusId.ToString() && x.IsExecuted!=true);
                else
                    IsExist = previouse.Any(x => x.PropertyValue == SyncType.DueDate.ToString() && x.IsExecuted != true);

                   if ((ticket.StatusId == (int)TicketStatus.Open ||
                       ticket.StatusId == (int)TicketStatus.Pending) && SyncTypeId == (int)SyncType.Status && !IsExist)
                   {

                    await UpdatePreviousSync(ticket.Id, (int)SyncType.Status);
                    var syncModel = new SyncViewModel
                    {
                        EnitityId = ticket.Id,
                        SyncCategoryId = (int)SyncCategory.Ticket,
                        SyncTypeId = (int)SyncType.Status,                      
                        CreatedBy = Common.Globals.User.Email,
                        PropertyName = SyncType.Status.ToString(),
                        PropertyValue = ticket.StatusId.ToString()

                    };
                    if (ticket.UpdatedDate != null)
                        syncModel.CreatedOn = ticket.UpdatedDate.Value;
                    else
                        syncModel.CreatedOn = ticket.CreatedDate;

                    AddSync(syncModel);
                }
                else if ((ticket.StatusId == (int)TicketStatus.Resolved ||ticket.StatusId == (int)TicketStatus.Closed ||
                   ticket.StatusId == (int)TicketStatus.WaitingOnCustomer))
                {
                    await UpdatePreviousSync(ticket.Id, (int)SyncType.Status);
                }
                
                
            }
            return true;
        }

        public async Task<bool> UpdatePreviousSync(int EntityId,int SyncType=1)
        {
            var prevousesync=uow.GenericRepository<EF.Sync>().Table.Where(x => x.RegisterSync.SyncType == SyncType && x.RegisterSync.EnitityId == EntityId
            && x.IsExecuted != true
            ).FirstOrDefault();
            if (prevousesync != null)
            {
                prevousesync.IsExecuted = true;
                uow.SaveChanges();
            }
            
            return true;
        }
        private string GetDynamicPropertyValue(dynamic entity ,string PropertyName)
        {
            var propertyInfo = entity.GetType().GetProperty(PropertyName);
            if (propertyInfo != null)
            {
                var value = Convert.ToString(propertyInfo.GetValue(entity, null));
                return value;
            }
            return null;
        }
    }
}
