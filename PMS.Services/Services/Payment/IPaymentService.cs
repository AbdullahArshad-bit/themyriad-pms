using PMS.DTO.ViewModels.PaymentViewModels;
using PMS.EF;
using PMS.Services.Services.AuditLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.Payment
{
    public interface IPaymentService
    {
        List<PaymentVM> GetPayment(int personId);
        string GetMaxCode(int LocationId);
        decimal InvoicePayableAmount(int Id);
        //List<PaymentVM> GetAll(DateTime? FromDate, DateTime? ToDate);
        PaymentResponse GetAll(PaymentBinding request, string QueryBY, string searchValue, string start, string lenght, string orderBy = null, string orderDir = "asc", string query = null, DateTime? FromDate = null, DateTime? ToDate = null, int? StudentId = 0);
        PaymentResponse GetRefunds(PaymentBinding request, string QueryBY, string searchValue, string start, string length, string query = null, string orderBy = null, string orderDir = "asc", DateTime? FromDate = null, DateTime? ToDate = null, int? StudentId = 0);
        PaymentResponse ExportPaymentReport(string QueryBY, DateTime? FromDate = null, DateTime? ToDate = null);
        PaymentResponse ExportRefundPaymentReport(string QueryBY, DateTime? FromDate = null, DateTime? ToDate = null);
        void CreatePayVoucher(StudentLedger studentLedger);
        void CreateRevPayVoucher(StudentLedger studentLedger);
        void CreateCreditNotePaymentVoucher(StudentLedger studentLedger);
        void CreateAdvancePaymentVoucher(StudentLedger studentLedger);

        void UpdatePaymentVoucher(StudentLedger studentLedger, IAuditLogsService auditLogsService);
        PMS.DTO.ViewModels.PartnerLedgerPageViewModel GetPartnerLedger(DateTime? FromDate, DateTime? ToDate, int PersonId, bool Status = true);

        // New methods for payment creation logic
        bool ValidatePaymentAmount(int? invoiceId, decimal? amount);
        string GenerateReceiptCode(int locationId);

        // New: process allocations across multiple invoices under one receipt code
        //void ProcessPaymentAllocations(EF.StudentLedger baseLedger, List<PMS.DTO.ViewModels.PaymentViewModels.InvoiceAllocationDTO> allocations);
        
        // Get child records for a parent payment
        List<PaymentVM> GetChildPayments(int parentId);

        // grouped removed
        decimal CalculateCreditAmountByCreditNote(int invoiceId, int creditNoteId);
        decimal GetRemainingPayablesByInvoiceId(int invoiceId);
        void UpdateInvoicePaymentStatus(int? invoiceId);
        void SendPaymentReceiptEmail(StudentLedger studentLedger);
        void SendPaymentNotification(StudentLedger studentLedger);
        void CreateAuditLog(StudentLedger studentLedger);
        void CreateAuditLogForOnlinePayment(StudentLedger studentLedger);
        void ValidateInvoicePaymentOrder(StudentLedger studentLedger);
        void ProcessPayment(StudentLedger studentLedger);
        void ProcessPaymentUpdate(StudentLedger studentLedger);

        // Partner ledger payment (advance/refund payment not tied to an invoice)
        StudentLedger CreatePartnerLedgerPayment(decimal DebitAmount, int PaymentTypeId, string Remarks, int PersonId, int LocationId);

        StudentLedger CreateDepositPaymentLedger(int invoiceId, int personId, decimal netAmount, int locationId, int paymentMethodId, string TranRef, int createdByUserId);

        void CreateOnlinePayVoucher(StudentLedger studentLedger, int createdByUserId);

        // Reverse a posted payment (TMD only)
        StudentLedger ReversePayment(int paymentId, decimal amount, int paymentTypeId, string remarks);
    }
}
