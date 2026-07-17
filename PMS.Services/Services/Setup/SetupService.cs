using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using AutoMapper;
using PMS.Common;
using PMS.Common.Classes;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.DTO.ViewModels.SetupViewModels;
using PMS.EF;
using PMS.Repository.Repositories.Generic;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.AuditLogs;
using TheMyriad.DTO.DTO_Mapings;
using PMS.Services.Services.Email;
using PMS.DTO.ViewModels.TransportationViewModels;
using System.Web;
using PMS.Services.Services.Correspondence;
using System.Text.RegularExpressions;
using PMS.DTO.ViewModels.FeedbackViewModels;
using PMS.DTO.ViewModels.PersonManageViewModels;
using PMS.Services.Helpers;
using static PMS.Common.Classes.Enumeration;
using PMS.Services.Services.VoucherSystem;
using PMS.DTO.ViewModels;
using PMS.Services.Services.Invoicings;
using PMS.Services.Services.Payment;
using PMS.Services.Services.LocationContext;

namespace PMS.Services.Services.Setup
{
    public class SetupService : ISetupService
    {
        private GenericRepository<PMS.EF.Booking> bookingRepo;
        private readonly IAuditLogsService auditLogsService;
        private readonly IEmailService emailService;
        private GenericRepository<PMS.EF.Person> PersonRepo;
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly ICorrespondenceService correspondenceService;
        private readonly IVoucherService voucherService;
        private readonly IInvoicingService invoicingService;
        private readonly IPaymentService paymentService;
        private readonly ILocationContextService locationContextService;
        public SetupService(UnitOfWork<PMSEntities> _uow, IAuditLogsService _auditLogsService, IEmailService _emailService, ICorrespondenceService _correspondenceService,
            IVoucherService _voucherService, IInvoicingService _invoicingService, IPaymentService _paymentService, ILocationContextService _locationContextService)
        {
            auditLogsService = _auditLogsService;
            uow = _uow;
            emailService = _emailService;
            correspondenceService = _correspondenceService;
            voucherService = _voucherService;
            invoicingService = _invoicingService;
            bookingRepo = uow.GenericRepository<PMS.EF.Booking>();
            PersonRepo = uow.GenericRepository<PMS.EF.Person>();
            paymentService = _paymentService;
            locationContextService = _locationContextService;
        }
        public Location AddLocation(AddLocationVM locationVM)
        {
            Location location = new Location
            {
                LocationName = locationVM.LocationName,
                Ar_LocationName = locationVM.Ar_LocationName,
                LocationDescription = locationVM.LocationDescription,
                Prefix = locationVM.Prefix,
                IsEnable = true,
                CreatedBy = locationVM.CreatedBy,
                CreatedDate = locationVM.CreatedDate
            };
            uow.GenericRepository<Location>().Insert(location);
            uow.SaveChanges();
            return location;
        }

        public Location UpdateLocation(AddLocationVM locationVM)
        {
            Location Oldlocation = uow.GenericRepository<Location>().GetByIdAsNoTracking(x => x.LocationID == locationVM.LocationID);
            Location location = GetLocationByID(locationVM.LocationID);
            if (location != null)
            {
                location.LocationName = locationVM.LocationName;
                location.Ar_LocationName = locationVM.Ar_LocationName;
                location.LocationDescription = locationVM.LocationDescription;
                location.UpdatedBy = locationVM.UpdatedBy;
                location.UpdatedDate = locationVM.UpdatedDate;
                location.Prefix = locationVM.Prefix;
                uow.GenericRepository<Location>().Update(location);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Location>(Oldlocation, location);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateLocation,
                        PK = location.LocationID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Location",
                        Reference = location.LocationName,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }


                return location;
            }
            else
                throw new Exception("Location not found to update.");
        }

        public bool DeleteLocation(int locationId)
        {
            Location Oldlocation = uow.GenericRepository<Location>().GetByIdAsNoTracking(x => x.LocationID == locationId);
            Location location = GetLocationByID(locationId);

            if (location != null)
            {
                location.IsEnable = false;

                uow.GenericRepository<Location>().Update(location);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Location>(Oldlocation, location);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteLocation,
                        PK = location.LocationID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Location",
                        Reference = location.LocationName,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return true;
            }
            else
                throw new Exception("Location not found to delete.");
        }

