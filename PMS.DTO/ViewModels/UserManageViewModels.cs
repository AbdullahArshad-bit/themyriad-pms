using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels.UserManageViewModels
{
    public class AddUserVM
    {
        public int UserID { get; set; }
        [Required]
        public bool IsActive { get; set; }

        [Display(Name = "Full Name")]
        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$", ErrorMessage = "Minimum eight characters, at least one uppercase letter, one lowercase letter, one number and one special character is required")]
        public string Password { get; set; }

        [Display(Name = "Confirm Password")]
        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "User Role")]
        [Required]
        public int RoleId { get; set; }

        public List<EF.Role> RolesList { get; set; }

        public string Department { get; set; }
        public string Designation { get; set; }


        [DataType(DataType.PhoneNumber)]
        [RegularExpression("^[0-9]*$", ErrorMessage = "Only numbers are accepted in Phone field.")]
        public string Phone { get; set; }

        
        [Required]
        public string Gender { get; set; }

        [Display(Name = "Date of Birth")]
        public DateTime DOB { get; set; }
        public string Address { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public int? PersonID { get; set; }
        public bool IsStudent { get; set; }
        [Required]
        public int LocationId { get; set; }
    }

    public class AddRoleVM
    {
        public int RoleID { get; set; }

        [Display(Name = "Role Name")]
        public string RoleName { get; set; }

        [Display(Name = "Role Description")]
        public string RoleDescription { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        [Required]
        public int LocationId { get; set; }
    }

    public class RoleRightsVM
    {
        [Required]
        public int RoleId { get; set; }
        
        public int UserMasterId { get; set; }

        public List<MenuVM> MenuList { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
  
    public class MenuVM
    {
        public MenuVM()
        {
            SubMenuList = new List<SubMenuVM>();
        }
        public int MainMenuId { get; set; }
        public string MainMenuName { get; set; }
        public List<SubMenuVM> SubMenuList { get; set; }

    }
    public class SubMenuVM
    {
        public int SubMenuId { get; set; }
        public bool IsChecked { get; set; }
        public string DisplayName { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
    }

    public class ChangePasswordVM
    {
        [Required(ErrorMessage = "This field is required.")]
        public string oldpassword { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        [StringLength(255, ErrorMessage = "Must be between 6 and 50 characters", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string newpassword { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        [Compare("newpassword")]
        public string ConfirmPassword { get; set; }

    }
    public class LocatonRightsVM
    {
        public string AssignedLocations { get; set; }
    }

}



