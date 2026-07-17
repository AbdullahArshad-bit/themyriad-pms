using PMS.DTO.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.TicketGroup
{
    public interface ITicketGroupService
    {
        List<TicketGroupVm> GetAll(int? GroupId);
        bool SaveTicketGroup(List<TicketGroupVm> groupsVM);
        List<TicketGroupVm> GetGroupUserByRoleId(int RoleId, int GroupId);

    }
}
