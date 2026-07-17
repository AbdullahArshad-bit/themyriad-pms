using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.EF;
using PMS.DTO.ViewModels.UserManageViewModels;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.DTO;
using System.Web;

namespace PMS.Services.Services.UserManage
{
    public interface IUserManageService
    {
        List<UserMaster> GetUsers();
        List<UserMaster> GetActiveUsers();
        UserMaster GetUserById(int id);
        UserMaster AddUser(AddUserVM userVM);
        UserMaster UpdateUser(AddUserVM userVM);
        bool DeleteUser(int id);


        List<Role> GetRoles();
        Role GetRoleById(int id);
        Role AddRole(AddRoleVM roleVM);
        Role UpdateRole(AddRoleVM roleVM);
        bool DeleteRole(int id);


        List<MenuVM> GetMenus(int roleId);

        bool SaveRoleRights(RoleRightsVM rightsVM);

        UserRole GetUserRoleByUserId(int userId);
        bool updateImage(int Id, HttpPostedFileBase file);
        bool UnActivePerson(int PersonId);
        bool SaveLocationRights(int ID, string AssignedLocation);
        LocatonRightsVM GetAssignedLocation(int id);


        //IService For APi

        ApiResponse<UserMasterViewModel> GetById(int Id);
        ApiResponse<UserMasterViewModel> updateImage(int Id, HttpFileCollection file);
    }
}
