using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using PMS.Common;
using PMS.Common.Classes;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.ContractViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.DTO.ViewModels.ContractViewModels;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using PMS.Services.Services.Correspondence;
using PMS.Services.Services.Email;
using PMS.Services.Services.AuditLogs;
using PMS.Services.Services.BedSpacePlace;
using PMS.DTO;
using PMS.Services.Services.Notifications;
using System.Data.Entity.Core.Objects;
using System.Data.Entity;
using PMS.Services.Services.LocationContext;

namespace PMS.Services.Services.Contracts
{
    public class ContractManageService : IContractManageService
    {
        // Optimized contract template cache (per ContractId)
        private static readonly ConcurrentDictionary<int, ContractTemplateCache> _contractTemplateCache = new ConcurrentDictionary<int, ContractTemplateCache>();
        private static readonly Regex TokenRegex = new Regex(@"\[\[(?<k>[^\]]+)\]\]", RegexOptions.Compiled);

        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IEmailService emailService;
        private ICorrespondenceService correspondenceService;
        private readonly IAuditLogsService auditLogsService;
        private readonly IBedSpacePlacementService placementService;
        private readonly INotificationService notificationService;
        private readonly ILocationContextService locationContextService;

        public ContractManageService(UnitOfWork<PMSEntities> _uow, ICorrespondenceService _correspondenceService, IEmailService _emailService, IAuditLogsService _auditLogsService,
            IBedSpacePlacementService _placementService, INotificationService _notificationService, ILocationContextService _locationContextService)
        {
            uow = _uow;
            correspondenceService = _correspondenceService;
            emailService = _emailService;
            auditLogsService = _auditLogsService;
            placementService = _placementService;
            notificationService = _notificationService;
            locationContextService = _locationContextService;
        }

        // Refresh or invalidate the per-contract template cache entry
        private void RefreshContractTemplateCache(int contractId)
        {
            try
            {
                var vm = GetContractVMById(contractId);
                var updatedCache = new ContractTemplateCache
                {
                    EditorContent = vm?.Content?.EditorContent ?? string.Empty,
                    EmailMessageId = vm?.Email?.EmailMessageID ?? 0,
                    ContractName = vm?.Properties?.ContractName ?? string.Empty
                };

                _contractTemplateCache.AddOrUpdate(contractId, updatedCache, (key, old) => updatedCache);
            }
            catch
            {
                // Best-effort: if refresh fails, drop the cache so it can be lazily rebuilt later
                ContractTemplateCache _;
                _contractTemplateCache.TryRemove(contractId, out _);
            }
        }
        public ContractTemplateCache GetCachedContractTemplate(int contractId)
        {
            return _contractTemplateCache.GetOrAdd(contractId, id =>
            {
                var vm = GetContractVMById(id);
                return new ContractTemplateCache
                {
                    EditorContent = vm?.Content?.EditorContent ?? string.Empty,
                    EmailMessageId = vm?.Email?.EmailMessageID ?? 0,
                    ContractName = vm?.Properties?.ContractName ?? string.Empty
                };
            });
        }

        public ContractPlacementData GetPlacementWithRelatedDataOptimized(int placementId)
        {
            var db = uow.Context;
            var data = (from pl in db.BedSpacePlacements.AsNoTracking()
                        join b in db.Bookings.AsNoTracking() on pl.BookingID equals b.BookingID
                        join p in db.People.AsNoTracking() on b.PersonID equals p.PersonID
                        join pc in db.PriceConfigs.AsNoTracking() on b.PriceConfigID equals pc.PriceConfigID
                        join rt in db.RoomTypes.AsNoTracking() on pc.RoomTypeID equals rt.RoomTypeID
                        where pl.BedSpacePlacementID == placementId
                        select new ContractPlacementData
                        {
                            PlacementId = pl.BedSpacePlacementID,
                            BookingId = b.BookingID,
                            PersonId = p.PersonID,
                            LocationId = b.LocationID,
                            PersonLocationId = p.LocationId,
                            PersonFullName = p.FullName,
                            PersonCode = p.Code,
                            PersonEmail = p.Email,
                            PersonPhone = p.Phone,
                            PersonNationality = p.Nationality,
                            UniversityName = p.University.UniversityName,
                            UniversityText = p.Universiry,
                            RoomTypeName = rt.RoomName,
                            BuildingName = pl.BedSpace.Room.Floor.Building.BuildingName,
                            FloorName = pl.BedSpace.Room.Floor.FloorName,
                            RoomName = pl.BedSpace.Room.RoomName,
                            BedName = pl.BedSpace.BedName,
                            CheckInDate = b.CheckInDate,
                            CheckOutDate = b.CheckOutDate,
                            ECFullName = p.EmergencyContacts.Select(ec => ec.FullName).FirstOrDefault(),
                            ECRelation = p.EmergencyContacts.Select(ec => ec.Relation).FirstOrDefault(),
                            ECEmail = p.EmergencyContacts.Select(ec => ec.Email).FirstOrDefault(),
                            ECPhone = p.EmergencyContacts.Select(ec => ec.Phone).FirstOrDefault()
                        }).FirstOrDefault();
            return data;
        }

        public string ProcessContractContentOptimized(string template, ContractPlacementData pd, StudentConractsVM vm, string currentDate, string contractDueDate)
        {
            var tokens = new Dictionary<string, string>
            {
                ["Current_Date"] = currentDate,
                ["PersonID"] = pd.PersonCode,
                ["Person_Country"] = pd.PersonNationality,
                ["EmergencyContact_FullName"] = pd.ECFullName ?? string.Empty,
                ["EmergencyContact_Relation"] = pd.ECRelation ?? string.Empty,
                ["EmergencyContact_Email"] = pd.ECEmail ?? string.Empty,
                ["EmergencyContact_Phone"] = pd.ECPhone ?? string.Empty,
                ["Person_UniversityName"] = pd.UniversityName ?? string.Empty,
                ["PersonAddress"] = string.Empty,
                ["PersonFull_Name"] = pd.PersonFullName,
                ["Person_Email"] = pd.PersonEmail,
                ["PersonPhone"] = pd.PersonPhone,
                ["PersonGuardianFullName"] = string.Empty,
                ["PersonGuardianEmail"] = string.Empty,
                ["PersonGuardianPhone"] = string.Empty,
                ["PersonUniversity"] = pd.UniversityText ?? string.Empty,
                ["BookingNumber"] = vm.BookingId > 0 ? vm.BookingId.ToString() : pd.BookingId.ToString(),
                ["BookingRoomManagementRoomType"] = pd.RoomTypeName,
                ["BookingRoomManagementBuilding"] = pd.BuildingName,
                ["BookingRoomManagementFloor"] = pd.FloorName,
                ["BookingRoomManagementRoomNo"] = pd.RoomName,
                ["BookingRoomManagementBedSpace"] = pd.BedName,
                ["BookingRoomManagementMoveInDate"] = pd.CheckInDate.ToString("dd/MM/yyyy"),
                ["BookingRoomManagementNoOFMonths"] = ((pd.CheckOutDate - pd.CheckInDate)?.TotalDays.ToString() ?? "0"),
                ["BookingRoomManagementMoveOutDate"] = (pd.CheckOutDate ?? pd.CheckInDate).ToString("dd/MM/yyyy"),
                ["BookingGrossAmount"] = vm.GrossAmount.ToString(),
                ["BookingTaxAmount"] = vm.TaxAmount.ToString(),
                ["BookingRegistrationFee"] = vm.RegistrationFee.ToString(),
                ["BookingNetAmount"] = vm.NetAmount.ToString(),
                ["BookingSecurityDeposit"] = vm.SecurityDeposit.ToString(),
                ["BookingDiscountAmount"] = vm.DiscountAmount.ToString(),
                ["Assignment.LayoutRepeat.End"] = string.Empty,
                ["ContractDueDate"] = contractDueDate
            };

            string output = TokenRegex.Replace(template ?? string.Empty, m =>
            {
                var key = m.Groups["k"].Value;
                return tokens.TryGetValue(key, out var val) ? val ?? string.Empty : string.Empty;
            });

            return output;
        }

