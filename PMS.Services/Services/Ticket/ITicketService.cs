using PMS.DTO;
using PMS.DTO.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.Services.Services.Ticket
{
    public interface ITicketService
    {
        List<TickeStatusLookupVm> GetActiveStatus();
        List<PeriorityLookupVm> GetActivePeriority();
       
        List<TicketViewModel> GetTickets(int? statusId,int? ticketId);

        ApiResponse<object> addTicket(TicketViewModel addTicketVM, HttpFileCollectionBase files);
        ApiResponse<object> AddTicketDetail(HttpFileCollectionBase files,TicketDetailViewModel model);
        ApiResponse<TicketViewModel> GetById(int Id);
        ApiResponse<object> Update(TicketViewModel model, HttpFileCollectionBase files,List<MockFiles> ExistingFile);
        ApiResponse<object> GetAttachement(int Id);
        List<TicketViewModel> GetStudentTickets(int Id);
        ApiResponse<List<CommentViewModel>> GetAllComments(int Id);
        ApiResponse<string> AddComment(CommentViewModel model);
        ApiResponse<TicketViewModel> GetDetailById(int Id);
        ApiResponse<string> UpdateStatus(TicketViewModel model);
        string GetMaxCode(int LocationId);
        ApiResponse<object> Delete(int Id);
        List<GroupLookupVm> GetActiveGroup();

        List<DropDownViewModel> GetUserByGroupId(int GroupId);

    }
}
