using PMS.Common.Classes;
using PMS.DTO.ViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.AuditLogs;
using PMS.Services.Services.LocationContext;
using PMS.Services.Services.VoucherSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.Services.Services.CreditNote
{
    public class CreditNoteService : ICreditNoteService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IVoucherService voucherService;
        private readonly IAuditLogsService auditLogsService;
        private readonly ILocationContextService locationContextService;

        public CreditNoteService(UnitOfWork<PMSEntities> _uow, IVoucherService _voucherService, IAuditLogsService _auditLogsService, ILocationContextService _locationContextService)
        {
            uow = _uow;
            voucherService = _voucherService;
            auditLogsService = _auditLogsService;
            locationContextService = _locationContextService;
        }
        public List<CreditNoteType> GetTypes()
        {
            return uow.GenericRepository<CreditNoteTypeLookup>().GetAll().Select(x => new CreditNoteType
            {
                Id = x.Id,
                Name = x.Name
            }).ToList();
        }
        public string GetCode(int LocationId)
        {
            var prevcode = uow.GenericRepository<StudentCreditNote>().Table.Where(x => x.LocationId == LocationId).OrderByDescending(x => x.Code).Select(x => x.Code).FirstOrDefault();
            var randomNo = new Random().Next(0, 9999);
            var code = "CRD-0001-" + randomNo;
            if (prevcode != null)
            {
                var maxCode = String.Format("{0:D4}", (Convert.ToInt32(prevcode.Split('-')[1]) + 1));
                code = "CRD-" + maxCode + "-" + randomNo;
            }
            return code;
        }

        //public string GetCode(int LocationId, int TypeId)
        //{
        //    string typePrefix = "CRD"; // Default type prefix

        //    if (TypeId == 4) // Advance Payment
        //    {
        //        typePrefix = "ADV";
        //    }

        //    var lastCode = uow.GenericRepository<StudentCreditNote>()
        //                      .Table
        //                      .Where(x => x.LocationId == LocationId && x.Type == TypeId)
        //                      .OrderByDescending(x => x.Code)
        //                      .Select(x => x.Code)
        //                      .FirstOrDefault();

        //    int newNumber = 1; // Default start number
        //    if (!string.IsNullOrEmpty(lastCode) && lastCode.Contains("-"))
        //    {
        //        string[] parts = lastCode.Split('-');
        //        if (parts.Length == 2 && int.TryParse(parts[1], out int lastNumber))
        //        {
        //            newNumber = lastNumber + 1;
        //        }
        //    }

        //    return $"{typePrefix}-{newNumber:D3}";
        //}
        public string GetCode(int LocationId, int TypeId)
        {
            string prefix = "CRD"; // Default prefix
            if (TypeId == 4) // Advance Payment
            {
                prefix = "ADV";
            }

            var lastCode = uow.GenericRepository<StudentCreditNote>()
                .GetAll()
                .Where(x => x.Type == TypeId)
                .OrderByDescending(x => x.Code)
                .Select(x => x.Code)
                .FirstOrDefault();

            int newNumber = 1;
            if (!string.IsNullOrEmpty(lastCode))
            {
                var parts = lastCode.Split('-');
                if (parts.Length > 1 && int.TryParse(parts[1], out int lastNumber))
                {
                    newNumber = lastNumber + 1;
                }
            }

            string newCode = $"{prefix}-{newNumber:D4}-{new Random().Next(1000, 9999)}";
            return newCode;
        }


        public bool Add(StudentCreditNoteVm model)
        {
            try
            {
                var studentNote = new StudentCreditNote
                {
                    LocationId = model.LocationId,
                    Type = model.TypeId,
                    Code = model.Code,
                    StudentId = model.StudentId,
                    Amount = model.Amount,
                    Percentage = model.Percentage,
                    CreatedDate = DateTime.Now,
                    CreatedBy = Common.Globals.User.ID,
                    Reason = model.Reason,
                    Status = model.Status,
                    IsEnable = true,
                    IsUtilized = false,
                    PaymentTypeId = model.PaymentTypeId
                };

                if (model.Status == (int)Enumeration.Status.Approved)
                    studentNote.ApprovedBy = Common.Globals.User.ID;

                uow.GenericRepository<StudentCreditNote>().Insert(studentNote);
                uow.SaveChanges();

                CreateCreditNoteVoucher(studentNote);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public void CreateCreditNoteVoucher(StudentCreditNote studentCreditNote)
        {
            var request = new VoucherCreationRequest
            {
                VoucherType = VoucherType.CreditNote,
                BaseVoucherData = new BaseVoucherData
                {
                    VoucherDate = DateTime.Now,
                    ReferenceId = studentCreditNote.ID,
                    StudentId = studentCreditNote.StudentId,
                    Remarks = studentCreditNote.Reason,
                    CreatedDate = studentCreditNote.CreatedDate,
                    CreatedBy = studentCreditNote.CreatedBy,
                    LocationId = studentCreditNote.LocationId,
                    TransactionType = "CreditNote"

                },
                CreditNoteData = studentCreditNote
            };
            voucherService.CreateVoucherWithDetails(request, auditLogsService);
        }
        public List<StudentCreditNoteVm> GetAll()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();
            var voucherTable = uow.GenericRepository<Voucher>().Table.Where(x => x.TransactionType == "CreditNote");

            var queryResult = (from scn in uow.GenericRepository<StudentCreditNote>().Table
                               join v in voucherTable
                                   on scn.ID equals v.ReferenceId into voucherGroup
                               from voucher in voucherGroup.DefaultIfEmpty()
                               where scn.IsEnable == true &&
                                     scn.Type != 3 &&
                                     assignedLocationIds.Contains((int)scn.LocationId)
                               select new StudentCreditNoteVm
                               {
                                   Id = scn.ID,
                                   Code = scn.Code,
                                   Location = scn.Location.Ar_LocationName,
                                   Student = scn.Person.Code + ": " + scn.Person.FullName,
                                   Type = scn.CreditNoteTypeLookup.Name,
                                   Amount = scn.Amount,
                                   RemainingAmount = scn.AdjustedAmount == null ? scn.Amount : (scn.Amount - scn.AdjustedAmount ?? 0),
                                   Reason = scn.Reason,
                                   Status = scn.Status,
                                   IsUtilized = scn.IsUtilized,
                                   CreatedDate = scn.CreatedDate,
                                   CreatedBy = scn.UserMaster.FullName,
                                   ApprovedBy = scn.UserMaster1 == null ? "" : scn.UserMaster1.FullName,
                                   VoucherId = voucher != null ? voucher.VoucherId : (int?)null,
                               }).ToList();

            return queryResult;
        }


        public bool ApprovedById(int Id)
        {

            var creditnote = uow.GenericRepository<StudentCreditNote>().GetById(Id);
            if (creditnote == null)
            {
                throw new Exception("Record Does Not Exist!");
            }
            creditnote.ApprovedBy = Common.Globals.User.ID;
            creditnote.Status = (int)Enumeration.Status.Approved;
            uow.GenericRepository<StudentCreditNote>().Update(creditnote);
            uow.SaveChanges();
            return true;
        }


        public StudentCreditNoteVm GetById(int Id)
        {
            var creditNote = uow.GenericRepository<StudentCreditNote>().GetById(Id);
            var model = new StudentCreditNoteVm();
            if (creditNote == null)
            {
                return model;
            }
            model.Amount = creditNote.Amount;
            model.Code = creditNote.Code;
            model.Id = creditNote.ID;
            model.Reason = creditNote.Reason;
            model.TypeId = creditNote.Type;
            model.StudentId = creditNote.StudentId;
            model.LocationId = creditNote.LocationId;
            model.PaymentTypeId = creditNote.PaymentTypeId ?? 0;
            model.Percentage = creditNote.Percentage ?? 0;
            model.Status = creditNote.Status;
            return model;
        }



        public bool Edit(StudentCreditNoteVm model)
        {
            var db_value = uow.GenericRepository<StudentCreditNote>().GetById(model.Id);
            if (db_value == null)
                throw new Exception("Not Found!");

            db_value.Reason = model.Reason;
            db_value.LocationId = model.LocationId;
            db_value.StudentId = model.StudentId;
            db_value.Percentage = model.Percentage;
            db_value.Status = model.Status;
            db_value.Amount = model.Amount;
            db_value.Type = model.TypeId;
            db_value.PaymentTypeId = model.PaymentTypeId;
            db_value.UpdatedDate = DateTime.Now;
            db_value.UpdatedBy = Common.Globals.User.ID;

            if (model.Status == (int)Enumeration.Status.Approved)
                db_value.ApprovedBy = Common.Globals.User.ID;

            uow.GenericRepository<StudentCreditNote>().Update(db_value);
            uow.SaveChanges();
            return true;
        }



        public List<StudentCreditNoteVm> GetStudentCreditNote(int id)
        {
            var value = uow.GenericRepository<StudentCreditNote>().Table.Where(x => x.StudentId == id && x.IsUtilized == false && x.IsEnable == true
                        && x.Status == (int)Enumeration.Status.Approved).OrderByDescending(x => x.CreatedDate).Select(x => new StudentCreditNoteVm
                        {
                            Id = x.ID,
                            Type = x.CreditNoteTypeLookup.Name,
                            StudentId = x.StudentId,
                            Code = x.Code,
                            Amount = x.AdjustedAmount == null ? x.Amount : (x.Amount - x.AdjustedAmount ?? 0),
                            AdjustedAmount = x.AdjustedAmount ?? 0,
                            CurrencyName = x.Location.Currencies.Select(y => y.Name).FirstOrDefault()

                        }).ToList();
            return value;
        }
        public StudentCreditNoteVm GetForPaymentById(int id)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var paymenttype = uow.GenericRepository<PaymentType>().Table.Where(x => x.KeyCode == "CRD-01" && assignedLocationIds.Contains((int)x.LocationId)).FirstOrDefault();

            var creditNote = uow.GenericRepository<StudentCreditNote>().Table.Where(x => x.ID == id && x.IsUtilized == false && x.IsEnable == true
                        && x.Status == (int)Enumeration.Status.Approved).Select(x => new StudentCreditNoteVm
                        {
                            Amount = x.AdjustedAmount == null ? x.Amount : (x.Amount - x.AdjustedAmount ?? 0),
                            Id = x.ID,
                            PaymentTypeId = x.PaymentTypeId,
                            TypeId = x.Type
                        }).FirstOrDefault();

            //if (creditNote != null && paymenttype != null)
            //    creditNote.PaymentTypeId = paymenttype.PaymentId;

            if (creditNote != null && paymenttype != null)
            {
                // If TypeId is NOT 4, assign paymenttype.PaymentId; otherwise, keep the existing PaymentTypeId
                if (creditNote.TypeId == 4)
                {
                    creditNote.PaymentTypeId = creditNote.PaymentTypeId;
                }
                else
                {
                    creditNote.PaymentTypeId = paymenttype.PaymentId;
                }
            }

            return creditNote;
        }
        public bool SaveCreditDetail(int InvoiceId, decimal Amount, int CreditNoteId)
        {
            var creditNote = uow.GenericRepository<StudentCreditNote>().Table.Where(x => x.ID == CreditNoteId).FirstOrDefault();
            if (creditNote != null)
            {
                if (creditNote.AdjustedAmount == null)
                    creditNote.AdjustedAmount = 0;
                var detail = new StudentCreditNoteDetail
                {
                    StudentCreditNoteId = CreditNoteId,
                    InvoiceId = InvoiceId,
                    Amount = Amount,
                    CreatedDate = DateTime.Now,
                    CreatedBy = Common.Globals.User.ID
                };
                creditNote.AdjustedAmount += Amount;
                var remainingAmount = creditNote.Amount - creditNote.AdjustedAmount;
                if (remainingAmount == 0)
                    creditNote.IsUtilized = true;

                uow.GenericRepository<StudentCreditNoteDetail>().Insert(detail);
                uow.SaveChanges();
                return true;
            }
            return false;
        }
        public List<StudentCreditNoteVm> GetReferralCreditNotes()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();


            var data = uow.GenericRepository<StudentCreditNote>().GetAll(x => x.IsEnable == true && x.Type == 3 && assignedLocationIds.Contains((int)x.LocationId)).Select(x => new StudentCreditNoteVm
            {
                Id = x.ID,
                Code = x.Code,
                Location = x.Location.Ar_LocationName,
                Student = x.Person.FullName,
                Type = x.CreditNoteTypeLookup.Name,
                Amount = x.Amount,
                Reason = x.Reason,
                Status = x.Status,
                IsUtilized = x.IsUtilized,
                CreatedDate = x.CreatedDate,
                CreatedBy = x.UserMaster.FullName,
                ApprovedBy = x.UserMaster1 == null ? "" : x.UserMaster1.FullName
            }).ToList();
            return data;
        }
        public int? GetLatestCreatedCreditNoteId(int studentId, int locationId, decimal amount, int status)
        {
            var note = uow.GenericRepository<StudentCreditNote>().Table
                .Where(x => x.StudentId == studentId && x.LocationId == locationId && x.Amount == amount && x.Status == status)
                .OrderByDescending(x => x.CreatedDate)
                .FirstOrDefault();
            return note?.ID;
        }
    }
}
