using PMS.DTO.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.Sync
{
    public interface ISyncService
    {
        Task<bool> AddSync(SyncViewModel model);
        Task<List<SyncViewModel>> GetAllSync();
        Task<bool> UpdateSyncByEntity(int syncId);
        Task<bool> AddTicketSync(EF.Ticket ticket, int SyncTypeId = 1);
    }
}