        public List<ContractTypesVM> GetContractTypes()
        {
            return uow.GenericRepository<ContractType>().Table.Where(x => x.IsEnable == true).ToList()
                .Select(x => new ContractTypesVM
                {
                    ContractTypeID = x.ContractTypeID,
                    ContractTypeName = x.ContractTypeName,
                    ContractTypeDescription = x.ContractTypeDescription,
                    CreatedDate = x.CreatedDate,
                    CreatedBy = x.CreatedBy,
                    UpdatedDate = Convert.ToDateTime(x.UpdatedDate),
                    UpdatedBy = x.UpdatedBy
                }).ToList();
        }

        public ContractTypesVM GetContractTypeById(int id)
        {
            var contractType = uow.GenericRepository<ContractType>().Table.FirstOrDefault(x => x.IsEnable == true && x.ContractTypeID == id);
            if (contractType != null)
            {
                return new ContractTypesVM
                {
                    ContractTypeID = contractType.ContractTypeID,
                    ContractTypeName = contractType.ContractTypeName,
                    ContractTypeDescription = contractType.ContractTypeDescription,
                    CreatedDate = contractType.CreatedDate,
                    CreatedBy = contractType.CreatedBy,
                    UpdatedDate = Convert.ToDateTime(contractType.UpdatedDate),
                    UpdatedBy = contractType.UpdatedBy
                };
            }
            else
            {
                return null;
            }
        }

        public ContractType AddContractType(ContractTypesVM model)
        {
            EF.ContractType type = new EF.ContractType
            {
                ContractTypeName = model.ContractTypeName,
                ContractTypeDescription = model.ContractTypeDescription,
                CreatedDate = model.CreatedDate,
                CreatedBy = model.CreatedBy,
                IsEnable = true
            };

            uow.GenericRepository<EF.ContractType>().Insert(type);
            uow.SaveChanges();

            return type;
        }

        public ContractType UpdateContractType(ContractTypesVM model)
        {
            EF.ContractType type = uow.GenericRepository<EF.ContractType>().GetById(model.ContractTypeID);

            if (type != null)
            {
                type.ContractTypeName = model.ContractTypeName;
                type.ContractTypeDescription = model.ContractTypeDescription;
                type.UpdatedDate = model.UpdatedDate;
                type.UpdatedBy = model.UpdatedBy;

                uow.GenericRepository<EF.ContractType>().Update(type);
                uow.SaveChanges();

                return type;
            }
            else
            {
                throw new Exception("Contract type not found to update.");
            }
        }

        public bool DeleteContractType(int id)
        {
            EF.ContractType type = uow.GenericRepository<EF.ContractType>().GetById(id);

            if (type != null)
            {
                type.IsEnable = false;

                uow.GenericRepository<EF.ContractType>().Update(type);
                uow.SaveChanges();

                return true;
            }
            else
            {
                throw new Exception("Contract type not found to delete.");
            }
        }

        public List<SelectListVM> GetContractTypesDD()
        {
            return uow.GenericRepository<ContractType>().Table.Where(x => x.IsEnable == true)
                .Select(x => new SelectListVM
                {
                    Text = x.ContractTypeName,
                    Value = x.ContractTypeID.ToString()
                }).ToList();
        }

        public List<ContractsListVM> GetContracts(string contractNumber = "")
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var contracts = uow.Context.Contracts
        .Include("ContractType")
        .Include("ContractContent")
        .Where(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId));

            //var contracts = uow.GenericRepository<Contract>().Table.ToList().Where(x => x.IsEnable == true);

            if (!string.IsNullOrEmpty(contractNumber))
            {
                contracts = contracts.Where(x => x.ContractNumber == contractNumber);
            }

