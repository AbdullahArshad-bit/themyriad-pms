using PMS.DTO.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.CreditNote
{
    public interface ICreditNoteService
    {
        List<CreditNoteType> GetTypes();
        string GetCode(int LocationId);
        string GetCode(int LocationId, int TypeId);
        bool Add(StudentCreditNoteVm model);
        List<StudentCreditNoteVm> GetAll();
        bool ApprovedById(int Id);
        StudentCreditNoteVm GetById(int Id);

        bool Edit(StudentCreditNoteVm model);
        List<StudentCreditNoteVm> GetStudentCreditNote(int id);
        StudentCreditNoteVm GetForPaymentById(int id);
        bool SaveCreditDetail(int InvoiceId, decimal Amount, int CreditNoteId);
        List<StudentCreditNoteVm> GetReferralCreditNotes();
        int? GetLatestCreatedCreditNoteId(int studentId, int locationId, decimal amount, int status);
    }
}