        public List<Location> GetLocations()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            return uow.GenericRepository<Location>().Table.Where(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationID)).ToList();
        }

        public List<Location> GetAllLocations()
        {
            return uow.GenericRepository<Location>().Table.Where(x => x.IsEnable == true).ToList();
        }

        public Location GetLocationByID(int id)
        {
            return uow.GenericRepository<Location>().GetById(id);
        }

        public List<Location> GetLocationsByID(List<int> ids)
        {
            return uow.GenericRepository<Location>().Table.Where(x => ids.Contains(x.LocationID)).ToList();
        }

        public List<AllRoomFeature> GetAllRoomFeatures()
        {
            return uow.GenericRepository<AllRoomFeature>().Table.Where(x => x.IsEnable == true).ToList();
        }

        public AllRoomFeature GetAllRoomFeatureByID(int id)
        {
            return uow.GenericRepository<AllRoomFeature>().GetById(id);
        }
        public AllRoomFeature AddAllRoomFeature(AddAllRoomFeatureVM allRoomFeatureVM)
        {
            AllRoomFeature feature = new AllRoomFeature
            {
                FeatureName = allRoomFeatureVM.FeatureName,
                Ar_FeatureName = allRoomFeatureVM.Ar_FeatureName,
                IsEnable = true,
            };
            if (allRoomFeatureVM.ImageSource != null)
            {
                ImageResult result = new ImageResult();

                Common.ImageUpload upload = new Common.ImageUpload()
                {
                    //Width = 92,
                    //Height = 92,
                    //Quality = 80
                };
                result = upload.RenameUploadFileNew(allRoomFeatureVM.ImageSource);

                if (!result.Success)
                    return feature;
                feature.ImageUrl = result.ImageName;
            }
            uow.GenericRepository<AllRoomFeature>().Insert(feature);
            uow.SaveChanges();
            return feature;
        }

        public AllRoomFeature UpdateAllRoomFeature(AddAllRoomFeatureVM allRoomFeatureVM)
        {
            AllRoomFeature Oldfeature = uow.GenericRepository<EF.AllRoomFeature>().GetByIdAsNoTracking(x => x.AllRoomFeatureId == allRoomFeatureVM.AllRoomFeatureID);
            AllRoomFeature feature = GetAllRoomFeatureByID(allRoomFeatureVM.AllRoomFeatureID);

            if (feature != null)
            {
                feature.FeatureName = allRoomFeatureVM.FeatureName;
                feature.Ar_FeatureName = allRoomFeatureVM.Ar_FeatureName;
                if (allRoomFeatureVM.ImageSource != null)
                {
                    ImageResult result = new ImageResult();

                    Common.ImageUpload upload = new Common.ImageUpload()
                    {
                        Width = 92,
                        Height = 92,
                        Quality = 80
                    };
                    result = upload.RenameUploadFileNew(allRoomFeatureVM.ImageSource);

                    if (!result.Success)
                        return feature;
                    feature.ImageUrl = result.ImageName;
                }
                uow.GenericRepository<AllRoomFeature>().Update(feature);
                uow.SaveChanges();

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.AllRoomFeature>(Oldfeature, feature);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();
                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateRoomFeature,
                        PK = feature.AllRoomFeatureId.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "AllRoomFeature",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return feature;
            }
            else
                throw new Exception("Room feature not found to update.");
        }

        public bool DeleteAllRoomFeature(int id)
        {
            AllRoomFeature Oldfeature = uow.GenericRepository<EF.AllRoomFeature>().GetByIdAsNoTracking(x => x.AllRoomFeatureId == id);
            AllRoomFeature feature = GetAllRoomFeatureByID(id);
            if (feature != null)
            {
                feature.IsEnable = false;
                uow.GenericRepository<AllRoomFeature>().Update(feature);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.AllRoomFeature>(Oldfeature, feature);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteRoomFeature,
                        PK = feature.AllRoomFeatureId.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "AllRoomFeature",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return true;
            }
            else
                throw new Exception("Room feature not found to update.");
        }

        public List<RoomTypeFeature> GetAllRoomTypeFeatures()
        {
            return uow.GenericRepository<RoomTypeFeature>().Table.Where(x => x.IsEnable == true).ToList();
        }

        public List<RoomTypeFeature> GetRoomTypeFeaturesByRoomTypeID(int roomTypeId)
        {
            return uow.GenericRepository<RoomTypeFeature>().Table.Where(x => x.IsEnable == true && x.RoomTypeID == roomTypeId).ToList();
        }

        public List<RoomType> GetRoomTypes()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();
            return uow.GenericRepository<RoomType>().Table.Where(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId)).ToList();
        }

        public List<RoomType> GetRoomTypesForAPI()
        {
            return (from rt in uow.GenericRepository<RoomType>().Table
                    join p in uow.GenericRepository<PriceConfig>().Table
                        on rt.RoomTypeID equals p.RoomTypeID
                    join t in uow.GenericRepository<Term>().Table
                        on p.TermID equals t.TermID
                    where rt.IsEnable == true
                       && p.IsEnable == true
                       && t.IsPublished == true
                    select rt)
          .Distinct()
          .ToList();
        }

        public List<RoomTypeDetail> GetRoomTypeDetails()
        {
            return uow.GenericRepository<RoomTypeDetail>().Table.ToList();
        }

        public RoomType GetRoomTypeByID(int id)
        {
            return uow.GenericRepository<RoomType>().GetById(id);
        }

        public RoomType GetRoomTypeByIDandLocation(int id, int? locationid)
        {
            return uow.GenericRepository<RoomType>().Table.Where(x => x.RoomTypeID == id).Where(x => x.LocationId == locationid).FirstOrDefault();
        }

        public RoomType AddRoomType(AddRoomTypeVM roomTypeVM)
        {
            try
            {
                uow.CreateTransaction();

                RoomType roomType = new RoomType
                {
                    RoomCode = roomTypeVM.RoomCode,
                    RoomName = roomTypeVM.RoomName,
                    RoomDescription = roomTypeVM.RoomDescription,
                    RoomArea = roomTypeVM.RoomArea,
                    IsEnable = true,
                    CreatedBy = roomTypeVM.CreatedBy,
                    CreatedDate = roomTypeVM.CreatedDate,
                    LocationId = roomTypeVM.LocationId,
                    Ar_RoomDescription = roomTypeVM.Ar_RoomDescription,
                    Ar_RoomName = roomTypeVM.Ar_RoomName,
                    BedSpace = roomTypeVM.BedSpace,
                    Actual_Price = roomTypeVM.Actual_Price,
                    Thumbnail = roomTypeVM.thumbnail,
                    Ar_Thumbnail = roomTypeVM.Ar_thumbnail
                };
                uow.GenericRepository<RoomType>().Insert(roomType);
                if (roomTypeVM.SelectedFeatures != null)
                {
                    if (roomTypeVM.SelectedFeatures.Count > 0)
                    {
                        foreach (int i in roomTypeVM.SelectedFeatures)
                        {
                            uow.GenericRepository<RoomTypeFeature>().Insert(
                            new RoomTypeFeature
                            {
                                RoomTypeID = roomType.RoomTypeID,
                                AllRomFeatureID = i,
                                IsEnable = true
                            });
                        }
                    }
                }
                uow.SaveChanges();
                uow.Commit();
                return roomType;
            }
            catch (Exception ex)
            {
                uow.Rollback();
                throw new Exception(ex.Message);
            }
        }

        public RoomType UpdateRoomType(AddRoomTypeVM roomTypeVM)
        {
            try
            {
                uow.CreateTransaction();
                RoomType OldroomType = uow.GenericRepository<RoomType>().GetByIdAsNoTracking(x => x.RoomTypeID == roomTypeVM.RoomTypeID);
                RoomType roomType = GetRoomTypeByID(roomTypeVM.RoomTypeID);

                if (roomType != null)
                {
                    roomType.RoomCode = roomTypeVM.RoomCode;
                    roomType.RoomName = roomTypeVM.RoomName;
                    roomType.RoomDescription = roomTypeVM.RoomDescription;
                    roomType.RoomArea = roomTypeVM.RoomArea;
                    roomType.UpdatedBy = roomTypeVM.UpdatedBy;
                    roomType.UpdatedDate = roomTypeVM.UpdatedDate;
                    roomType.LocationId = roomTypeVM.LocationId;
                    roomType.Ar_RoomName = roomTypeVM.Ar_RoomName;
                    roomType.Ar_RoomDescription = roomTypeVM.Ar_RoomDescription;
                    roomType.BedSpace = roomTypeVM.BedSpace;
                    roomType.Actual_Price = roomTypeVM.Actual_Price;
                    roomType.Thumbnail = roomTypeVM.thumbnail;
                    roomType.Ar_Thumbnail = roomTypeVM.Ar_thumbnail;
                    roomType.RoomInstruction = roomTypeVM.RoomInstruction;
                    roomType.Ar_RoomInstruction = roomTypeVM.Ar_RoomInstruction;
                    uow.GenericRepository<RoomType>().Update(roomType);
                    var features = GetRoomTypeFeaturesByRoomTypeID(roomType.RoomTypeID);
                    if (features.Count > 0)
                    {
                        foreach (var f in features)
                        {
                            uow.GenericRepository<RoomTypeFeature>().Delete(f);
                        }
                    }
                    if (roomTypeVM.SelectedFeatures != null)
                    {
                        if (roomTypeVM.SelectedFeatures.Count > 0)
                        {
                            foreach (int i in roomTypeVM.SelectedFeatures)
                            {
                                uow.GenericRepository<RoomTypeFeature>().Insert(
                                new RoomTypeFeature
                                {
                                    RoomTypeID = roomType.RoomTypeID,
                                    AllRomFeatureID = i,
                                    IsEnable = true
                                });
                            }
                        }
                    }
                    uow.SaveChanges();
                    uow.Commit();
                    var oldobj = new JavaScriptSerializer().Serialize(OldroomType.RoomTypeFeatures.Select(x => new { x.RoomTypeID, x.AllRomFeatureID, x.IsEnable }).ToList());
                    var newobj = new JavaScriptSerializer().Serialize(roomTypeVM.SelectedFeatures);

                    //Insert Audit Log
                    {
                        var difference = Common.Classes.Common.DetailedCompare<EF.RoomType>(OldroomType, roomType);
                        List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                        EF.AuditLog auditLog = new EF.AuditLog()
                        {
                            OldValue = oldobj,
                            NewValue = newobj,
                            AuditType = (int)Enumeration.AuditType.Update,
                            ActionId = (int)Enumeration.CorrespondenceAction.UpdateRoomType,
                            PK = roomType.RoomTypeID.ToString(),
                            UserId = Common.Globals.User.ID,
                            TableName = "RoomType",
                            UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                            AuditLogDetails = difference
                        };
                        auditLogsService.AddAuditLog(auditLog);
                    }
                    return roomType;
                }
                else
                    throw new Exception("Room Type not found to update.");
            }
            catch (Exception ex)
            {
                uow.Rollback();
                throw new Exception(ex.Message);
            }
        }

        public bool DeleteRoomType(int id)
        {
            RoomType OldroomType = uow.GenericRepository<RoomType>().GetByIdAsNoTracking(x => x.RoomTypeID == id);
            RoomType roomType = GetRoomTypeByID(id);

            if (roomType != null)
            {
                roomType.IsEnable = false;

                uow.GenericRepository<RoomType>().Update(roomType);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.RoomType>(OldroomType, roomType);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteRoomtype,
                        PK = roomType.RoomTypeID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "RoomType",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return true;
            }
            else
                throw new Exception("Room Type not found to delete.");
        }
        public List<PriceConfigVM> GetTermsWithRoomNames()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            return uow.GenericRepository<PriceConfig>().Table.Where(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId) && x.IsAvailable == true && x.Term.IsPublished == true)
                            .ToList().Select(x => new PriceConfigVM
                            {
                                TermID = x.TermID,
                                TermName = x.Term.TermName + " - " + x.RoomType.RoomName
                            }).ToList();
        }

        public List<Term> GetTerms()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            return uow.GenericRepository<Term>().Table.Where(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId)).ToList();
        }

        public List<PriceConfigVM> GetTermsByRoomTypeID(int roomTypeId)
        {
            return uow.GenericRepository<PriceConfig>().Table.Where(x => x.RoomTypeID == roomTypeId && x.IsEnable == true).Select(x => new PriceConfigVM
            {
                TermID = x.PriceConfigID,
                TermName = (x.Term.TermDescription == null ? x.Term.TermName.ToString() : x.Term.TermName.ToString()
                  + " - " + x.Term.TermDescription.ToString())
            }).ToList();
        }

        public List<Term> GetTermsForDropDown()
        {
            return uow.GenericRepository<Term>().Table.Where(x => x.IsEnable == true)
                .ToList().Select(x => new Term
                {
                    TermID = x.TermID,
                    TermName = x.TermName + " - " + x.TermDescription
                }).ToList();
        }

        public Term GetTermsByID(int id)
        {
            return uow.GenericRepository<Term>().GetById(id);
        }

        public Term AddTerm(AddTermVM addTermVM)
        {
            Term term = new Term
            {
                LocationId = addTermVM.LocationId,
                FrequencyId = addTermVM.FrequencyId,
                TermName = addTermVM.TermName,
                AR_TermName = addTermVM.Ar_TermName,
                TermDescription = addTermVM.TermDescription,
                Ar_TermDescription = addTermVM.Ar_TermDescription,
                TermStartDate = addTermVM.TermStartDate,
                TermEndDate = addTermVM.TermEndDate,
                Min_Duration = addTermVM.Min_Duration,
                Room_Occupancy = addTermVM.Room_Occupancy,
                Ar_Room_Occupancy = addTermVM.Ar_Room_Occupancy,
                Room_Standared = addTermVM.Room_Standared,
                Ar_Room_Standared = addTermVM.Ar_Room_Standared,
                IsEnable = true,
                CreatedBy = addTermVM.CreatedBy,
                CreatedDate = addTermVM.CreatedDate,
                DurationType = addTermVM.DurationType,
                IsPublished = addTermVM.IsPublished,
                UniversityId = addTermVM.UniversityId
            };
            uow.GenericRepository<Term>().Insert(term);
            uow.SaveChanges();
            return term;
        }


        public Term AddTerm(AddTermVM addTermVM, HttpPostedFileBase file)
        {
            Term term = new Term
            {
                LocationId = addTermVM.LocationId,
                FrequencyId = addTermVM.FrequencyId,
                TermName = addTermVM.TermName,
                AR_TermName = addTermVM.Ar_TermName,
                TermDescription = addTermVM.TermDescription,
                Ar_TermDescription = addTermVM.Ar_TermDescription,
                TermStartDate = addTermVM.TermStartDate,
                TermEndDate = addTermVM.TermEndDate,
                Min_Duration = addTermVM.Min_Duration,
                Room_Occupancy = addTermVM.Room_Occupancy,
                Ar_Room_Occupancy = addTermVM.Ar_Room_Occupancy,
                Room_Standared = addTermVM.Room_Standared,
                Ar_Room_Standared = addTermVM.Ar_Room_Standared,
                IsEnable = true,
                CreatedBy = addTermVM.CreatedBy,
                CreatedDate = addTermVM.CreatedDate,
                DurationType = addTermVM.DurationType,
                IsPublished = addTermVM.IsPublished,
                UniversityId = addTermVM.UniversityId
            };
            uow.GenericRepository<Term>().Insert(term);
            uow.SaveChanges();
            return term;
        }


        public Term UpdateTerm(AddTermVM addTermVM)
        {
            Term Oldterm = uow.GenericRepository<Term>().GetByIdAsNoTracking(x => x.TermID == addTermVM.TermID);
            Term term = GetTermsByID(addTermVM.TermID);
            if (term != null)
            {
                term.LocationId = addTermVM.LocationId;
                term.FrequencyId = addTermVM.FrequencyId;
                term.TermName = addTermVM.TermName;
                term.AR_TermName = addTermVM.Ar_TermName;
                term.TermDescription = addTermVM.TermDescription;
                term.Ar_TermDescription = addTermVM.Ar_TermDescription;
                term.TermStartDate = addTermVM.TermStartDate;
                term.TermEndDate = addTermVM.TermEndDate;
                term.UpdatedBy = addTermVM.UpdatedBy;
                term.UpdatedDate = addTermVM.UpdatedDate;
                term.Min_Duration = addTermVM.Min_Duration;
                term.Room_Standared = addTermVM.Room_Standared;
                term.Ar_Room_Standared = addTermVM.Ar_Room_Standared;
                term.Room_Occupancy = addTermVM.Room_Occupancy;
                term.Ar_Room_Occupancy = addTermVM.Ar_Room_Occupancy;
                term.IsPublished = addTermVM.IsPublished;
                term.UniversityId = addTermVM.UniversityId;
                uow.GenericRepository<Term>().Update(term);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Term>(Oldterm, term);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();
                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateTerm,
                        PK = term.TermID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Term",
                        Reference = term.TermName,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return term;
            }
            else
                throw new Exception("Term not found to update.");
        }

        public bool DeleteTerm(int id)
        {
            Term Oldterm = uow.GenericRepository<Term>().GetByIdAsNoTracking(x => x.TermID == id);
            Term term = GetTermsByID(id);

            if (term != null)
            {
                term.IsEnable = false;

                uow.GenericRepository<Term>().Update(term);
                uow.SaveChanges();

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Term>(Oldterm, term);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteTerm,
                        PK = term.TermID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Term",
                        Reference = term.TermName,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return true;
            }
            else
                throw new Exception("Term not found to delete.");
        }

        public List<PriceConfig> GetPriceConfigs()
        {

            return uow.GenericRepository<PriceConfig>().Table.Where(x => x.IsEnable == true).ToList();
        }

        public PriceConfig GetPriceConfigByID(int id)
        {
            return uow.GenericRepository<PriceConfig>().GetById(id);
        }

        public PriceConfig AddPriceConfig(AddPriceConfigVM priceConfigVM)
        {
            PriceConfig priceConfig = new PriceConfig
            {
                LocationId = priceConfigVM.LocationId,
                TermID = priceConfigVM.TermID,
                RoomTypeID = priceConfigVM.RoomTypeID,
                Price = priceConfigVM.Price,
                InitialDeposit = priceConfigVM.Deposit,
                CleaningCharge = priceConfigVM.CleaningCharge,
                Currency = priceConfigVM.Currency,
                IsEnable = true,
                CreatedBy = priceConfigVM.CreatedBy,
                CreatedDate = priceConfigVM.CreatedDate,
                OrderBy = priceConfigVM.OrderBy
            };
            uow.GenericRepository<PriceConfig>().Insert(priceConfig);
            uow.SaveChanges();
            return priceConfig;
        }

        public int? GetTermIdByName(string termName)
        {
            return uow.GenericRepository<EF.Term>().Table
                .FirstOrDefault(t => t.TermName == termName)?.TermID;
        }

        public int GetRoomTypeIdByTermName(string termName)
        {
            if (termName.Contains("Double"))
            {
                return uow.GenericRepository<EF.RoomType>().Table
                    .FirstOrDefault(r => r.RoomName == "Double Room")?.RoomTypeID ?? 0;
            }
            else if (termName.Contains("Single"))
            {
                return uow.GenericRepository<EF.RoomType>().Table
                    .FirstOrDefault(r => r.RoomName == "Single Room")?.RoomTypeID ?? 0;
            }
            else if (termName.Contains("Studio"))
            {
                return uow.GenericRepository<EF.RoomType>().Table
                    .FirstOrDefault(r => r.RoomName == "Studio Room")?.RoomTypeID ?? 0;
            }
            return 0; // Default to 0 for invalid room types
        }

        public decimal? ExtractPriceFromRateInfo(string rateInfo)
        {
            var rateInfoParts = rateInfo.Split(' ');
            if (rateInfoParts.Length >= 2 && decimal.TryParse(rateInfoParts[1], out decimal extractedPrice))
            {
                return extractedPrice;
            }
            return null; // Invalid price format
        }

        public PriceConfig AddPriceConfig(AddPriceConfigVM priceConfigVM, HttpPostedFileBase file)
        {
            PriceConfig priceConfig = new PriceConfig
            {
                LocationId = priceConfigVM.LocationId,
                TermID = priceConfigVM.TermID,
                RoomTypeID = priceConfigVM.RoomTypeID,
                Price = priceConfigVM.Price,
                InitialDeposit = priceConfigVM.Deposit,
                CleaningCharge = priceConfigVM.CleaningCharge,
                Currency = priceConfigVM.Currency,
                IsEnable = true,
                CreatedBy = priceConfigVM.CreatedBy,
                CreatedDate = priceConfigVM.CreatedDate,
                OrderBy = priceConfigVM.OrderBy
            };
            uow.GenericRepository<PriceConfig>().Insert(priceConfig);
            uow.SaveChanges();
            return priceConfig;
        }

        public bool TryAddPriceConfig(AddPriceConfigVM priceConfig, out string reason)
        {
            reason = string.Empty;

            // Match Term
            var term = uow.GenericRepository<EF.Term>().Table
                .FirstOrDefault(t => t.TermName == priceConfig.TermName);

            if (term == null)
            {
                reason = $"Term '{priceConfig.TermName}' not found.";
                return false;
            }

            priceConfig.TermID = term.TermID;

            // Match RoomType


            var roomType = uow.GenericRepository<EF.RoomType>().Table
       .FirstOrDefault(r => (priceConfig.TermName.Contains("Double") && r.RoomName == "Double Room") ||
                            (priceConfig.TermName.Contains("Single") && r.RoomName == "Single Room") ||
                            (priceConfig.TermName.Contains("Studio") && r.RoomName == "Studio Room"));

            if (roomType != null)
            {
                priceConfig.RoomTypeID = roomType.RoomTypeID;
                priceConfig.RoomName = roomType.RoomName; // Set RoomTypeName
            }
            else
            {
                reason = "Invalid RoomType.";
                return false;
            }

            // Check for existing record with same TermID and RoomTypeID
            var existingPriceConfig = uow.GenericRepository<EF.PriceConfig>().Table
                .FirstOrDefault(pc => pc.TermID == priceConfig.TermID && pc.RoomTypeID == priceConfig.RoomTypeID);

            if (existingPriceConfig != null)
            {
                reason = "Duplicate entry for the same TermID and RoomTypeID.";
                return false;
            }

            // Extract Price from RateInfo
            var rateInfoParts = priceConfig.RateInfo.Split(' ');
            if (rateInfoParts.Length >= 2 && decimal.TryParse(rateInfoParts[1], out decimal extractedPrice))
            {
                priceConfig.Price = extractedPrice;
            }
            else
            {
                reason = "Invalid RateInfo format.";
                return false;
            }

            // Set defaults
            priceConfig.Deposit = 0;
            priceConfig.CleaningCharge = 0;
            priceConfig.OrderBy = 0;
            priceConfig.Currency = "AED";

            // Save to database
            var entity = new PriceConfig
            {
                LocationId = priceConfig.LocationId,
                TermID = priceConfig.TermID,
                RoomTypeID = priceConfig.RoomTypeID,
                Price = priceConfig.Price,
                InitialDeposit = priceConfig.Deposit,
                CleaningCharge = priceConfig.CleaningCharge,
                Currency = priceConfig.Currency,
                IsEnable = true,
                CreatedBy = priceConfig.CreatedBy,
                CreatedDate = priceConfig.CreatedDate,
                OrderBy = priceConfig.OrderBy
            };

            uow.GenericRepository<EF.PriceConfig>().Insert(entity);
            uow.SaveChanges();
            return true;
        }


        public bool AddPriceConfiglist(AddPriceConfigVM priceConfigVM)
        {
            PriceConfig priceConfig = new PriceConfig
            {
                LocationId = priceConfigVM.LocationId,
                TermID = priceConfigVM.TermID,
                RoomTypeID = priceConfigVM.RoomTypeID,
                Price = priceConfigVM.Price,
                InitialDeposit = priceConfigVM.Deposit,
                CleaningCharge = priceConfigVM.CleaningCharge,
                Currency = priceConfigVM.Currency,
                IsEnable = true,
                CreatedBy = priceConfigVM.CreatedBy,
                CreatedDate = priceConfigVM.CreatedDate,
                OrderBy = priceConfigVM.OrderBy,
            };
            uow.GenericRepository<PriceConfig>().Insert(priceConfig);
            uow.SaveChanges();
            return true;
        }

        public PriceConfig UpdatePriceConfig(AddPriceConfigVM priceConfigVM)
        {
            PriceConfig OldpriceConfig = uow.GenericRepository<PriceConfig>().GetByIdAsNoTracking(x => x.PriceConfigID == priceConfigVM.PriceConfigID);
            PriceConfig priceConfig = GetPriceConfigByID(priceConfigVM.PriceConfigID);
            if (priceConfig != null)
            {
                priceConfig.LocationId = priceConfig.LocationId;
                priceConfig.TermID = priceConfigVM.TermID;
                priceConfig.RoomTypeID = priceConfigVM.RoomTypeID;
                priceConfig.Price = priceConfigVM.Price;
                priceConfig.InitialDeposit = priceConfigVM.Deposit;
                priceConfig.CleaningCharge = priceConfigVM.CleaningCharge;
                priceConfig.Currency = priceConfigVM.Currency;
                priceConfig.UpdatedBy = priceConfigVM.UpdatedBy;
                priceConfig.UpdatedDate = priceConfigVM.UpdatedDate;
                priceConfig.IsAvailable = priceConfigVM.IsAvailable;
                priceConfig.OrderBy = priceConfigVM.OrderBy;
                uow.GenericRepository<PriceConfig>().Update(priceConfig);
                uow.SaveChanges();

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.PriceConfig>(OldpriceConfig, priceConfig);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdatePriceConfig,
                        PK = priceConfig.PriceConfigID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "PriceConfig",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return priceConfig;
            }
            else
                throw new Exception("Price config not found to update.");
        }

        public bool DeletePriceConfig(int id)
        {
            PriceConfig OldpriceConfig = uow.GenericRepository<PriceConfig>().GetByIdAsNoTracking(x => x.PriceConfigID == id);
            PriceConfig priceConfig = GetPriceConfigByID(id);

            if (priceConfig != null)
            {
                priceConfig.IsEnable = false;

                uow.GenericRepository<PriceConfig>().Update(priceConfig);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.PriceConfig>(OldpriceConfig, priceConfig);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeletePriceConfig,
                        PK = priceConfig.PriceConfigID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "PriceConfig",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return true;
            }
            else
                throw new Exception("Price config not found to delete.");
        }

        public List<PriceConfigVM> GetPriceConfigVM()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            return uow.GenericRepository<PriceConfig>().Table.Where(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId)).
                Select(x => new PriceConfigVM
                {

                    LocationId = x.LocationId,
                    LocationName = x.Location.LocationName,
                    PriceConfigID = x.PriceConfigID,
                    TermID = x.TermID,
                    RoomTypeID = x.RoomTypeID,
                    TermName = x.Term.TermName,
                    UniversityName = x.Term.University.UniversityName,
                    TermDescription = x.Term.TermDescription,
                    RoomTypeName = x.RoomType.RoomName,
                    RoomTypeDescription = x.RoomType.RoomDescription,
                    Currency = x.Currency,
                    Price = x.Price,
                    Deposit = x.InitialDeposit,
                    CleaningCharge = x.CleaningCharge,
                    IsAvailable = x.IsAvailable,
                    OrderBy = x.OrderBy
                }).ToList();
        }

        public List<Project> GetProjects()
        {
            return uow.GenericRepository<Project>().Table.Where(x => x.IsEnable == true).ToList();
        }

        public List<Project> GetProjects(int locationId)
        {
            return uow.GenericRepository<Project>().Table.Where(x => x.IsEnable == true && x.LocationID == locationId).ToList();
        }

        public Project GetProjectByID(int id)
        {
            return uow.GenericRepository<Project>().GetById(id);
        }

        public Project AddProject(AddProjectVM projectVM)
        {
            Project project = new Project
            {
                LocationID = projectVM.LocationID,
                ProjectName = projectVM.ProjectName,
                ProjectCity = projectVM.ProjectCity,
                ProjectState = projectVM.ProjectState,
                ProjectZip = projectVM.ProjectZip,
                ProjectAddress = projectVM.ProjectAddress,
                ProjectDescription = projectVM.ProjectDescription,
                IsEnable = true,
                CreatedBy = projectVM.CreatedBy,
                CreatedDate = projectVM.CreatedDate
            };
            uow.GenericRepository<Project>().Insert(project);
            uow.SaveChanges();

            return project;
        }

        public Project UpdateProject(AddProjectVM projectVM)
        {
            Project Oldproject = uow.GenericRepository<Project>().GetByIdAsNoTracking(x => x.ProjectID == projectVM.ProjectID);
            Project project = GetProjectByID(projectVM.ProjectID);

            if (project != null)
            {
                project.LocationID = projectVM.LocationID;
                project.ProjectName = projectVM.ProjectName;
                project.ProjectCity = projectVM.ProjectCity;
                project.ProjectState = projectVM.ProjectState;
                project.ProjectZip = projectVM.ProjectZip;
                project.ProjectAddress = projectVM.ProjectAddress;
                project.ProjectDescription = projectVM.ProjectDescription;
                project.UpdatedBy = projectVM.UpdatedBy;
                project.UpdatedDate = projectVM.UpdatedDate;
                uow.GenericRepository<Project>().Update(project);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Project>(Oldproject, project);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateProject,
                        PK = project.ProjectID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Project",
                        Reference = project.ProjectName,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return project;
            }
            else
                throw new Exception("Project not found to update.");
        }

        public bool DeleteProject(int id)
        {
            Project Oldproject = uow.GenericRepository<Project>().GetByIdAsNoTracking(x => x.ProjectID == id);
            Project project = GetProjectByID(id);

            if (project != null)
            {
                project.IsEnable = false;

                uow.GenericRepository<Project>().Update(project);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Project>(Oldproject, project);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteProject,
                        PK = project.ProjectID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Project",
                        Reference = project.ProjectName,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }

                return true;
            }
            else
                throw new Exception("Project not found to delete.");
        }

        public List<Building> GetBuildings()
        {
            return uow.GenericRepository<Building>().Table.Where(x => x.IsEnable == true).ToList();
        }

        public List<Building> GetBuildings(int projectId)
        {
            return uow.GenericRepository<Building>().Table.Where(x => x.IsEnable == true && x.ProjectID == projectId).ToList();
        }

        public Building GetBuildingByID(int id)
        {
            return uow.GenericRepository<Building>().GetById(id);
        }

        public Building AddBuilding(AddBuildingVM buildingVM)
        {
            Building building = new Building
            {
                ProjectID = buildingVM.ProjectID,
                BuildingName = buildingVM.BuildingName,
                BuildingDescription = buildingVM.BuildingDescription,
                IsEnable = true,
                CreatedBy = buildingVM.CreatedBy,
                CreatedDate = buildingVM.CreatedDate
            };
            uow.GenericRepository<Building>().Insert(building);
            uow.SaveChanges();
            return building;
        }

        public Building UpdateBuilding(AddBuildingVM buildingVM)
        {
            Building Oldbuilding = uow.GenericRepository<Building>().GetByIdAsNoTracking(x => x.BuildingID == buildingVM.BuildingID);
            Building building = GetBuildingByID(buildingVM.BuildingID);
            if (building != null)
            {
                building.ProjectID = buildingVM.ProjectID;
                building.BuildingName = buildingVM.BuildingName;
                building.BuildingDescription = buildingVM.BuildingDescription;
                building.UpdatedBy = buildingVM.UpdatedBy;
                building.UpdatedDate = buildingVM.UpdatedDate;
                uow.GenericRepository<Building>().Update(building);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Building>(Oldbuilding, building);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateBuilding,
                        PK = building.BuildingID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Building",
                        Reference = building.BuildingName,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return building;
            }
            else
                throw new Exception("Building not found to update.");
        }

        public bool DeleteBuilding(int id)
        {
            Building Oldbuilding = uow.GenericRepository<Building>().GetByIdAsNoTracking(x => x.BuildingID == id);
            Building building = GetBuildingByID(id);
            if (building != null)
            {
                building.IsEnable = false;
                uow.GenericRepository<Building>().Update(building);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Building>(Oldbuilding, building);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();
                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteBuilding,
                        PK = building.BuildingID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Building",
                        Reference = building.BuildingName,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return true;
            }
            else
                throw new Exception("Building not found to delete.");
        }

        public List<Floor> GetFloors()
        {
            return uow.GenericRepository<Floor>().Table.Where(x => x.IsEnable == true).ToList();
        }

        public List<Floor> GetFloors(int buildingId)
        {
            return uow.GenericRepository<Floor>().Table.Where(x => x.IsEnable == true && x.BuildingID == buildingId).ToList();
        }

        public Floor GetFloorByID(int id)
        {
            return uow.GenericRepository<Floor>().GetById(id);
        }

        public Floor AddFloor(AddFloorVM floorVM)
        {
            Floor floor = new Floor
            {
                BuildingID = floorVM.BuildingID,
                FloorName = floorVM.FloorName,
                FloorDescription = floorVM.FloorDescription,
                IsEnable = true,
                CreatedBy = floorVM.CreatedBy,
                CreatedDate = floorVM.CreatedDate
            };
            uow.GenericRepository<Floor>().Insert(floor);
            uow.SaveChanges();
            return floor;
        }

        public Floor UpdateFloor(AddFloorVM floorVM)
        {
            Floor oldfloor = uow.GenericRepository<Floor>().GetByIdAsNoTracking(x => x.FloorID == floorVM.FloorID);
            Floor floor = GetFloorByID(floorVM.FloorID);
            if (floor != null)
            {
                floor.BuildingID = floorVM.BuildingID;
                floor.FloorName = floorVM.FloorName;
                floor.FloorDescription = floorVM.FloorDescription;
                floor.UpdatedBy = floorVM.UpdatedBy;
                floor.UpdatedDate = floorVM.UpdatedDate;

                uow.GenericRepository<Floor>().Update(floor);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Floor>(oldfloor, floor);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateFloor,
                        PK = floor.FloorID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Floor",
                        Reference = floor.FloorName,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return floor;
            }
            else
                throw new Exception("Floor not found to update.");
        }

        public bool DeleteFloor(int id)
        {
            Floor oldfloor = uow.GenericRepository<Floor>().GetByIdAsNoTracking(x => x.FloorID == id);
            Floor floor = GetFloorByID(id);
            if (floor != null)
            {
                floor.IsEnable = false;
                uow.GenericRepository<Floor>().Update(floor);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Floor>(oldfloor, floor);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();
                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteFloor,
                        PK = floor.FloorID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Floor",
                        Reference = floor.FloorName,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return true;
            }
            else
                throw new Exception("Floor not found to delete.");
        }

        public List<Room> GetRooms()
        {
            return uow.GenericRepository<Room>().Table.Where(x => x.IsEnable == true).ToList();
        }

        public List<Room> GetRooms(int floorId)
        {
            return uow.GenericRepository<Room>().Table.Where(x => x.IsEnable == true && x.FloorID == floorId).ToList();
        }

        public Room GetRoomByID(int id)
        {
            return uow.GenericRepository<Room>().GetById(id);
        }

        public Room AddRoom(AddRoomVM roomVM)
        {
            Room room = new Room
            {
                FloorID = roomVM.FloorID,
                RoomTypeID = roomVM.RoomTypeID,
                RoomGender = roomVM.RoomGender,
                RoomName = roomVM.RoomName,
                RoomSize = roomVM.RoomSize,
                RoomDescription = roomVM.RoomDescription,
                RoomLockId = roomVM.RoomLockId,
                IsEnable = true,
                CreatedBy = roomVM.CreatedBy,
                CreatedDate = roomVM.CreatedDate
            };
            uow.GenericRepository<Room>().Insert(room);
            uow.SaveChanges();
            return room;
        }

        public Room ExcelAddRoom(AddRoomVM roomVM, HttpPostedFileBase file)
        {
            Room room = new Room
            {
                FloorID = roomVM.FloorID,
                RoomTypeID = roomVM.RoomTypeID,
                RoomName = roomVM.RoomName,
                RoomSize = roomVM.RoomSize,
                RoomDescription = roomVM.RoomDescription,
                RoomLockId = roomVM.RoomLockId,
                IsEnable = true,
                CreatedBy = roomVM.CreatedBy,
                CreatedDate = roomVM.CreatedDate
            };
            uow.GenericRepository<Room>().Insert(room);
            uow.SaveChanges();
            return room;
        }

        public Room UpdateRoom(AddRoomVM roomVM)
        {
            Room oldroom = uow.GenericRepository<Room>().GetByIdAsNoTracking(x => x.RoomID == roomVM.RoomID);
            Room room = GetRoomByID(roomVM.RoomID);
            if (room != null)
            {
                room.FloorID = roomVM.FloorID;
                room.RoomTypeID = roomVM.RoomTypeID;
                room.RoomGender = roomVM.RoomGender;
                room.RoomName = roomVM.RoomName;
                room.RoomSize = roomVM.RoomSize;
                room.RoomLockId = roomVM.RoomLockId;
                room.RoomDescription = roomVM.RoomDescription;
                room.UpdatedBy = roomVM.UpdatedBy;
                room.UpdatedDate = roomVM.UpdatedDate;
                uow.GenericRepository<Room>().Update(room);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Room>(oldroom, room);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateRoom,
                        PK = room.RoomID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Room",
                        Reference = room.RoomName,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return room;
            }
            else
                throw new Exception("Room not found to update.");
        }

        public bool DeleteRoom(int id)
        {
            Room room = GetRoomByID(id);
            if (room != null)
            {
                room.IsEnable = false;
                uow.GenericRepository<Room>().Update(room);
                uow.SaveChanges();
                return true;
            }
            else
                throw new Exception("Room not found to delete.");
        }

        public List<BedSpace> GetBedSpaces()
        {
            return uow.GenericRepository<BedSpace>().Table.Where(x => x.IsEnable == true).ToList();
        }

        public List<BedSpace> GetBedSpaces(int roomId)
        {
            return uow.GenericRepository<BedSpace>().Table.Where(x => x.IsEnable == true && x.RoomID == roomId).ToList();
        }

        public BedSpace GetBedSpaceByID(int id)
        {
            return uow.GenericRepository<BedSpace>().GetById(id);
        }

        public BedSpace AddBedSpace(AddBedSpaceVM bedSpaceVM)
        {
            BedSpace bedSpace = new BedSpace
            {
                RoomID = bedSpaceVM.RoomID,
                RoomGender = bedSpaceVM.BedSpaceGender,
                BedName = bedSpaceVM.BedSpaceName,
                BedAddress = bedSpaceVM.BedSpaceAddress,
                BedDescription = bedSpaceVM.BedSpaceDescription,
                IsEnable = true,
                CreatedBy = bedSpaceVM.CreatedBy,
                CreatedDate = bedSpaceVM.CreatedDate,
                Status = bedSpaceVM.Status
            };
            uow.GenericRepository<BedSpace>().Insert(bedSpace);
            uow.SaveChanges();
            return bedSpace;
        }
        public BedSpace ExcelAddBedSpace(AddBedSpaceVM bedSpaceVM, HttpPostedFileBase file)
        {
            // Match Room
            var room = uow.GenericRepository<EF.Room>().Table
                .FirstOrDefault(r => r.RoomName == bedSpaceVM.RoomName);

            bedSpaceVM.RoomID = room.RoomID;

            BedSpace bedSpace = new BedSpace
            {
                RoomID = bedSpaceVM.RoomID,
                RoomGender = bedSpaceVM.BedSpaceGender,
                BedName = bedSpaceVM.BedSpaceName,
                BedAddress = bedSpaceVM.BedSpaceAddress,
                BedDescription = bedSpaceVM.BedSpaceDescription,
                IsEnable = true,
                CreatedBy = bedSpaceVM.CreatedBy,
                CreatedDate = bedSpaceVM.CreatedDate,
                Status = bedSpaceVM.Status
            };
            uow.GenericRepository<BedSpace>().Insert(bedSpace);
            uow.SaveChanges();
            return bedSpace;
        }

        public BedSpace UpdateBedSpace(AddBedSpaceVM bedSpaceVM)
        {
            BedSpace oldbedSpace = uow.GenericRepository<BedSpace>().GetByIdAsNoTracking(x => x.BedSpaceID == bedSpaceVM.BedSpaceID);
            BedSpace bedSpace = GetBedSpaceByID(bedSpaceVM.BedSpaceID);
            if (bedSpace != null)
            {
                bedSpace.RoomID = bedSpaceVM.RoomID;
                bedSpace.RoomGender = bedSpaceVM.BedSpaceGender;
                bedSpace.BedName = bedSpaceVM.BedSpaceName;
                bedSpace.BedAddress = bedSpaceVM.BedSpaceAddress;
                bedSpace.BedDescription = bedSpaceVM.BedSpaceDescription;
                bedSpace.UpdatedBy = bedSpaceVM.UpdatedBy;
                bedSpace.UpdatedDate = bedSpaceVM.UpdatedDate;
                bedSpace.Status = bedSpaceVM.Status;

                uow.GenericRepository<BedSpace>().Update(bedSpace);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.BedSpace>(oldbedSpace, bedSpace);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateBedSpace,
                        PK = bedSpace.BedSpaceID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "BedSpace",
                        Reference = bedSpace.BedName,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return bedSpace;
            }
            else
                throw new Exception("Bed space not found to update.");
        }

        public bool DeleteBedSpace(int id)
        {
            BedSpace oldbedSpace = uow.GenericRepository<BedSpace>().GetByIdAsNoTracking(x => x.BedSpaceID == id);
            BedSpace bedSpace = GetBedSpaceByID(id);
            if (bedSpace != null)
            {
                bedSpace.IsEnable = false;
                uow.GenericRepository<BedSpace>().Update(bedSpace);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.BedSpace>(oldbedSpace, bedSpace);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteBedSpace,
                        PK = bedSpace.BedSpaceID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "BedSpace",
                        Reference = bedSpace.BedName,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return true;
            }
            else
                throw new Exception("Bed space not found to delete.");
        }

        public List<Building> GetBlocks()
        {
            throw new NotImplementedException();
        }

        public bool AddOrEditLocationSettings(LocationSettingsVM locationSettingsVM)
        {
            var data = uow.GenericRepository<EF.LocationSetting>().Table.Where(x => x.LocationId == locationSettingsVM.LocationId).FirstOrDefault();
            if (data == null)
            {
                var configuration = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<LocationSettingsVM, EF.LocationSetting>().BeforeMap((s, d) => s.CreatedDate = DateTime.Now).BeforeMap((s, d) => s.CreatedBy = Common.Globals.User.ID);
                });
                var mapper = new Mapper(configuration);
                var dest = mapper.Map<LocationSettingsVM, EF.LocationSetting>(locationSettingsVM);
                if (!string.IsNullOrWhiteSpace(locationSettingsVM.TransactionPassword))
                {
                    dest.TransactionPassword = PMS.Common.Security.StringCipher.Encrypt(locationSettingsVM.TransactionPassword);
                }
                uow.GenericRepository<EF.LocationSetting>().Insert(dest);
                uow.SaveChanges();
                return true;
            }
            else
            {
                data.RegistrationFee = locationSettingsVM.RegistrationFee;
                data.CodeOfConduct_EN = locationSettingsVM.CodeOfConduct_EN;
                data.CodeOfConduct_AR = locationSettingsVM.CodeOfConduct_AR;
                data.TermsAndCondition_EN = locationSettingsVM.TermsAndCondition_EN;
                data.TermsAndCondition_AR = locationSettingsVM.TermsAndCondition_AR;
                data.UpdatedDate = DateTime.Now;
                data.UpdatedBy = Common.Globals.User.ID;
                data.CompanyName = locationSettingsVM.CompanyName;
                data.VATNo = locationSettingsVM.VATNo;
                data.IBAN = locationSettingsVM.IBAN;
                data.PaymentGateWay = locationSettingsVM.PaymentGateWay;
                data.ReferralProgram = locationSettingsVM.ReferralProgram;
                data.ReferralIsActive = locationSettingsVM.ReferralIsActive;
                data.PreCheckinDocumentationIsActive = locationSettingsVM.PreCheckinDocumentationIsActive;
                data.Def_Acc_Pay = locationSettingsVM.Def_Acc_Pay;
                data.Def_Acc_Rec = locationSettingsVM.Def_Acc_Rec;
                data.Def_Acc_Discount = locationSettingsVM.Def_Acc_Discount;
                data.Def_Acc_Adv_Pay = locationSettingsVM.Def_Acc_Adv_Pay;
                data.Bank = locationSettingsVM.Bank;
                data.Branch = locationSettingsVM.Branch;
                data.Account = locationSettingsVM.Account;
                data.Title = locationSettingsVM.Title;
                data.Currency = locationSettingsVM.Currency;
                data.SwiftCode = locationSettingsVM.SwiftCode;
                if (!string.IsNullOrWhiteSpace(locationSettingsVM.TransactionPassword))
                {
                    data.TransactionPassword = PMS.Common.Security.StringCipher.Encrypt(locationSettingsVM.TransactionPassword);
                }
                uow.GenericRepository<EF.LocationSetting>().Update(data);
                uow.SaveChanges();

                //Update Cache of COA
                LocationAccountsCacheHelper.UpdateLocationSettingsCache(locationSettingsVM.LocationId, uow);

            }
            return true;
        }

        public LocationSettingsVM GetLocationSettingsByLocationid(int locationid)
        {
            var model = uow.GenericRepository<LocationSetting>().Table
                .Where(x => x.LocationId == locationid).FirstOrDefault();
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<EF.LocationSetting, LocationSettingsVM>();
            });
            var mapper = new Mapper(configuration);
            var dest = mapper.Map<EF.LocationSetting, LocationSettingsVM>(model);
            if (dest != null && !string.IsNullOrWhiteSpace(model?.TransactionPassword))
            {
                try { dest.TransactionPassword = PMS.Common.Security.StringCipher.Decrypt(model.TransactionPassword); } catch { }
            }
            return dest;
        }


        public List<UniversitiesVM> GetUniversityListByLoactionId(int id, string culture)
        {
            uow.Context.Configuration.LazyLoadingEnabled = true;
            var list = uow.GenericRepository<University>().Table.Where(x => x.LocationId == id).Where(x => x.IsEnable == true && x.IsActive == true).ToList();
            List<UniversitiesVM> model = new List<UniversitiesVM>();
            if (culture.Contains("ar-"))
            {
                foreach (var item in list)
                {
                    UniversitiesVM vM = new UniversitiesVM();
                    vM.Id = item.Id;
                    vM.UniversityName = item.UniversityArabicName == null ? item.UniversityName : item.UniversityArabicName;
                    model.Add(vM);
                }
            }
            else
            {
                foreach (var item in list)
                {
                    UniversitiesVM vM = new UniversitiesVM();
                    vM.Id = item.Id;
                    vM.UniversityName = item.UniversityName;
                    model.Add(vM);
                }
            }
            uow.Context.Configuration.LazyLoadingEnabled = false;
            return model;
        }

        public List<University> GetAllUniversities()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            return uow.GenericRepository<University>().GetAll().Where(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId)).ToList();
        }

        public List<University> GetAllUniversityList()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            return uow.GenericRepository<University>().GetAll().Where(x => x.IsEnable == true && x.IsActive == true && x.LocationId != 1 && assignedLocationIds.Contains((int)x.LocationId)).ToList();
        }

        public UniversityVM GetUniversityById(int id)
        {
            var university = uow.GenericRepository<EF.University>().Table.Where(x => x.Id == id).Select(x => new UniversityVM
            {
                Id = x.Id,
                UniversityName = x.UniversityName,
                LocationId = x.LocationId,
                IsEnable = x.IsEnable,
                CreatedBy = x.CreatedBy,
                CreatedDate = x.CreatedDate,
                UpdatedBy = x.Updatedby,
                UpdatedDate = x.UpdatedDate,
                UniversityArabicName = x.UniversityArabicName,
                UniDescription = x.UniDescription,
                Ar_UniDescription = x.Ar_UniDescription,
                Prefix = x.Prefix,
                IsActive = x.IsActive,
                EmailCC = x.EmailCC,
                EmailPrefix = x.Email,
                ThumbnailImageUrl = x.ImageUrl

            }).FirstOrDefault();
            return university;
        }

        public bool AddNewUniversity(UniversityVM universityVM)
        {
            try
            {
                var university = new EF.University();
                if (universityVM.ThumbnailImage != null)
                {
                    ImageResult result = new ImageResult();
                    Common.ImageUpload upload = new Common.ImageUpload()
                    {
                        Width = 2250,
                        Height = 508,
                        Quality = 80
                    };
                    result = upload.RenameUploadFile(universityVM.ThumbnailImage);

                    if (!result.Success)
                        return false;
                    university.ImageUrl = result.ImageName;
                }
                else
                {
                    university.ImageUrl = null;

                }

                university.CreatedBy = Common.Globals.User.ID;
                university.CreatedDate = DateTime.Now;
                university.IsEnable = true;
                university.UniversityName = universityVM.UniversityName;
                university.UniversityArabicName = universityVM.UniversityArabicName;
                university.UniDescription = universityVM.UniDescription;
                university.Ar_UniDescription = universityVM.Ar_UniDescription;
                university.Prefix = universityVM.Prefix;
                university.IsActive = universityVM.IsActive;
                university.LocationId = universityVM.LocationId;
                university.EmailCC = universityVM.EmailCC;
                university.Email = universityVM.EmailPrefix;
                uow.GenericRepository<University>().Insert(university);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool UpdateUniversity(UniversityVM universityVM)
        {
            try
            {
                var university = new EF.University();
                var oldmode = uow.GenericRepository<University>().GetByIdAsNoTracking(x => x.Id == universityVM.Id);
                var mode = uow.GenericRepository<University>().GetById(universityVM.Id);
                ImageResult result = new ImageResult();
                Common.ImageUpload upload = new Common.ImageUpload()
                {
                    Width = 2250,
                    Height = 508,
                    Quality = 80
                };

                if (universityVM.ThumbnailImage != null)
                {
                    result = upload.RenameUploadFile(universityVM.ThumbnailImage);
                    if (!result.Success)
                        return false;
                    mode.ImageUrl = result.ImageName;
                }

                mode.UniversityName = universityVM.UniversityName;
                mode.UniversityArabicName = universityVM.UniversityArabicName;
                mode.UniDescription = universityVM.UniDescription;
                mode.Ar_UniDescription = universityVM.Ar_UniDescription;
                mode.Prefix = universityVM.Prefix;
                mode.IsActive = universityVM.IsActive;
                mode.LocationId = universityVM.LocationId;
                mode.Updatedby = Common.Globals.User.ID;
                mode.UpdatedDate = DateTime.Now;
                mode.EmailCC = universityVM.EmailCC;
                mode.Email = universityVM.EmailPrefix;
                uow.GenericRepository<University>().Update(mode);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.University>(oldmode, mode);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateUniversity,
                        PK = university.Id.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Universities",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool DeleteUniversity(int id)
        {
            var oldmode = uow.GenericRepository<University>().GetByIdAsNoTracking(x => x.Id == id);
            var uni = uow.GenericRepository<EF.University>().GetById(id);
            if (uni != null)
            {
                uni.IsEnable = false;
                uni.Updatedby = Common.Globals.User.ID;
                uni.UpdatedDate = DateTime.Now;
                uow.GenericRepository<University>().Update(uni);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.University>(oldmode, uni);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteUniversity,
                        PK = uni.Id.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Universities",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public int? GetLastLocation()
        {
            int id = PMS.Common.Globals.User.ID;
            var User = uow.GenericRepository<UserMaster>().GetById(id);

            var lastLocationId = User.LastLocationId;
            return lastLocationId;
        }
        public void UpdateLastLocation(int UpdateLocation)
        {
            int id = PMS.Common.Globals.User.ID;
            var User = uow.GenericRepository<UserMaster>().GetById(id);

            User.LastLocationId = UpdateLocation;
            uow.GenericRepository<UserMaster>().Update(User);
            uow.SaveChanges();

        }

        //Booking API To Get all Terms and PAckeges 
        public BookingSearchVM GetBookingSearches(string location, string roomType, string duration, string university, string currentCulture)
        {
            BookingSearchVM model = new BookingSearchVM();

            try
            {
                var roomsDetail = uow.GenericRepository<v_RommTypeDetail>().GetAll().Where(x => x.LocationId == Convert.ToInt32(location)).Where(x => x.RoomTypeID == Convert.ToInt32(roomType)).Where(x => x.Prefix == university).ToList();

                if (roomsDetail == null)
                    return model;

                model.SearchResult.IsClubRoom = roomsDetail.FirstOrDefault().IsClubRoom;
                model.SearchResult.Room = roomsDetail.FirstOrDefault().RoomName;
                model.SearchResult.RoomTitle = currentCulture.StartsWith("en-") ? roomsDetail.FirstOrDefault().RoomName : roomsDetail.FirstOrDefault().Ar_RoomName;
                model.SearchResult.RoomDescription = currentCulture.StartsWith("en-") ? roomsDetail.FirstOrDefault().RoomDescription : roomsDetail.FirstOrDefault().Ar_RoomDescription;
                model.SearchResult.RoomInstruction = currentCulture.StartsWith("en-") ? roomsDetail.FirstOrDefault().RoomInstruction : roomsDetail.FirstOrDefault().Ar_RoomInstruction;
                model.SearchResult.Actual_Price = (int)roomsDetail.FirstOrDefault().Actual_Price;
                model.SearchResult.Currency = roomsDetail.FirstOrDefault().Currency;
                model.SearchResult.Bank = roomsDetail.FirstOrDefault().Bank;
                model.SearchResult.Branch = roomsDetail.FirstOrDefault().Branch;
                model.SearchResult.BranchTitle = roomsDetail.FirstOrDefault().Title;
                model.SearchResult.Account = roomsDetail.FirstOrDefault().Account;
                model.SearchResult.SwiftCode = roomsDetail.FirstOrDefault().SwiftCode;
                model.SearchResult.Commintments = GetCommintments(roomsDetail, duration, university, currentCulture);
                model.SearchResult.Prefix = roomsDetail.FirstOrDefault().Prefix;
                model.SearchResult.Email = roomsDetail.FirstOrDefault().Email;
                //Get room images, Features and Other offers
                model.SearchResult.RoomTypeImages = GetWebRoomImages(Convert.ToInt32(roomType));
                model.SearchResult.RoomTypeFeatureIcons = GetWebRoomFeatures(Convert.ToInt32(roomType), currentCulture);
                model.OtherOffers = GetOtherOffers(roomType, location, roomsDetail, university, currentCulture);

            }
            catch (Exception ex)
            {
                throw;

            }


            return model;


        }

        public BookingSearchVM GetRoomsDetail(string location, string currentCulture)
        {
            BookingSearchVM model = new BookingSearchVM();

            try
            {
                var roomsDetail = uow.GenericRepository<v_RommTypeDetail>().GetAll().Where(x => x.LocationId == Convert.ToInt32(location)).ToList();

                if (roomsDetail == null)
                    return model;
                model.SearchResult.Currency = roomsDetail.FirstOrDefault().Currency;
                model.SearchResult.Room = roomsDetail.FirstOrDefault().RoomName;
                model.SearchResult.Ar_Room = roomsDetail.FirstOrDefault().Ar_RoomName;
                model.SearchResult.RoomDescription = roomsDetail.FirstOrDefault().RoomDescription;
                model.SearchResult.Ar_RoomDescription = roomsDetail.FirstOrDefault().Ar_RoomDescription;
                //Get room images
                model.SearchResult.RoomTypeImages = GetWebRoomImages();
                model.SearchResult.RoomTypeFeatureIcons = GetWebRoomFeatures(currentCulture);
            }
            catch (Exception ex)
            {
                throw;

            }
            return model;
        }

        public List<Commitment> GetCommintments(List<v_RommTypeDetail> rommTypeDetails, string duration, string university, string currentCulture)
        {
            List<Commitment> commitmentsList = new List<Commitment>();
            List<Term> terms = new List<Term>();
            try
            {
                var comit = rommTypeDetails.Where(x => x.Price > 0).OrderBy(x => x.OrderBy);
                foreach (var item in university == null ? comit : comit)
                {
                    Commitment commitment = new Commitment();
                    Term term = new Term();
                    decimal totalPrice = university == null ? Convert.ToDecimal(item.Price * 1) : Convert.ToDecimal(item.Price * 1);
                    commitment.RoomTypePriceID = item.PriceConfigID;
                    commitment.Price = (totalPrice / 1).ToString();
                    commitment.DurationMonths = Convert.ToInt32(1);
                    commitment.Min_Duration = item.Min_Duration;
                    commitment.Currency = item.Currency;
                    commitment.PriceText = String.Format("{0:n0}", totalPrice);
                    commitment.IsAvailable = item.IsAvailable;
                    commitment.TimeStartDate = item.TermStartDate;
                    commitment.TimeEndDate = item.TermEndDate;
                    if (currentCulture.StartsWith("en-"))
                    {
                        commitment.CommitmentText = (item.TermName.EndsWith("ly") ? item.TermName.Substring(0, item.TermName.Length - 2) : item.TermName);
                        commitment.CommitmentLabel = item.TermName;
                        commitment.CommitmentDescription = item.TermDescription;
                        commitment.Room_Occupancy = item.Room_Occupancy ?? "";
                        commitment.RoomStandard = item.Room_Standared ?? "";
                        commitment.Ar_Room_Occupancy = item.Ar_Room_Occupancy ?? "";
                        commitment.Ar_RoomStandard = item.Ar_Room_Standared ?? "";
                    }
                    else
                    {
                        commitment.CommitmentText = item.AR_TermName;
                        commitment.CommitmentLabel = item.AR_TermName;
                        commitment.CommitmentDescription = item.Ar_TermDescription;
                        commitment.Ar_Room_Occupancy = item.Ar_Room_Occupancy ?? "";
                        commitment.Ar_RoomStandard = item.Ar_Room_Standared ?? "";
                        commitment.Room_Occupancy = item.Ar_Room_Occupancy ?? "";
                        commitment.RoomStandard = item.Ar_Room_Standared ?? "";
                    }
                    commitment.TotalRent = String.Format("{0:n0}", totalPrice);
                    commitment.Frequency = item.FrequencyId ?? 1;
                    commitment.CommitmentDeposit = String.Format("{0:n0}", university == null ? item.InitialDeposit : item.InitialDeposit);
                    commitment.RefundableDeposit = university == null ? Convert.ToDecimal(item.InitialDeposit) : Convert.ToDecimal(item.InitialDeposit);
                    commitment.RefundableDepositText = String.Format("{0:n0}", university == null ? Convert.ToDecimal(item.InitialDeposit) : Convert.ToDecimal(item.InitialDeposit));
                    if (item.TermID == Convert.ToInt32(duration) && item.IsAvailable != false)
                        commitment.IsSelected = true;
                    else
                        commitment.IsSelected = false;
                    commitmentsList.Add(commitment);
                }
                return commitmentsList;
            }
            catch (Exception ex)
            {
                return commitmentsList;

            }
        }

        private List<OtherOffersVM> GetOtherOffers(string roomType, string location, List<v_RommTypeDetail> roomsDetail, string university, string currentCulture)
        {
            List<OtherOffersVM> otherOffersList = new List<OtherOffersVM>();
            var rommTypeDetails = uow.GenericRepository<v_RommTypeDetail>().GetAll().Where(x => x.LocationId == Convert.ToInt32(location)).Where(x => x.Prefix == university).ToList();
            var room = roomsDetail.Where(x => x.RoomTypeID == Convert.ToInt32(roomType)).FirstOrDefault();
            if (room != null)
            {
                rommTypeDetails.RemoveAll(x => x.RoomName == room.RoomName);
            }
            var durationList = rommTypeDetails.Where(x => x.RoomTypeID != Convert.ToInt32(roomType)).ToList();
            foreach (var item in durationList.OrderBy(x => x.Price))
            {
                if (item.RoomName.ToLower().Contains("club") && item.Room_Standared != null && item.Room_Standared.ToLower().Contains("premium") != true)
                {
                    continue;
                }
                OtherOffersVM otherOffers = new OtherOffersVM();
                otherOffers.RoomTitle = currentCulture.StartsWith("en-") ? item.RoomName : item.Ar_RoomName;
                otherOffers.RoomDescription = currentCulture.StartsWith("en-") ? item.RoomDescription : item.Ar_RoomDescription;
                otherOffers.Location = location;
                otherOffers.Duration = item.TermID.ToString();
                otherOffers.RoomType = item.RoomTypeID.ToString();
                otherOffers.CommitmentLabel = item.TermName.ToString();
                otherOffers.Price = university == null ? String.Format("{0:n0}", item.Price) : String.Format("{0:n0}", item.Price);
                otherOffers.ThumbnailImageUrl = Convert.ToString(item.Thumbnail);
                otherOffers.Ar_ThumbnailImageUrl = Convert.ToString(item.Ar_Thumbnail);
                if (otherOffersList.Any(x => x.RoomType == otherOffers.RoomType) != true)
                    otherOffersList.Add(otherOffers);
            }
            return otherOffersList;
        }

        public List<ImageURLs> GetWebRoomImages(int roomTypeId)
        {
            return uow.GenericRepository<RoomTypeDetail>().Table
                .Where(x => x.RoomTypeID == roomTypeId)
                .OrderBy(x => x.DisplayOrder)
                .OrderBy(x => x.ID)
                .Select(x => new ImageURLs
                {
                    ImageUrl = x.ImageUrl,
                    Description = x.Description
                }).ToList();
        }

        public List<ImageURLs> GetWebRoomImages()
        {
            return uow.GenericRepository<RoomTypeDetail>().Table
                .OrderBy(x => x.DisplayOrder)
                .OrderBy(x => x.ID)
                .Select(x => new ImageURLs
                {
                    ImageUrl = x.ImageUrl,
                    Description = x.Description,
                    RoomTypeId = x.RoomTypeID
                }).ToList();
        }

        public List<ImageURLs> GetWebRoomFeatures(int roomTypeId, string culture)
        {
            return uow.GenericRepository<RoomTypeFeature>().Table
                .Where(x => x.RoomTypeID == roomTypeId)
                .OrderBy(x => x.OrderBy)

                .Select(x => new ImageURLs
                {
                    ImageUrl = x.AllRoomFeature.ImageUrl,
                    Description = culture.StartsWith("en-") ? x.AllRoomFeature.FeatureName : x.AllRoomFeature.Ar_FeatureName
                }).ToList();
        }

        public List<ImageURLs> GetWebRoomFeatures(string culture)
        {
            return uow.GenericRepository<RoomTypeFeature>().Table
                .Select(x => new ImageURLs
                {
                    ImageUrl = x.AllRoomFeature.ImageUrl,
                    Description = culture.StartsWith("en-") ? x.AllRoomFeature.FeatureName : x.AllRoomFeature.Ar_FeatureName,
                    RoomTypeId = x.RoomTypeID
                }).ToList();
        }

        #region Via API Website Booking, invoice, mail 
        public EF.Booking AddNewBooking(BookingVM bookingVM)
        {
            EF.Booking createdBooking = null;

            try
            {
                // Step 1: Create booking in separate transaction
                createdBooking = CreateBookingWithTransaction(bookingVM);

                if (createdBooking == null)
                {
                    throw new Exception("Failed to create booking");
                }

                // Step 2: Create invoice and payment only for credit card bookings (not bank transfers)
                if (bookingVM.PaymentMethodID == (int)PaymentMethodType.CreditCard)
                {
                    try
                    {
                        CreateInvoiceAndPaymentSeparately(bookingVM, createdBooking.PersonID);
                    }
                    catch (Exception invoiceEx)
                    {
                        // Log the invoice creation error but don't fail the booking
                        LogError($"Invoice creation failed for booking {createdBooking.BookingNumber}: {invoiceEx.Message}");
                    }
                }
                else
                {
                    // For bank transfers, no invoice/payment processing needed
                    LogError($"Bank transfer booking {createdBooking.BookingNumber} - No invoice created as per business rules");
                }

                // Step 3: Send confirmation emails (non-critical)
                try
                {
                    SendBookingConfirmationEmail(createdBooking, bookingVM);
                }
                catch (Exception emailEx)
                {
                    // Log email error but don't fail the process
                    LogError($"Email sending failed for booking {createdBooking.BookingNumber}: {emailEx.Message}");
                }

                return createdBooking;
            }
            catch (Exception ex)
            {
                LogError($"Booking creation failed: {ex.Message}");
                return null;
            }
        }

        private EF.Booking CreateBookingWithTransaction(BookingVM bookingVM)
        {
            EF.Booking booking = null;
            EF.Person person = null;

            using (var transaction = uow.Context.Database.BeginTransaction())
            {
                try
                {
                    // Performance optimization
                    var ctx = uow.Context;
                    bool prevAutoDetect = ctx.Configuration.AutoDetectChangesEnabled;
                    bool prevValidate = ctx.Configuration.ValidateOnSaveEnabled;

                    ctx.Configuration.AutoDetectChangesEnabled = false;
                    ctx.Configuration.ValidateOnSaveEnabled = false;

                    try
                    {
                        // Check for existing person
                        var normalizedEmail = (bookingVM.Email ?? string.Empty).Trim().ToLower();
                        var existingPerson = PersonRepo.GetAll()
                            .FirstOrDefault(p => p.LocationId == bookingVM.LocationID &&
                                               p.IsEnable == true &&
                                               p.Email.ToLower() == normalizedEmail);

                        if (existingPerson == null)
                        {
                            person = CreateNewPersonWithBooking(bookingVM, normalizedEmail);
                            booking = person.Bookings.FirstOrDefault();
                        }
                        else
                        {
                            person = existingPerson;
                            booking = CreateBookingForExistingPerson(bookingVM, existingPerson);
                        }

                        // Create payment record
                        var payment = Mapping.MapPayment(bookingVM, booking.BookingID);
                        uow.GenericRepository<EF.Payment>().Insert(payment);
                        uow.SaveChanges();

                        // Create audit logs
                        CreateAuditLogs(person, booking, existingPerson == null);

                        // Commit the booking transaction
                        transaction.Commit();

                        return booking;
                    }
                    finally
                    {
                        // Always restore EF settings
                        ctx.Configuration.AutoDetectChangesEnabled = prevAutoDetect;
                        ctx.Configuration.ValidateOnSaveEnabled = prevValidate;
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception($"Booking creation failed: {ex.Message}", ex);
                }
            }
        }

        private EF.Person CreateNewPersonWithBooking(BookingVM bookingVM, string normalizedEmail)
        {
            var person = Mapping.MapPerson(bookingVM);
            person.Email = normalizedEmail;
            person.Code = GetMaxPersonCode(Convert.ToInt32(bookingVM.LocationID)).ToString();

            var booking = Mapping.MapBooking(bookingVM, person.PersonID, bookingVM.RoomTypePriceID);
            booking.BookingNumber = Common.Globals.GetBookingNumber((int)bookingVM.LocationID);

            person.Bookings.Add(booking);
            PersonRepo.Insert(person);

            // Handle emergency contact
            var emergencyContact = Mapping.MapEmergencyContact(bookingVM, person.PersonID);
            uow.GenericRepository<EF.EmergencyContact>().Insert(emergencyContact);

            // Handle special request
            var specialRequest = Mapping.MapSpecialRequest(bookingVM, person.PersonID);
            uow.GenericRepository<EF.SpecialRequest>().Insert(specialRequest);

            uow.SaveChanges();
            return person;
        }

        private EF.Booking CreateBookingForExistingPerson(BookingVM bookingVM, EF.Person existingPerson)
        {
            var booking = Mapping.MapBooking(bookingVM, existingPerson.PersonID, bookingVM.RoomTypePriceID);
            booking.BookingNumber = Common.Globals.GetBookingNumber((int)bookingVM.LocationID);
            bookingRepo.Insert(booking);

            // Update emergency contact
            UpdateEmergencyContact(bookingVM, existingPerson.PersonID);

            // Update special request
            UpdateSpecialRequest(bookingVM, existingPerson.PersonID);

            uow.SaveChanges();
            return booking;
        }

        private void UpdateEmergencyContact(BookingVM bookingVM, int personId)
        {
            var existingEmergencyContact = uow.GenericRepository<EF.EmergencyContact>().GetAll()
                .FirstOrDefault(ec => ec.PersonID == personId);

            if (existingEmergencyContact != null)
            {
                var updatedEmergencyContact = Mapping.MapEmergencyContact(bookingVM, personId);
                updatedEmergencyContact.ID = existingEmergencyContact.ID;
                uow.Context.Entry(existingEmergencyContact).CurrentValues.SetValues(updatedEmergencyContact);
            }
            else
            {
                var newEmergencyContact = Mapping.MapEmergencyContact(bookingVM, personId);
                uow.GenericRepository<EF.EmergencyContact>().Insert(newEmergencyContact);
            }
        }

        private void UpdateSpecialRequest(BookingVM bookingVM, int personId)
        {
            var existingSpecialRequest = uow.GenericRepository<EF.SpecialRequest>().GetAll()
                .FirstOrDefault(sr => sr.PersonID == personId);

            if (existingSpecialRequest != null)
            {
                var updatedSpecialRequest = Mapping.MapSpecialRequest(bookingVM, personId);
                updatedSpecialRequest.ID = existingSpecialRequest.ID;
                uow.Context.Entry(existingSpecialRequest).CurrentValues.SetValues(updatedSpecialRequest);
            }
            else
            {
                var newSpecialRequest = Mapping.MapSpecialRequest(bookingVM, personId);
                uow.GenericRepository<EF.SpecialRequest>().Insert(newSpecialRequest);
            }
        }

        private void CreateAuditLogs(EF.Person person, EF.Booking booking, bool isNewPerson)
        {
            List<EF.AuditLog> auditLogList = new List<EF.AuditLog>();

            // Create audit log for person if new
            if (isNewPerson)
            {
                var oldPerson = new EF.Person();
                var personDifference = Common.Classes.Common.DetailedCompare<EF.Person>(oldPerson, person);

                var personAuditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Create,
                    ActionId = (int)Enumeration.CorrespondenceAction.CreatePerson,
                    PK = person.PersonID.ToString(),
                    UserId = 1,
                    TableName = "Person",
                    Reference = person.Code.ToString() + " Online from " + booking.Channel,
                    UserName = person.Email,
                    PersonId = person.PersonID,
                    TimeStamp = DateTime.Now,
                    AuditLogDetails = personDifference
                };
                auditLogList.Add(personAuditLog);
            }

            // Create audit log for booking
            var oldBooking = new EF.Booking();
            var bookingDifference = Common.Classes.Common.DetailedCompare<EF.Booking>(oldBooking, booking);

            var bookingAuditLog = new EF.AuditLog()
            {
                AuditType = (int)Enumeration.AuditType.Create,
                ActionId = (int)Enumeration.CorrespondenceAction.CreateBooking,
                PK = booking.BookingID.ToString(),
                UserId = 1,
                TableName = "Booking",
                Reference = booking.BookingNumber + " Online from " + booking.Channel,
                UserName = person.Email,
                PersonId = booking.PersonID,
                TimeStamp = DateTime.Now,
                AuditLogDetails = bookingDifference
            };
            auditLogList.Add(bookingAuditLog);

            auditLogsService.AddAuditLogList(auditLogList);
        }

        private void CreateInvoiceAndPaymentSeparately(BookingVM bookingVM, int personId)
        {
            using (var transaction = uow.Context.Database.BeginTransaction())
            {
                try
                {
                    invoicingService.CreateDepositInvoiceAndPayment(bookingVM, personId);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception($"Invoice and payment creation failed: {ex.Message}", ex);
                }
            }
        }

        private void SendBookingConfirmationEmail(EF.Booking booking, BookingVM bookingVM)
        {
            try
            {
                var detail = roomTypePriceDetail(booking.PriceConfigID);

                if (bookingVM.PaymentMethodID == (int)PaymentMethodType.BankTransfer)
                {
                    SendBankTransferEmail(booking, detail, bookingVM);
                }
                else
                {
                    SendCreditCardEmail(booking, detail, bookingVM);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Email sending failed: {ex.Message}", ex);
            }
        }

        private void SendBankTransferEmail(EF.Booking booking, PMS.EF.V_RoomTypePriceDetail detail, BookingVM bookingVM)
        {
            var emailTemplate = correspondenceService.GetEmailMessagesByActionId(
                (int)Enumeration.CorrespondenceAction.GenerateBookingWithBankTransfer,
                booking.LocationID ?? 0);

            string body = Common.mailbody.BankTransferBooking(booking, detail, emailTemplate.EmailMessageBody);
            string recipients = GetEmailRecipients(booking.Person.Email, bookingVM.Prefix);

            emailService.SendEmail(
                Convert.ToString(emailTemplate.EmailMessageSubject),
                body,
                true,
                recipients,
                emailTemplate.EmailMessageSenderID);
        }

        private void SendCreditCardEmail(EF.Booking booking, PMS.EF.V_RoomTypePriceDetail detail, BookingVM bookingVM)
        {
            var emailTemplate = correspondenceService.GetEmailMessagesByActionId(
                (int)Enumeration.CorrespondenceAction.GenerateBookingWithCreditCard,
                booking.LocationID ?? 0);

            string body = mailbody.CreditCardBooking(booking, detail, emailTemplate.EmailMessageBody);
            string recipients = GetEmailRecipients(booking.Person.Email, bookingVM.Prefix);

            emailService.SendEmail(
                "Booking",
                body,
                true,
                recipients,
                emailTemplate.EmailMessageSenderID);
        }

        private string GetEmailRecipients(string primaryEmail, string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return primaryEmail;
            }

            var ccemail = uow.GenericRepository<University>().Table
                .Where(x => x.Prefix == prefix)
                .Select(x => x.EmailCC)
                .FirstOrDefault();

            return string.IsNullOrEmpty(ccemail) ? primaryEmail : $"{primaryEmail},{ccemail}";
        }

        private void LogError(string message)
        {
            ErrorLogger.WriteToTestingLog("$ERROR:", message);
        }

        #endregion

        public string GetMaxPersonCode(int Locationid)
        {
            int code = 0;
            if (uow.GenericRepository<EF.Person>().Table.Where(x => x.Code != null && x.LocationId == Locationid).Count() != 0)
            {
                var nowithGRn = Convert.ToDecimal(uow.GenericRepository<EF.Person>().Table.Where(x => x.Code != null && x.LocationId == Locationid).AsEnumerable().Select(x => new { Number = Convert.ToDecimal(x.Code.Split('-').Last()) }).Max(x => x.Number)) + 1;
                code = (int)nowithGRn;
            }
            else

            {
                code = 1;
            }

            var data = GetLocationByID(Locationid);
            var maxcode = code;
            string value = String.Format("{0:D4}", maxcode);
            var Code = "PER-" + data.Prefix + "-" + value;
            return Code;
        }

        public List<Currency> GetCurrency()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            return uow.GenericRepository<EF.Currency>()
                      .Table
                      .Where(x => assignedLocationIds.Contains((int)x.LocationId))
                      .ToList();
        }

        public bool IsAlreadyBooked(string email, int LocationId)
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToLower();
            return PersonRepo.Table.Any(x => x.LocationId == LocationId && x.IsEnable == true && x.Email.ToLower() == normalizedEmail);
        }

        public LocationSettingsApiVM GetLocationSettingByLocationid(int id, string culture)
        {
            var locationSetting = uow.GenericRepository<LocationSetting>().Table.FirstOrDefault(x => x.LocationId == id);
            if (locationSetting == null)
                return null;
            LocationSettingsApiVM settingsApiVM = new LocationSettingsApiVM();
            if (culture.ToLower().Contains("ar-"))
            {
                settingsApiVM.LocationId = locationSetting.LocationId;
                settingsApiVM.RegistrationFee = locationSetting.RegistrationFee;

                settingsApiVM.CodeOfConduct = (locationSetting.CodeOfConduct_AR == null) ?
                     locationSetting.CodeOfConduct_EN : locationSetting.CodeOfConduct_AR;

                settingsApiVM.TermsAndCondition = (locationSetting.TermsAndCondition_AR == null) ?
                     locationSetting.TermsAndCondition_EN : locationSetting.TermsAndCondition_AR;
            }
            else
            {
                settingsApiVM.LocationId = locationSetting.LocationId;
                settingsApiVM.RegistrationFee = locationSetting.RegistrationFee;
                settingsApiVM.CodeOfConduct = locationSetting.CodeOfConduct_EN;
                settingsApiVM.TermsAndCondition = locationSetting.TermsAndCondition_EN;
            }
            return settingsApiVM;
        }

        public List<GetAvailableRoomsForLandingPage_Result> GetAvailableRoomsForLandingPage(int LocationId, string university)
        {
            return uow.Context.GetAvailableRoomsForLandingPage(LocationId, university).ToList();
        }

        public List<FrequencyVm> GetFrequency()
        {
            return uow.GenericRepository<EF.VehiclePriceLookUp>().Table.Where(x => x.Status == true).Select(x => new FrequencyVm
            {
                Id = x.Id,
                Name = x.PriceRate
            }).ToList();
        }

        public V_RoomTypePriceDetail roomTypePriceDetail(int RoomTypePriceId)
        {
            return uow.Context.V_RoomTypePriceDetail.Where(x => x.RoomTypePriceId == RoomTypePriceId).FirstOrDefault();
        }

        public LocationSetting paymentGateway(int LocationId/*, string id*/)
        {
            return uow.Context.LocationSettings.Where(x => x.LocationId == LocationId /*&& x.PaymentGateWay == id*/).FirstOrDefault();
        }

        public List<UniversitiesVM> GetUniversityListByLoactionIdAPI(int id, string culture, string university)
        {
            bool originalLazyLoadingState = uow.Context.Configuration.LazyLoadingEnabled;
            uow.Context.Configuration.LazyLoadingEnabled = true;

            try
            {
                var query = uow.GenericRepository<University>().Table
                    .Where(x => x.LocationId == id && x.IsEnable && x.IsActive);

                if (!string.IsNullOrWhiteSpace(university))
                {
                    query = query.Where(x => x.Prefix == university);
                }

                var list = query.ToList();

                var model = list.Select(item => new UniversitiesVM
                {
                    Id = item.Id,
                    UniversityName = culture.Contains("ar-") && !string.IsNullOrEmpty(item.UniversityArabicName)
                        ? item.UniversityArabicName
                        : item.UniversityName,
                    Prefix = item.Prefix
                }).ToList();

                return model;
            }
            finally
            {
                uow.Context.Configuration.LazyLoadingEnabled = originalLazyLoadingState;
            }
        }
        public bool CheckMyriadIdAndEmail(string myriadID, string email)
        {
            try
            {
                var person = uow.GenericRepository<EF.Person>().Table.Any(x => x.Code == myriadID && x.Email == email);

                return person;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in checking person data: " + ex.Message);
            }
        }

        public RefundRequest RefundRequest(RefundRequestVM requestVM)
        {
            requestVM.SendEmail = "refunds@mct.themyriad.com";
            var person = uow.GenericRepository<EF.Person>().Table.Where(x => x.Code == requestVM.Code).FirstOrDefault();
            RefundRequest ret = new RefundRequest();

            try
            {
                uow.CreateTransaction();
                RefundRequest refundRequest = new RefundRequest
                {
                    MyriadID = requestVM.Code,
                    Email = requestVM.Email,
                    AccountNumber = requestVM.AccountNumber,
                    BankAccount = requestVM.BankAccount,
                    IFSCCode = requestVM.IFSCCode,
                    Signature = requestVM.Signature,
                    PersonID = person.PersonID,
                    CreatedDate = DateTime.Now
                };

                uow.GenericRepository<RefundRequest>().Insert(refundRequest);
                uow.SaveChanges();
                uow.Commit();
                //ErrorLogger.WriteToTestingLog("", "------------------entry inserted-------------------");

                try
                {
                    var emailTemplate = correspondenceService.GetEmailMessagesByActionId((int)Enumeration.CorrespondenceAction.GenerateRefundRequest, person.LocationId ?? 0);
                    if (emailTemplate != null)
                    {
                        string body = Common.mailbody.RefundRequest(refundRequest, emailTemplate.EmailMessageBody);
                        emailService.SendEmail(Convert.ToString(emailTemplate.EmailMessageSubject), body, true, requestVM.SendEmail, emailTemplate.EmailMessageSenderID);
                    }

                    var studentEmailTemplate = correspondenceService.GetEmailMessagesByActionId((int)Enumeration.CorrespondenceAction.SendRefundRequestToStudent, person.LocationId ?? 0);

                    if (studentEmailTemplate != null)
                    {
                        string body = Common.mailbody.RefundRequest(refundRequest, studentEmailTemplate.EmailMessageBody);

                        emailService.SendEmail(Convert.ToString(emailTemplate.EmailMessageSubject), body, true, requestVM.Email, emailTemplate.EmailMessageSenderID);
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }

                return refundRequest;
            }
            catch (Exception ex)
            {
                uow.Rollback();
                return ret;
            }
        }

    }
}
