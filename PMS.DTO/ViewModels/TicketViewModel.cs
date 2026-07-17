using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels
{
    public class TicketViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TypeId { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; }
        public int PeriorityId { get; set; }
        public string PriorityName { get; set; }
        public string Source { get; set; }
        public int StatusId { get; set; }
        public bool IsEnable { get; set; }
        public string Status { get; set; }
        public DateTime? DueDate { get; set; }
        public string DueDateString { get; set; }
        public int AssignTo { get; set; }
        public string AssignToName { get; set; }
        public int IssueBy { get; set; }
        public string IssueByName { get; set; }
        public string IssueName { get; set; }
        //[RegularExpression(@"^[A-Za-z0-9](([_\.\-]?[a-zA-Z0-9]+)*)@([A-Za-z0-9]+)(([\.\-‌​]?[a-zA-Z0-9]+)*)\.([A-Za-z]{2,})$", ErrorMessage = "Email is not valid")]
        public string IssueByEmail { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public string Description { get; set; }
        public string ResolvedDate { get; set; }
        public string Code { get; set; }
        public int IssueByStaff { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public List<TicketDetailViewModel> TicketDetailViewModel { get; set; }
    }
    public class TicketDetailViewModel
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string Description { get; set; }
        public List<TicketDetailAttachementVm> TicketDetailAttachementVm { get; set; }
    }
    public class TicketDetailAttachementVm
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public long FileSize { get; set; }
        public int TicketDetailId { get; set; }
        public string FileType { get; set; }
        public string Description { get; set; }
    }
    public class MockFiles
    {
        public string name { get; set; }
        public int size { get; set; }
        public int serverID { get; set; }
    }
    public class TickeStatusLookupVm
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class PeriorityLookupVm
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class CommentViewModel
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string Description { get; set; }
        public string UserImageUrl { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedDate { get; set; }
    }

    public class GroupLookupVm
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class TicketGroupVm
    {
        public string Name { get; set; }
        public string RoleName { get; set; }
        public int UserId { get; set; }
        public int? GroupUserID { get; set; }
        public int? GroupRoleId { get; set; }
        public int GroupId { get; set; }
        public string UserEmail { get; set; }
        public List<TicketGroupRole> TicketGroupRole { get; set; }
        
    }
    public class TicketGroupRole
    {
        public int Id { get; set; }
        public string Name { get; set; }

    }
}
