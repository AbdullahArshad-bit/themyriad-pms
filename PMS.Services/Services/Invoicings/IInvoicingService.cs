using PMS.DTO;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.DTO.ViewModels.PaymentViewModels;
using PMS.DTO.ViewModels.ReportingViewModels;
using PMS.DTO.ViewModels.SetupViewModels;
using PMS.EF;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.Invoicings
{
    public interface IInvoicingService
    {
        List<Invoicing> GetInvoicesByStudentId(int studentId);
        OutputInvoicingVM GetStudentOccupancy(int serviceId, int residentId, int invoiceTypeId);
        List<OutputInvoicingVM> GetAllDepositOptions(int serviceId, int residentId);
        List<InvoicingDetailVM> GetInvoiceDetail(int InvoiceId);
        Invoicing GetLastInoviceByStudentIdId(int? StudentId);
        List<InvoicingDetailVM> GetFeeAssessmentInvoiceDetail(int InvoiceId);
        EF.Invoicing GetById(int? Id);
        InvoicingsResponse Getcalculation();
        List<EF.InvoiceTypeLookup> GetInvoicingTypes();
        bool SaveInvoice(Invoicing invoicing, List<InvoicingDetail> list);
        void CreateRevInvVoucher(Invoicing invoicing, List<InvoicingDetail> list);
        List<InvoicingVM> GetByPersonId(int PersonId);
        InvoicingVM GetEditInvoiceById(int InvoiceId);
        bool Approve(int id);
        string GetMaxInvoiceCodeString(int id, int InvoiceTypeId);
        InvoicingsResponse GetAll(InvoicingBinding request, string QueryBY, string searchValue, string start, string lenght, string query = null, string orderBy = null, string orderDir = "asc", DateTime? FromDate = null, DateTime? ToDate = null, int? InvoiceTypeId = null);
        InvoicingsResponse ExportInvoiceReport(string QueryBY, DateTime? FromDate = null, DateTime? ToDate = null, int? InvoiceTypeId = null);
        Task<InvoicingsResponse> ExportInvoiceReportAsync(string QueryBY, DateTime? FromDate = null, DateTime? ToDate = null, int? InvoiceTypeId = null);
        List<OutputInvoicingVM> GetActiveInvoicesByPerson(int personId);
        List<PaymentVM> ReceiptDetail(int? id);

        //Services for api
        ApiResponse<List<OutputInvoicingVM>> GetInvoicesById(int Id);

        // New: unpaid invoices with remaining due for payment allocation UI
        List<UnpaidInvoiceVM> GetUnpaidInvoicesWithDueByPerson(int personId);
        ApiResponse<OutputInvoicingVM> GetUnpaidInvoice(int Id);
        AddTermVM GetFrequencyById(int Id, int configId);
        DepositInvoicesVM GetDepositInvoices(int InvoiceId);
        bool CloneInvoice(DepositInvoicesVM depositInvoicesVM, string source = "deposit");
        bool SaveCloneInvoicePayment(DepositInvoicesVM depositInvoicesVM);

        //For Multiple fee assessment
        bool IsPersonBookedAndPlaced(int personId, DateTime startDate, DateTime endDate);
        LastInvoiceCheckResult GetLastMultipleInvoicesStatusWithInvoice(int? studentId, DateTime fromDate, DateTime toDate);
        List<InvoicingDetail> GetInvoiceDetailsByInvoiceId(int invoiceId);
        List<InvoicingDetail> GetMultipleInvoiceDetail(Invoicing inv, int InvoiceId, DateTime invoiceStartDate, DateTime invoiceEndDate);
        bool SaveMultipleInvoices(Invoicing invoicing, List<InvoicingDetail> list);
        Invoicing GetInoviceForReverse(int id);
        List<InvoicingDetail> GetInvoiceDetailsForReverse(int invoiceId);
        bool SaveReverseInvoice(Invoicing invoicing, List<InvoicingDetail> list);
        (bool Success, string SuccessMessage, string ErrorMessage) GenerateInvoices(int[] personIds, DateTime startDate, DateTime endDate);
        (bool Success, string Message) ProcessRefundedInvoicePayment(DepositInvoicesVM depositInvoicesVM);
        void ProcessInvoiceAdvancePayments(int invoiceId, decimal invoiceAmount, int studentId, int locationId);
        void CreateInvoicingVoucher(Invoicing invoicing, List<InvoicingDetail> list);
        void CreateDepositInvoiceAndPayment(BookingVM bookingVM, int personId);
        int? GetServiceType(int invoiceDetailId);
    }
}