            return contracts.Select(y => new ContractsListVM
            {
                ContractID = y.ContractID,
                ContractTypeID = y.ContractTypeID,
                ContractName = y.ContractName,
                ContractNumber = y.ContractNumber,
                ContractReferenceNumber = y.ContractReference,
                ContractVersion = y.ContractVersion,
                ContractType = y.ContractType.ContractTypeName,
                IsActive = y.IsActive,
                IsPublish = y.IsPublish,
                ContentType = y.ContractContent.ContentType,
                ContentValue = y.ContractContent.ContentType.ToLower().Contains("file") ? y.ContractContent.ContentValue : "",
                LocationName = y.Location.LocationName
            }).ToList();
        }

        public EF.Contract GetContractById(int id)
        {
            return uow.Context.Contracts.Include("ContractContent").Include("ContractAssertions").Include("ContractSignature").Include("ContractEmail")
                .Where(x => x.IsEnable == true && x.ContractID == id).FirstOrDefault();
        }

        public AddContractVM GetContractVMById(int id)
        {
            var contract = GetContractById(id);

            if (contract != null)
            {
                var c = new AddContractVM
                {
                    ContractID = contract.ContractID,
                    OriginalContractID = contract.ContractID,
                    IsEnable = contract.IsEnable,
                    IsPublish = contract.IsPublish,
                    CreatedDate = contract.CreatedDate,
                    CreatedBy = contract.CreatedBy,
                    UpdatedDate = Convert.ToDateTime(contract.UpdatedDate),
                    UpdatedBy = contract.UpdatedBy,
                    Properties = new ContractProperties
                    {
                        IsActive = contract.IsActive,
                        ContractName = contract.ContractName,
                        ContractNumber = contract.ContractNumber,
                        ContractTypeID = contract.ContractTypeID,
                        ContractVersion = contract.ContractVersion,
                        ContractReferenceNumber = contract.ContractReference,
                        Description = contract.ContractDescription
                    },
                    Content = new DTO.ViewModels.ContractViewModels.ContractContent(),
                    Assertions = contract.ContractAssertions.Select(x =>
                    new ContractAssertions
                    {
                        ContractAssertionID = x.ContractAssertionID,
                        ShowCheckBox = x.ShowCheckBox,
                        AssertionText = x.AssertionText,
                        AssertionRequired = x.AssertionRequired,
                        AssertionHyperlinkText = x.AssertionHyperlinkText,
                        AssertionHyperlinkUrl = x.AssertionHyperlinkUrl
                    }).ToList(),
                    Email = new DTO.ViewModels.ContractViewModels.ContractEmail(),
                    Signature = new DTO.ViewModels.ContractViewModels.ContractSignature(),
                    Notes = new ContractNotes
                    {
                        Notes = contract.ContractNotes
                    }
                };

                if (contract.ContractContent != null)
                {
                    c.Content = new DTO.ViewModels.ContractViewModels.ContractContent
                    {
                        ContentType = contract.ContractContent.ContentType
                    };

                    if (contract.ContractContent.ContentType.ToLower().Contains("file"))
                    {
                        c.Content.FileContentPath = contract.ContractContent.ContentValue;
                    }
                    else
                    {
                        c.Content.EditorContent = contract.ContractContent.ContentValue;
                    }
                }
                if (contract.ContractEmail != null)
                {
                    c.Email = new DTO.ViewModels.ContractViewModels.ContractEmail
                    {
                        SendEmailOnContractAcceptance = contract.ContractEmail.SendEmailOnAcceptance,
                        EmailMessageID = contract.ContractEmail.EmailMessageID,
                        IncludeContractAsPDF = contract.ContractEmail.IncludeContractAsPDF
                    };
                }
                if (contract.ContractSignature != null)
                {
                    c.Signature = new DTO.ViewModels.ContractViewModels.ContractSignature
                    {
                        RequireElectronicSignature = contract.ContractSignature.RequireElectronicSignature,
                        SignatureText = contract.ContractSignature.SignatureText
                    };
                }

                return c;
            }
            else
                return null;
        }

        public AddContractVM AddContract(AddContractVM model)
        {
            if (!string.IsNullOrEmpty(model.Properties.ContractNumber))
            {
                if (ContractVersionExists(model.Properties.ContractNumber, model.Properties.ContractVersion))
                    throw new Exception("Contract version already exist with same contract number.");
            }


            try
            {
                var previousContract = uow.GenericRepository<EF.Contract>().GetById(model.OriginalContractID);
                if (previousContract != null)
                {
                    previousContract.IsPublish = false;
                    uow.GenericRepository<EF.Contract>().Update(previousContract);
                    uow.SaveChanges();
                }
                uow.CreateTransaction();

                Contract contract = new Contract
                {
                    ContractTypeID = model.Properties.ContractTypeID,
                    ContractName = model.Properties.ContractName,
                    ContractNumber = string.IsNullOrEmpty(model.Properties.ContractNumber) ? CreateContractNumber() : model.Properties.ContractNumber,
                    ContractVersion = (byte)model.Properties.ContractVersion,
                    ContractReference = model.Properties.ContractReferenceNumber,
                    ContractNotes = model.Notes.Notes,
                    IsActive = model.Properties.IsActive,
                    IsEnable = true,
                    IsPublish = model.IsPublish,
                    CreatedDate = model.CreatedDate,
                    CreatedBy = model.CreatedBy,
                    UpdatedDate = model.UpdatedDate,
                    UpdatedBy = model.UpdatedBy,
                    LocationId = model.LocationId
                };

                string contentValue = "";

                if (model.Content.ContentType.ToLower().Contains("file") && model.Content.FileContent != null)
                {
                    FileUpload upload = new FileUpload();
                    var result = upload.Upload(model.Content.FileContent, Globals.UploadDirectory);
                    if (result.Success)
                    {
                        contentValue = result.ServerPath;
                        model.Content.FileContentPath = result.ServerPath;
                    }
                }
                else
                {
                    contentValue = model.Content.EditorContent;
                }

                EF.ContractContent contractContent = new EF.ContractContent
                {
                    ContractID = contract.ContractID,
                    ContentValue = contentValue,
                    ContentType = model.Content.ContentType,
                    IsEnable = true
                };
                contract.ContractContent = contractContent;

                EF.ContractEmail contractEmail = new EF.ContractEmail
                {
                    ContractID = contract.ContractID,
                    SendEmailOnAcceptance = model.Email.SendEmailOnContractAcceptance,
                    IncludeContractAsPDF = model.Email.IncludeContractAsPDF,
                    EmailMessageID = model.Email.EmailMessageID,
                    IsEnable = true
                };
                contract.ContractEmail = contractEmail;

                EF.ContractSignature contractSignature = new EF.ContractSignature
                {
                    ContractID = contract.ContractID,
                    RequireElectronicSignature = model.Signature.RequireElectronicSignature,
                    SignatureText = model.Signature.SignatureText,
                    IsEnable = true
                };
                contract.ContractSignature = contractSignature;

                List<EF.ContractAssertion> contractAssertions = new List<ContractAssertion>();

                if (model.Assertions != null)
                {
                    foreach (var assertion in model.Assertions)
                    {
                        EF.ContractAssertion a = new ContractAssertion
                        {
                            ContractID = contract.ContractID,
                            ShowCheckBox = assertion.ShowCheckBox,
                            AssertionRequired = assertion.AssertionRequired,
                            AssertionText = assertion.AssertionText,
                            AssertionHyperlinkText = assertion.AssertionHyperlinkText,
                            AssertionHyperlinkUrl = assertion.AssertionHyperlinkUrl,
                            IsEnable = true
                        };

                        contractAssertions.Add(a);
                    }
                }

                contract.ContractAssertions = contractAssertions;


                uow.GenericRepository<Contract>().Insert(contract);
                uow.SaveChanges();
                uow.Commit();

                model.ContractID = contract.ContractID;

                RefreshContractTemplateCache(contract.ContractID);

            }
            catch (Exception ex)
            {
                uow.Rollback();
                throw ex;
            }


            return model;
        }

        public AddContractVM UpdateContract(AddContractVM model)
        {
            var contract = GetContractById(model.ContractID);

            if (contract != null)
            {
                contract.ContractTypeID = model.Properties.ContractTypeID;
                contract.ContractName = model.Properties.ContractName;
                contract.ContractNumber = model.Properties.ContractNumber;
                contract.ContractVersion = (byte)model.Properties.ContractVersion;
                contract.ContractReference = model.Properties.ContractReferenceNumber;
                contract.ContractNotes = model.Notes.Notes;
                contract.IsActive = model.Properties.IsActive;
                contract.IsPublish = model.IsPublish;
                contract.UpdatedDate = model.UpdatedDate;
                contract.UpdatedBy = model.UpdatedBy;
                contract.LocationId = model.LocationId;


                //content
                string contentValue = "";

                if (model.Content.ContentType.ToLower().Contains("file") && model.Content.FileContent != null)
                {
                    FileUpload upload = new FileUpload();
                    var result = upload.Upload(model.Content.FileContent, Globals.UploadDirectory);
                    if (result.Success)
                    {
                        contentValue = result.ServerPath;
                        model.Content.FileContentPath = result.ServerPath;
                    }
                }
                else
                {
                    contentValue = model.Content.EditorContent;
                }

                if (!string.IsNullOrEmpty(contentValue))
                {
                    contract.ContractContent.ContentValue = contentValue;
                    contract.ContractContent.ContentType = model.Content.ContentType;
                }


                //email
                contract.ContractEmail.SendEmailOnAcceptance = model.Email.SendEmailOnContractAcceptance;
                contract.ContractEmail.IncludeContractAsPDF = model.Email.IncludeContractAsPDF;
                contract.ContractEmail.EmailMessageID = model.Email.EmailMessageID;


                //signature
                contract.ContractSignature.RequireElectronicSignature = model.Signature.RequireElectronicSignature;
                contract.ContractSignature.SignatureText = model.Signature.SignatureText;


                //assertions

                var removeAssertions = uow.GenericRepository<EF.ContractAssertion>().Table.Where(x => x.ContractID == contract.ContractID);

                if (model.Assertions != null)
                {
                    foreach (var assertion in model.Assertions)
                    {
                        if (assertion.ContractAssertionID > 0)
                        {
                            removeAssertions = removeAssertions.Where(x => x.ContractAssertionID != assertion.ContractAssertionID);

                            var asrt = contract.ContractAssertions.FirstOrDefault(x => x.IsEnable == true && x.ContractAssertionID == assertion.ContractAssertionID);
                            if (asrt != null)
                            {
                                asrt.ShowCheckBox = assertion.ShowCheckBox;
                                asrt.AssertionRequired = assertion.AssertionRequired;
                                asrt.AssertionText = assertion.AssertionText;
                                asrt.AssertionHyperlinkText = assertion.AssertionHyperlinkText;
                                asrt.AssertionHyperlinkUrl = assertion.AssertionHyperlinkUrl;

                                continue;
                            }
                        }

                        EF.ContractAssertion a = new ContractAssertion
                        {
                            ContractID = contract.ContractID,
                            ShowCheckBox = assertion.ShowCheckBox,
                            AssertionRequired = assertion.AssertionRequired,
                            AssertionText = assertion.AssertionText,
                            AssertionHyperlinkText = assertion.AssertionHyperlinkText,
                            AssertionHyperlinkUrl = assertion.AssertionHyperlinkUrl,
                            IsEnable = true
                        };

                        contract.ContractAssertions.Add(a);

                    }
                }
                foreach (var sAser in removeAssertions)
                {
                    uow.GenericRepository<ContractAssertion>().Delete(sAser);
                }


                uow.GenericRepository<Contract>().Update(contract);
                uow.SaveChanges();

                // After updating contract content, refresh the cache so generators see latest content
                RefreshContractTemplateCache(contract.ContractID);

            }
            else
            {
                throw new Exception("Contract not found to update.");
            }

            return model;
        }

        public bool DeleteContract(int id)
        {
            EF.Contract contract = uow.GenericRepository<EF.Contract>().GetById(id);

            if (contract != null)
            {
                contract.IsEnable = false;

                uow.GenericRepository<EF.Contract>().Update(contract);
                uow.SaveChanges();

                return true;
            }
            else
            {
                throw new Exception("Contract not found to delete.");
            }
        }


        private bool ContractVersionExists(string contractNumber, int version)
        {
            bool exist = false;

            if (uow.GenericRepository<Contract>().Table.Any(x => x.IsEnable == true && x.ContractNumber == contractNumber && x.ContractVersion == version))
                exist = true;

            return exist;
        }

        #region Contract Generation
        public bool AddStudentContract(StudentConractsVM studentConractsVM, string SendEmailTo, int SenderId, out string message)
        {
            try
            {
                // Validate signature requirement
                var contract = GetContractById(studentConractsVM.ContractId);
                if (contract?.ContractSignature?.RequireElectronicSignature == true)
                {
                    // Ensure signature is provided when required
                    if (string.IsNullOrWhiteSpace(studentConractsVM.Signature))
                    {
                        message = "Electronic signature is required for this contract.";
                        return false;
                    }
                }

                var studentContract = uow.GenericRepository<StudentContract>().Table
                    .Where(x => x.PlacementId == studentConractsVM.PlacementId)
                    .OrderByDescending(x => x.CreatedOn)
                    .FirstOrDefault();

                if (studentContract != null)
                {
                    // Update existing contract with optimized data handling
                    UpdateExistingContractOptimized(studentContract, studentConractsVM);
                    message = "Contract updated successfully!";

                    // Log audit trail
                    LogContractAuditOptimized(studentContract, Enumeration.AuditType.Update);
                }
                else
                {
                    // Create new contract with optimized structure
                    studentContract = CreateNewContractOptimized(studentConractsVM);
                    uow.GenericRepository<StudentContract>().Insert(studentContract);
                    message = "Contract created successfully";

                    // Log audit trail
                    LogContractAuditOptimized(studentContract, Enumeration.AuditType.Create);
                }

                // Send notification
                var Description = "Your Contract has been generated against contract name: " + studentConractsVM.ContractName;
                notificationService.SendNotification(null, studentConractsVM.PersonId, "Student", "New Contract", Description, "/Student/ContractsManage/StudentContracts", PMS.Common.Globals.User.Email);

                // Save core data quickly
                uow.SaveChanges();

                // Fire and forget: generate and persist PDF URL after response (capture base URL safely)
                var req = HttpContext.Current != null ? HttpContext.Current.Request : null;
                var baseUrl = req != null ? req.Url.GetLeftPart(UriPartial.Authority) + req.ApplicationPath : string.Empty;
                Task.Run(() =>
                {
                    try
                    {
                        UpdateContractUrlOptimizedWithBase(studentContract.id, baseUrl);
                        // After URL is persisted, send email with the persisted URL
                        SendContractEmailWithSignatureBase(studentContract.id, SendEmailTo, studentConractsVM.LocationID, baseUrl);
                    }
                    catch { /* swallow background errors */ }
                });

                // Do not send email synchronously

                return true;
            }
            catch (Exception ex)
            {
                uow.Rollback();
                message = "Internal Server Error: " + ex.Message;
                return false;
            }
        }

        private void UpdateExistingContractOptimized(StudentContract studentContract, StudentConractsVM studentConractsVM)
        {
            // Store old values for audit comparison
            var oldContract = uow.GenericRepository<StudentContract>()
                                 .Table
                                 .AsNoTracking()
                                 .FirstOrDefault(x => x.id == studentContract.id);

            // Update contract properties efficiently
            studentContract.BookingId = studentConractsVM.BookingId;
            studentContract.ContractContent = studentConractsVM.Content;
            studentContract.ContractId = studentConractsVM.ContractId;
            studentContract.GrossAmount = studentConractsVM.GrossAmount;
            studentContract.DiscountAmount = studentConractsVM.DiscountAmount;
            studentContract.NetAmount = studentConractsVM.NetAmount;
            studentContract.TaxAmount = studentConractsVM.TaxAmount;
            studentContract.PersonId = studentConractsVM.PersonId;
            studentContract.RegistrationFee = studentConractsVM.RegistrationFee;
            studentContract.SecurityDeposit = studentConractsVM.SecurityDeposit;
            studentContract.PersonCode = studentConractsVM.PersonCode;
            studentContract.PersonFullName = studentConractsVM.PersonFullName;
            studentContract.ContractName = studentConractsVM.ContractName;
            studentContract.IsSigned = false; // Reset signature status for new version
            studentContract.updatedBy = Globals.User.ID;
            studentContract.UpdatedOn = DateTime.Now;

            // Handle signature if provided
            if (!string.IsNullOrWhiteSpace(studentConractsVM.Signature))
            {
                studentContract.StudentSignature = studentConractsVM.Signature;
                studentContract.IsSigned = true;
                studentContract.SignedBy = "By User Online";
            }

            uow.GenericRepository<StudentContract>().Update(studentContract);
        }

        private StudentContract CreateNewContractOptimized(StudentConractsVM studentConractsVM)
        {
            var contract = new StudentContract()
            {
                BookingId = studentConractsVM.BookingId,
                ContractContent = studentConractsVM.Content,
                ContractId = studentConractsVM.ContractId,
                PlacementId = studentConractsVM.PlacementId,
                GrossAmount = studentConractsVM.GrossAmount,
                DiscountAmount = studentConractsVM.DiscountAmount,
                NetAmount = studentConractsVM.NetAmount,
                TaxAmount = studentConractsVM.TaxAmount,
                PersonId = studentConractsVM.PersonId,
                RegistrationFee = studentConractsVM.RegistrationFee,
                SecurityDeposit = studentConractsVM.SecurityDeposit,
                PersonCode = studentConractsVM.PersonCode,
                PersonFullName = studentConractsVM.PersonFullName,
                ContractName = studentConractsVM.ContractName,
                ContractKey = Guid.NewGuid().ToString(),
                CreatedBy = Globals.User.ID,
                CreatedOn = DateTime.Now,
                IsSigned = false
            };

            // Handle signature if provided during creation
            if (!string.IsNullOrWhiteSpace(studentConractsVM.Signature))
            {
                contract.StudentSignature = studentConractsVM.Signature;
                contract.IsSigned = true;
                contract.SignedBy = "By User Online";
            }

            return contract;
        }

        // Removed obsolete UpdateContractUrlOptimized (no longer used)

        // Background-friendly variant: re-fetch and compute URLs using provided baseUrl
        private void UpdateContractUrlOptimizedWithBase(int studentContractId, string baseUrl)
        {
            var updatecontracturl = uow.GenericRepository<EF.StudentContract>()
                        .Table
                        .Include(x => x.Person)
                        .FirstOrDefault(x => x.id == studentContractId);

            if (updatecontracturl != null)
            {
                var contcaturl = GetContractURL(
                    (string.IsNullOrEmpty(baseUrl) ? string.Empty : baseUrl) +
                    "/contractsmanage/ContractDownloadPdf/" + studentContractId + "?type=studentcontract",
                    updatecontracturl.PersonFullName);

                updatecontracturl.ContractUrl = (string.IsNullOrEmpty(baseUrl) ? string.Empty : baseUrl) + contcaturl;
                uow.GenericRepository<EF.StudentContract>().Update(updatecontracturl);
                uow.SaveChanges();
            }
        }

        // Removed obsolete SendContractEmailWithSignature (no longer used)

        // Background-friendly email sender using baseUrl and studentContractId
        private void SendContractEmailWithSignatureBase(int studentContractId, string SendEmailTo, int? locationId, string baseUrl)
        {
            var sc = uow.GenericRepository<EF.StudentContract>()
                         .Table
                         .Include(x => x.Person)
                         .FirstOrDefault(x => x.id == studentContractId);
            if (sc == null) return;

            var req = HttpContext.Current != null ? HttpContext.Current.Request : null;
            var fallbackBase = req != null ? req.Url.GetLeftPart(UriPartial.Authority) + req.ApplicationPath : baseUrl;
            var personLocationId = sc.Person?.LocationId ?? locationId;
            var locationSetting = uow.GenericRepository<EF.LocationSetting>()
                                     .Table
                                     .Where(x => x.LocationId == personLocationId)
                                     .FirstOrDefault();

            var emailAction = locationSetting?.PreCheckinDocumentationIsActive == true
                ? Enumeration.CorrespondenceAction.SendContractForDubai
                : Enumeration.CorrespondenceAction.SendContract;

            var emailTemplate = correspondenceService.GetEmailMessagesByActionId((int)emailAction, locationId ?? 0);
            if (emailTemplate == null) return;

            var body = emailTemplate.EmailMessageBody;
            var confirmationLink = (string.IsNullOrEmpty(fallbackBase) ? string.Empty : fallbackBase) +
                    "/Account/Accept_Contract_Details/" + sc.id + "?contractkey=" + sc.ContractKey;
            body = body.Replace("{{ConfirmationLink}}", confirmationLink);

            var persistedContractUrl = sc.ContractUrl;
            var dynamicDownloadUrl = (string.IsNullOrEmpty(fallbackBase) ? string.Empty : fallbackBase) +
                    "/contractsmanage/ContractDownloadPdf/" + sc.id + "?type=studentcontract";
            var contractLinkForEmail = !string.IsNullOrWhiteSpace(persistedContractUrl) ? persistedContractUrl : dynamicDownloadUrl;

            body = body.Replace("[[DownloadContractLink]]", contractLinkForEmail)
                       .Replace("{{DownloadContractLink}}", contractLinkForEmail)
                       .Replace("[[ContractUrl]]", contractLinkForEmail)
                       .Replace("{{ContractUrl}}", contractLinkForEmail);

            emailService.SendEmail(
                Convert.ToString(emailTemplate.EmailMessageSubject),
                body,
                true,
                SendEmailTo,
                emailTemplate.EmailMessageSenderID);
        }

        private void LogContractAuditOptimized(StudentContract studentContract, Enumeration.AuditType auditType)
        {
            EF.AuditLog auditLog = new EF.AuditLog()
            {
                AuditType = (int)auditType,
                ActionId = (int)Enumeration.CorrespondenceAction.SendContract,
                PK = studentContract.id.ToString(),
                UserId = Common.Globals.User.ID,
                TableName = "Generated Contract",
                Reference = studentContract.PersonCode,
                UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                PersonId = studentContract.PersonId
            };
            auditLogsService.AddAuditLog(auditLog);
        }

        public bool SignContractDocument(int id, string Signature, string signatureby)
        {
            try
            {
                // Validate signature requirement
                var studentContract = uow.GenericRepository<EF.StudentContract>()
                    .Table
                    .Include(x => x.Person)
                    .Include(x => x.Contract)
                    .Include(x => x.Contract.ContractSignature)
                    .FirstOrDefault(x => x.id == id);

                if (studentContract == null)
                {
                    return false;
                }

                // Check if signature is required but not provided
                if (studentContract.Contract?.ContractSignature?.RequireElectronicSignature == true &&
                    string.IsNullOrWhiteSpace(Signature))
                {
                    throw new Exception("Electronic signature is required for this contract.");
                }

                // Update contract with signature
                studentContract.StudentSignature = Signature;
                studentContract.IsSigned = true;
                studentContract.SignedBy = signatureby;
                studentContract.UpdatedOn = DateTime.Now;

                // Update contract content with signature placeholders
                if (!string.IsNullOrEmpty(Signature))
                {
                    studentContract.ContractContent = studentContract.ContractContent
                        .Replace("[[StudentSignature]]", studentContract.Person.FullName);

                    var currentDate = DateTime.Now.ToString("dd/MM/yyyy h:mm tt");
                    studentContract.ContractContent = studentContract.ContractContent
                        .Replace("[[StudentSignatureDate]]", currentDate);
                }

                uow.GenericRepository<EF.StudentContract>().Update(studentContract);
                uow.SaveChanges();

                // Log audit trail
                if (Common.Globals.User != null)
                {
                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Create,
                        ActionId = (int)Enumeration.CorrespondenceAction.SignContract,
                        PK = id.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Sign Contract",
                        Reference = signatureby,
                        PersonId = studentContract.PersonId,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }

                // Send confirmation email with signature status
                SendContractSignedEmail(studentContract);

                return true;
            }
            catch (Exception ex)
            {
                // Log error but don't throw to maintain user experience
                System.Diagnostics.Debug.WriteLine($"Contract signing error: {ex.Message}");
                return false;
            }
        }

        private void SendContractSignedEmail(StudentContract studentContract)
        {
            try
            {
                var PersonLocationId = studentContract.Person.LocationId;
                var locationSetting = uow.GenericRepository<EF.LocationSetting>()
                    .Table
                    .Where(x => x.LocationId == PersonLocationId)
                    .FirstOrDefault();

                var emailAction = locationSetting?.PreCheckinDocumentationIsActive == true
                    ? Enumeration.CorrespondenceAction.SignContractForDubai
                    : Enumeration.CorrespondenceAction.SignContract;

                var EmailNotification = correspondenceService.GetEmailMessagesByActionId((int)emailAction, studentContract.Person.LocationId ?? 0);

                if (EmailNotification != null && EmailNotification.IsActive == true)
                {
                    var Request = HttpContext.Current.Request;
                    var body = EmailNotification.EmailMessageBody;

                    // Replace standard placeholders
                    body = body.Replace("[[PersonID]]", studentContract.Person.Code);
                    body = body.Replace("[[PersonFull_Name]]", studentContract.Person.FullName);
                    body = body.Replace("[[PersonUniversity]]", studentContract.Person.Universiry);
                    body = body.Replace("[[DownloadContractLink]]",
                        Request.Url.GetLeftPart(UriPartial.Authority) + Request.ApplicationPath +
                        "/contractsmanage/ContractDownloadPdf/" + studentContract.id + "?type=studentcontract");

                    // Do not append signature confirmation lines to the template body

                    // Handle Dubai-specific flow
                    if (locationSetting?.PreCheckinDocumentationIsActive == true)
                    {
                        var precheckindocumenturl = uow.GenericRepository<EF.PreCheckInDocumentation>()
                            .Table
                            .Where(x => x.PlacementId == studentContract.PlacementId)
                            .FirstOrDefault();

                        if (precheckindocumenturl != null)
                        {
                            body = body.Replace("{{ConfirmationLink}}",
                                Request.Url.GetLeftPart(UriPartial.Authority) + Request.ApplicationPath +
                                "/Account/Accept_documentation_Details/" + precheckindocumenturl.id +
                                "?documentationkey=" + precheckindocumenturl.DocumentationKey);
                        }
                    }

                    emailService.SendEmailAsync(
                        EmailNotification.EmailMessageSubject,
                        body,
                        true,
                        studentContract.Person.Email,
                        EmailNotification.EmailMessageSenderID);
                }
            }
            catch (Exception ex)
            {
                // Log email error but don't fail the signing process
                System.Diagnostics.Debug.WriteLine($"Email sending failed for contract signing: {ex.Message}");
            }
        }

        public bool SignPrecheckinDocument(int id, string StudentSignature, string signatureby)
        {
            var updateprecheckin = uow.GenericRepository<EF.PreCheckInDocumentation>().Table.Where(x => x.id == id).FirstOrDefault();
            if (updateprecheckin != null)
            {
                updateprecheckin.StudentSignature = StudentSignature;

                DateTime nowDateTime = DateTime.Now;
                var Currentdate = String.Format("{0:dd/MM/yyyy h:mm tt}", nowDateTime);

                updateprecheckin.IsSigned = true;
                updateprecheckin.SignedBy = signatureby;
                updateprecheckin.UpdatedOn = DateTime.Now;
                uow.GenericRepository<EF.PreCheckInDocumentation>().Update(updateprecheckin);
                uow.SaveChanges();
                //Insert Audit Log
                if (Common.Globals.User != null)
                {

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Create,
                        ActionId = (int)Enumeration.CorrespondenceAction.SignDocumentation,
                        PK = id.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Sign Documentation",
                        Reference = signatureby,
                        PersonId = updateprecheckin.PersonId,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                var EmailNotification = correspondenceService.GetEmailMessagesByActionId(Convert.ToInt32(((int)Common.Classes.Enumeration.CorrespondenceAction.SignDocumentation)), updateprecheckin.Person.LocationId ?? 0);
                if (EmailNotification != null && EmailNotification.IsActive == true)
                {
                    var Request = HttpContext.Current.Request;
                    var body = EmailNotification.EmailMessageBody;
                    body = body.Replace("[[PersonFull_Name]]", updateprecheckin.PersonFullName);
                    body = body.Replace("[[PersonID]]", updateprecheckin.PersonCode);

                    body = body.Replace("[[DownloadDocumentationLink]]", Request.Url.GetLeftPart(UriPartial.Authority) + Request.ApplicationPath + "/contractsmanage/PreCheckInDownloadPdf/" + updateprecheckin.id);

                    var senderid = EmailNotification.EmailMessageSenderID;
                    emailService.SendEmailAsync(EmailNotification.EmailMessageSubject, body, true, updateprecheckin.Person.Email, senderid);

                }

                return true;
            }

            return false;
        }

        public bool MarkContractAsSigned(int id)
        {
            EF.StudentContract studentContract = GetStudentContractbyID(id);

            if (studentContract == null)
            {
                throw new Exception("Contract not found.");
            }

            if (studentContract.IsCancel)
            {
                throw new Exception("Cancelled contract cannot be marked as signed.");
            }

            if (studentContract.IsSigned)
            {
                return true;
            }

            EF.StudentContract oldStudentContract = new EF.StudentContract
            {
                id = studentContract.id,
                ContractId = studentContract.ContractId,
                ContractName = studentContract.ContractName,
                IsSigned = studentContract.IsSigned,
                PersonId = studentContract.PersonId,
                UpdatedOn = studentContract.UpdatedOn
            };

            studentContract.IsSigned = true;
            studentContract.UpdatedOn = DateTime.Now;
            studentContract.updatedBy = Globals.User.ID;

            uow.GenericRepository<EF.StudentContract>().Update(studentContract);
            uow.SaveChanges();

            var difference = Common.Classes.Common.DetailedCompare<EF.StudentContract>(oldStudentContract, studentContract);
            EF.AuditLog auditLog = new EF.AuditLog()
            {
                AuditType = (int)Enumeration.AuditType.Update,
                ActionId = (int)Enumeration.CorrespondenceAction.SignContract,
                PK = studentContract.id.ToString(),
                UserId = Common.Globals.User.ID,
                TableName = "StudentContracts",
                Reference = studentContract.ContractName,
                UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                PersonId = studentContract.PersonId,
                AuditLogDetails = difference
            };
            auditLogsService.AddAuditLog(auditLog);

            return true;
        }
        #endregion


        public List<StudentConractsListVM> GetStudentContracts(DateTime? FromDate, DateTime? ToDate)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();


            var contracts = uow.GenericRepository<StudentContract>().Table
                .Where(x => assignedLocationIds.Contains((int)x.Person.LocationId))
                .Join(
                    uow.GenericRepository<EF.Booking>().Table,
                    contract => contract.BookingId,
                    booking => booking.BookingID,
                    (contract, booking) => new { contract, booking }
                )
                .Join(
                    uow.GenericRepository<V_PlacementList>().Table
                        .Where(p => assignedLocationIds.Contains(p.LocationID ?? 0)), // Add placement location filter
                    cb => cb.contract.PlacementId,
                    placement => placement.BedSpacePlacementID,
                    (cb, placement) => new StudentConractsListVM
                    {
                        Id = cb.contract.id,
                        BookingId = cb.contract.BookingId,
                        Content = cb.contract.ContractContent,
                        ContractId = cb.contract.ContractId,
                        PlacementId = cb.contract.PlacementId,
                        PersonId = cb.contract.PersonId,
                        PersonFullName = cb.contract.PersonFullName,
                        PersonCode = cb.contract.PersonCode,
                        ContractName = cb.contract.ContractName,
                        Occupancy = placement.Commitment,
                        CreatedBy = cb.contract.UserMaster.FullName,
                        CreatedOn = cb.contract.CreatedOn,
                        UpdatedOn = cb.contract.UpdatedOn,
                        IsSigned = cb.contract.IsSigned,
                        Movein = placement.MoveIn,
                        MoveOut = placement.MoveOut,
                        CheckIn = placement.CheckIn,
                        CheckOut = placement.CheckOut,
                        InHouse = placement.CheckIn.HasValue && !placement.CheckOut.HasValue,
                        CheckedOut = placement.CheckIn.HasValue && placement.CheckOut.HasValue,
                        IsCancel = cb.contract.IsCancel,
                        CancellationReason = cb.contract.CancellationReason
                    }
                )
                .Where(x =>
                    (
                        (EntityFunctions.TruncateTime(x.CreatedOn) >= FromDate || FromDate == null) &&
                        (EntityFunctions.TruncateTime(x.CreatedOn) <= ToDate || ToDate == null)
                    ) ||
                    (
                        (EntityFunctions.TruncateTime(x.UpdatedOn) >= FromDate || FromDate == null) &&
                        (EntityFunctions.TruncateTime(x.UpdatedOn) <= ToDate || ToDate == null)
                    )
                )
                .OrderByDescending(x => x.CreatedOn)
                .ToList();

            return contracts;
        }
        public EF.StudentContract GetStudentContractbyID(int id)
        {
            return uow.GenericRepository<EF.StudentContract>().GetById(id);
        }
        public bool CancelContract(int id, string cancellationReason)
        {
            EF.StudentContract studentContract = GetStudentContractbyID(id);

            if (studentContract == null)
            {
                throw new Exception("Contract not found to cancel.");
            }

            EF.StudentContract oldStudentContract = new EF.StudentContract
            {
                ContractId = studentContract.ContractId,
                ContractName = studentContract.ContractName,
                IsCancel = studentContract.IsCancel,
                PersonId = studentContract.PersonId,
                CancellationReason = studentContract.CancellationReason
            };

            // Update the contract
            studentContract.IsCancel = true;
            studentContract.CancellationReason = cancellationReason;
            studentContract.UpdatedOn = DateTime.Now;
            studentContract.updatedBy = Globals.User.ID;

            uow.GenericRepository<EF.StudentContract>().Update(studentContract);
            uow.SaveChanges();

            // Insert Audit Log
            var difference = Common.Classes.Common.DetailedCompare<EF.StudentContract>(oldStudentContract, studentContract);
            List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

            EF.AuditLog auditLog = new EF.AuditLog()
            {
                AuditType = (int)Enumeration.AuditType.Delete,
                ActionId = (int)Enumeration.CorrespondenceAction.CancelContract,
                PK = studentContract.ContractId.ToString(),
                UserId = Common.Globals.User.ID,
                TableName = "StudentContracts",
                Reference = studentContract.ContractName.ToString(),
                UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                PersonId = studentContract.PersonId,
                AuditLogDetails = difference
            };
            auditLogsService.AddAuditLog(auditLog);


            var EmailNotification = correspondenceService.GetEmailMessagesByActionId(Convert.ToInt32(((int)Common.Classes.Enumeration.CorrespondenceAction.CancelContract)), studentContract.Person.LocationId ?? 0);
            if (EmailNotification != null && EmailNotification.IsActive == true)
            {
                try
                {
                    var Request = HttpContext.Current.Request;
                    var body = EmailNotification.EmailMessageBody;

                    // Essential placeholder replacements
                    body = body.Replace("[[ContractName]]", studentContract.ContractName ?? "");
                    body = body.Replace("[[FullName]]", studentContract.PersonFullName ?? "");
                    body = body.Replace("[[PersonCode]]", studentContract.Person.Code ?? "");
                    body = body.Replace("[[CancellationDate]]", studentContract.UpdatedOn?.ToString("dd/MMM/yyyy") ?? DateTime.Now.ToString("dd/MMM/yyyy"));
                    body = body.Replace("[[CancellationReason]]", studentContract.CancellationReason ?? "");
                    //body = body.Replace("[[CancelledBy]]", Common.Globals.User.Name ?? "");

                    var senderid = EmailNotification.EmailMessageSenderID;
                    emailService.SendEmailAsync(EmailNotification.EmailMessageSubject, body, true, studentContract.Person.Email, senderid);
                }
                catch (Exception ex)
                {
                    // Log the email error but don't fail the contract cancellation
                    System.Diagnostics.Debug.WriteLine($"Email sending failed for contract cancellation: {ex.Message}");
                }
            }

            return true;
        }

        public List<StudentConractsListVM> GetSingleStudentContract(int personId)
        {

            return uow.GenericRepository<StudentContract>().Table.Where(x => x.PersonId == personId).ToList()
                .Select(x => new StudentConractsListVM
                {
                    Id = x.id,
                    BookingId = x.BookingId,
                    ContractId = x.ContractId,
                    PlacementId = x.PlacementId,
                    PersonId = x.PersonId,
                    ContractKey = x.ContractKey,
                    PersonFullName = x.PersonFullName,
                    PersonCode = x.PersonCode,
                    ContractName = x.ContractName,
                    CreatedBy = x.UserMaster.FullName,
                    CreatedOn = x.CreatedOn,
                    IsSigned = x.IsSigned,
                    ContractUrl = x.ContractUrl,
                    IsCancel = x.IsCancel,
                    CancellationReason = x.CancellationReason
                }).ToList();
        }

        public StudentConractsListVM GetSingleStudentContractLatest(int personId)
        {
            return uow
                .GenericRepository<StudentContract>()
                .Table
                .Where(x => x.PersonId == personId)
                .OrderByDescending(o => o.CreatedOn)
                .Select(x => new StudentConractsListVM
                {
                    Id = x.id,
                    BookingId = x.BookingId,
                    ContractId = x.ContractId,
                    PlacementId = x.PlacementId,
                    PersonId = x.PersonId,
                    ContractKey = x.ContractKey,
                    PersonFullName = x.PersonFullName,
                    PersonCode = x.PersonCode,
                    ContractName = x.ContractName,
                    CreatedBy = x.UserMaster.FullName,
                    CreatedOn = x.CreatedOn,
                    IsSigned = x.IsSigned,
                    ContractUrl = x.ContractUrl,
                    IsCancel = x.IsCancel,
                    CancellationReason = x.CancellationReason
                })
                .FirstOrDefault();
        }

        public StudentConractsVM GetStudentContractById(int id)
        {
            return uow.GenericRepository<StudentContract>().Table.Where(x => x.id == id)
                .Select(x => new StudentConractsVM
                {
                    BookingId = x.BookingId,
                    Content = x.ContractContent,
                    ContractId = x.ContractId,
                    PlacementId = x.PlacementId,
                    GrossAmount = x.GrossAmount,
                    DiscountAmount = x.DiscountAmount,
                    NetAmount = x.NetAmount,
                    TaxAmount = x.TaxAmount,
                    PersonId = x.PersonId,
                    Signature = x.StudentSignature,
                    RegistrationFee = x.RegistrationFee,
                    SecurityDeposit = x.SecurityDeposit,
                    ContractName = x.ContractName,
                    PersonFullName = x.PersonFullName,
                    PersonCode = x.PersonCode,
                    ContractUrl = x.ContractUrl,
                    IsSigned = x.IsSigned,
                    contractkery = x.ContractKey,
                    IsCancel = x.IsCancel,
                    CancellationReason = x.CancellationReason

                }).FirstOrDefault();
        }

        public static string GetContractURL(string url, string ContractName)
        {
            try
            {
                string cleanContractName = ContractName.Trim() // Trim leading/trailing spaces
            .Replace("\t", "")
            .Replace("/", "")
            .Replace("\\", "")
            .Replace(":", "")
            .Replace("*", "")
            .Replace("?", "")
            .Replace("\"", "")
            .Replace("<", "")
            .Replace(">", "")
            .Replace("|", "");
                string path = "StudentContracts";
                string NewFileName = "Contract-" + cleanContractName + "-" + "" + "-" + DateTime.Now.ToString("MMddyyHHmmss") + ".pdf";
                string folderexist = System.Web.Hosting.HostingEnvironment.MapPath("~/Upload/Files/" + path + "/");
                if (string.IsNullOrEmpty(folderexist))
                {
                    folderexist = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Upload", "Files", path);
                }
                bool isExist = System.IO.Directory.Exists(folderexist);
                if (!isExist)
                {
                    System.IO.Directory.CreateDirectory(folderexist);
                }
                string myLocalFilePath = folderexist + NewFileName;


                //var response = ReportServerPath + "PaycheckReport%2fPayCheckMainReport&PayrollId=" + PayrollId + "&&EmployeeId=" + EmployeeId;
                var theURL = url;

                WebClient Client = new WebClient();
                Client.UseDefaultCredentials = true;

                Client.DownloadFile(theURL, myLocalFilePath);
                var ExternalPath = "/Upload/Files/StudentContracts/" + NewFileName;


                return ExternalPath;


            }
            catch (Exception ex)
            {

                throw;

            }
        }

        private string CreateContractNumber()
        {
            string contractNumber = "SHO-0001";

            var contract = uow.GenericRepository<Contract>().Table.OrderByDescending(x => x.ContractNumber).FirstOrDefault();
            if (contract != null)
            {
                contractNumber = "SHO-" + (Convert.ToInt32(contract.ContractNumber.Replace("SHO-", "")) + 1).ToString("0000");
            }

            return contractNumber;
        }

        public List<SelectListVM> GetEmailMessagesDDList()
        {
            return correspondenceService.GetEmailMessagesDDList();
        }

        public StudentConractsVM GetContractDetailByPlacment(int PlacementId)
        {
            try
            {
                var placement = placementService.GetBedSpacePlacementById(PlacementId);
                var studentcontract = uow.GenericRepository<StudentContract>().Table.Where(x => x.PlacementId == PlacementId).OrderByDescending(x => x.CreatedOn).FirstOrDefault();
                StudentConractsVM model = new StudentConractsVM();
                var ContractID = 0;
                bool IsPublish = false;
                if (studentcontract != null)
                {
                    ContractID = studentcontract.ContractId;
                    IsPublish = studentcontract.Contract.IsPublish;
                }
                if (placement != null)
                {
                    var booking = uow.GenericRepository<EF.Booking>().Table.FirstOrDefault(x => x.BookingID == placement.BookingID);
                    model.ContractId = ContractID;
                    model.IsPublish = IsPublish;
                    model.GrossAmount = booking.PriceConfig.Price;
                    model.NetAmount = booking.PriceConfig.Price;
                    model.RegistrationFee = booking.PriceConfig.InitialDeposit;
                    model.SecurityDeposit = booking.PriceConfig.InitialDeposit;

                }
                return model;
            }
            catch (Exception ex)
            {
                throw null;
            }
        }

        public bool AddPreCheckInDocumentation(PreCheckInDocumentationVM preCheckInDocumentationVM, out string message)
        {
            try
            {
                var preCheckInDoc = uow.GenericRepository<PreCheckInDocumentation>()
                    .Table
                    .Where(x => x.BookingId == preCheckInDocumentationVM.BookingId
                             && x.PlacementId == preCheckInDocumentationVM.PlacementId
                             && x.PersonId == preCheckInDocumentationVM.PersonId)
                    .OrderByDescending(x => x.CreatedOn)
                    .FirstOrDefault();

                if (preCheckInDoc != null)
                {
                    // Update existing documentation
                    UpdateExistingDocumentation(preCheckInDoc, preCheckInDocumentationVM);
                    message = "Contract updated successfully!";
                }
                else
                {
                    // Create new documentation
                    preCheckInDoc = CreateNewDocumentation(preCheckInDocumentationVM);
                    uow.GenericRepository<PreCheckInDocumentation>().Insert(preCheckInDoc);
                    message = "Contract and Documentation created successfully!";
                }

                uow.SaveChanges();

                // Background: Update documentation URL without blocking
                Task.Run(() =>
                {
                    try { UpdateDocumentationUrl(preCheckInDoc); } catch { }
                });

                return true;
            }
            catch (Exception ex)
            {
                uow.Rollback();
                message = "Internal Server Error!";
                return false;
            }
        }

        private void UpdateExistingDocumentation(PreCheckInDocumentation preCheckInDoc, PreCheckInDocumentationVM preCheckInDocumentationVM)
        {
            var oldDoc = uow.GenericRepository<PreCheckInDocumentation>().GetByIdAsNoTracking(x => x.id == preCheckInDoc.id);

            preCheckInDoc.PersonCode = preCheckInDocumentationVM.PersonCode;
            preCheckInDoc.PersonFullName = preCheckInDocumentationVM.PersonFullName;
            preCheckInDoc.DocumentationName = preCheckInDocumentationVM.DocumentationName;
            preCheckInDoc.DocumentationContent = preCheckInDocumentationVM.DocumentationContent;
            preCheckInDoc.DocumentationKey = preCheckInDocumentationVM.DocumentationKey;
            preCheckInDoc.IsSigned = preCheckInDocumentationVM.IsSigned;
            preCheckInDoc.UpdatedBy = preCheckInDocumentationVM.CreatedBy;
            preCheckInDoc.UpdatedOn = DateTime.Now;

            uow.GenericRepository<PreCheckInDocumentation>().Update(preCheckInDoc);

            // Log audit trail
            var difference = Common.Classes.Common.DetailedCompare<PreCheckInDocumentation>(oldDoc, preCheckInDoc);
            EF.AuditLog auditLog = new EF.AuditLog()
            {
                AuditType = (int)Enumeration.AuditType.Update,
                ActionId = (int)Enumeration.CorrespondenceAction.PreCheckInDocumentation,
                PK = preCheckInDoc.id.ToString(),
                UserId = Common.Globals.User.ID,
                TableName = "PreCheckInDocumentation",
                Reference = preCheckInDoc.PersonCode,
                UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                PersonId = preCheckInDoc.PersonId,
                AuditLogDetails = difference
            };
            auditLogsService.AddAuditLog(auditLog);
        }

        private PreCheckInDocumentation CreateNewDocumentation(PreCheckInDocumentationVM preCheckInDocumentationVM)
        {
            var preCheckInDoc = new PreCheckInDocumentation()
            {
                BookingId = preCheckInDocumentationVM.BookingId,
                PlacementId = preCheckInDocumentationVM.PlacementId,
                PersonId = preCheckInDocumentationVM.PersonId,
                PersonCode = preCheckInDocumentationVM.PersonCode,
                PersonFullName = preCheckInDocumentationVM.PersonFullName,
                DocumentationName = preCheckInDocumentationVM.DocumentationName,
                DocumentationContent = preCheckInDocumentationVM.DocumentationContent,
                DocumentationKey = preCheckInDocumentationVM.DocumentationKey,
                CreatedBy = preCheckInDocumentationVM.CreatedBy,
                CreatedOn = DateTime.Now,
                IsSigned = preCheckInDocumentationVM.IsSigned
            };

            // Log audit trail
            EF.AuditLog auditLog = new EF.AuditLog()
            {
                AuditType = (int)Enumeration.AuditType.Create,
                ActionId = (int)Enumeration.CorrespondenceAction.PreCheckInDocumentation,
                PK = preCheckInDoc.id.ToString(),
                UserId = Common.Globals.User.ID,
                TableName = "PreCheckInDocumentation",
                Reference = preCheckInDoc.PersonCode,
                UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                PersonId = preCheckInDoc.PersonId
            };
            auditLogsService.AddAuditLog(auditLog);

            return preCheckInDoc;
        }

        private void UpdateDocumentationUrl(PreCheckInDocumentation preCheckInDoc)
        {
            var req = HttpContext.Current != null ? HttpContext.Current.Request : null;
            var baseUrl = req != null ? req.Url.GetLeftPart(UriPartial.Authority) + req.ApplicationPath : string.Empty;
            var documentationUrl = GetDocumentationURL((string.IsNullOrEmpty(baseUrl) ? string.Empty : baseUrl) + "/contractsmanage/PreCheckInDownloadPdf/" + preCheckInDoc.id, preCheckInDoc.PersonFullName);
            preCheckInDoc.DocumentationUrl = (string.IsNullOrEmpty(baseUrl) ? string.Empty : baseUrl) + documentationUrl;
            uow.GenericRepository<PreCheckInDocumentation>().Update(preCheckInDoc);
            uow.SaveChanges();
        }

        public static string GetDocumentationURL(string url, string documentationName)
        {
            try
            {
                string path = "StudentPreCheckinDocumentation";
                string NewFileName = "PreCheckInDoc-" + documentationName + "-" + DateTime.Now.ToString("MMddyyHHmmss") + ".pdf";
                string folderexist = System.Web.Hosting.HostingEnvironment.MapPath("~/Upload/Files/" + path + "/");
                if (string.IsNullOrEmpty(folderexist))
                {
                    folderexist = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Upload", "Files", path);
                }
                bool isExist = System.IO.Directory.Exists(folderexist);
                if (!isExist)
                {
                    System.IO.Directory.CreateDirectory(folderexist);
                }
                string myLocalFilePath = folderexist + NewFileName;

                WebClient Client = new WebClient();
                Client.UseDefaultCredentials = true;
                Client.DownloadFile(url, myLocalFilePath);

                var ExternalPath = "/Upload/Files/" + path + "/" + NewFileName;
                return ExternalPath;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public PreCheckInDocumentationVM GetPreCheckInDocumentationById(int id)
        {
            var preCheckInDoc = uow.GenericRepository<PreCheckInDocumentation>().GetById(id);
            if (preCheckInDoc == null)
                return null;

            return new PreCheckInDocumentationVM
            {
                BookingId = preCheckInDoc.BookingId,
                PlacementId = preCheckInDoc.PlacementId,
                PersonId = preCheckInDoc.PersonId,
                PersonCode = preCheckInDoc.PersonCode,
                PersonFullName = preCheckInDoc.PersonFullName,
                DocumentationName = preCheckInDoc.DocumentationName,
                DocumentationContent = preCheckInDoc.DocumentationContent,
                DocumentationKey = preCheckInDoc.DocumentationKey,
                DocumentationUrl = preCheckInDoc.DocumentationUrl,
                StudentSignature = preCheckInDoc.StudentSignature,
                CreatedBy = preCheckInDoc.CreatedBy,
                CreatedOn = preCheckInDoc.CreatedOn,
                IsSigned = preCheckInDoc.IsSigned
            };
        }
        //api services
        public ApiResponse<List<StudentConractsListVM>> GetAllById(int Id)
        {
            var response = new ApiResponse<List<StudentConractsListVM>>();
            try
            {
                var StudentId = uow.GenericRepository<UserMaster>().Table.Where(x => x.ID == Id).Select(x => x.PersonID).FirstOrDefault();
                var data = GetSingleStudentContract(StudentId ?? 0);
                response.Code = (int)HttpStatusCode.OK;
                response.Success = true;
                response.Message = "success";
                response.Data = data;
                return response;
            }
            catch (Exception ex)
            {
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Success = false;
                response.Message = ex.Message;
                response.Data = null;
                return response;
            }
        }

        public List<StudentConractsListVM> GetUnsignedContracts()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            return uow.GenericRepository<StudentContract>().Table.Where(a => a.IsSigned == false && assignedLocationIds.Contains((int)a.Person.LocationId)).ToList()
                .Select(x => new StudentConractsListVM
                {
                    Id = x.id,
                    BookingId = x.BookingId,
                    Content = x.ContractContent,
                    ContractId = x.ContractId,
                    PlacementId = x.PlacementId,
                    PersonId = x.PersonId,
                    PersonFullName = x.PersonFullName,
                    PersonCode = x.PersonCode,
                    ContractName = x.ContractName,
                    CreatedBy = x.UserMaster.FullName,
                    CreatedOn = x.CreatedOn,
                    IsSigned = x.IsSigned,
                    LocationId = x.Person.LocationId,
                    IsCancel = x.IsCancel,
                    CancellationReason = x.CancellationReason
                }).ToList();
        }

        public List<string> GetUrl()
        {
            var url = uow.GenericRepository<EF.StudentContract>().Table.Select(x => x.ContractUrl).ToList();
            return url;
        }


    }
}
