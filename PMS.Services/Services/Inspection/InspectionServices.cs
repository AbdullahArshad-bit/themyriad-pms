using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using PMS.DTO.ViewModels.InspectionViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;

namespace PMS.Services.Services.Inspection
{
    public class InspectionServices : IInspectionService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        public InspectionServices(UnitOfWork<PMSEntities> _uow)
        {
            uow = _uow;
        }


        //ratingList
        public List<RatingListVM> GetRating()
        {
            List<RatingListVM> result = (from a in uow.GenericRepository<EF.InspectionRatingList>().Table.Where(x => x.IsEnable == true)
                                         select new RatingListVM
                                         {
                                             InspectionRatingListID = a.InspectionRatingListID,
                                             Name = a.InspectionName,
                                             IsActive = a.IsActive,

                                         }).Where(x => x.IsActive == true).ToList();
            return result;

            //return uow.GenericRepository<EF.InspectionRatingList>().Table.Where(x => x.IsEnable == true)
            //   .Select(x => new RatingListVM
            //   {
            //       InspectionRatingListID = x.InspectionRatingListID,
            //       Name = x.InspectionName,
            //       IsActive = x.IsActive,
            //   }).ToList();
        }

        public AddRatingListVM GetRatingById(int id)
        {
            //var record = uow.Context.InspectionRatingLists.FirstOrDefault(x => x.IsEnable == true);
            AddRatingListVM result;

            var db = uow.Context;
            result = (from a in db.InspectionRatingLists
                      where a.InspectionRatingListID == id && a.IsEnable == true
                      select new AddRatingListVM
                      {
                          InspectionRatingListID = a.InspectionRatingListID,
                          Name = a.InspectionName,
                          IsActive = a.IsActive,
                          RatingListItem = a.InspectionRatingListItems.Select(x => new RatingListItem
                          {
                              RatingListItemDetailID = x.InspectionRatingListItemID,
                              RatingListItemName = x.InspectionRatingListItemName,
                              RatingListItemDescripttion = x.InspectionRatingListItemDescription,
                              RatingListItemPercent = x.InspectionRatingListItemPercentOfCharge
                          }).ToList()

                      }).FirstOrDefault();

            return result;
        }

        public bool AddRatingList(AddRatingListVM model)
        {
            //insert insepectionList
            EF.InspectionRatingList tbl = new EF.InspectionRatingList
            {
                InspectionName = model.Name,
                IsActive = model.IsActive,
                CreatedDate = DateTime.Now,
                CreatedBy = model.CreatedBy,
                IsEnable = true
            };
            uow.GenericRepository<EF.InspectionRatingList>().Insert(tbl);
            uow.SaveChanges();
            var id = tbl.InspectionRatingListID;

            //insert ratingListItem
            foreach (var item in model.RatingListItem)
            {
                EF.InspectionRatingListItem subTbl = new EF.InspectionRatingListItem
                {
                    InspectionRatingListItemName = item.RatingListItemName,
                    InspectionRatingListID = id,
                    InspectionRatingListItemDescription = item.RatingListItemDescripttion,
                    InspectionRatingListItemPercentOfCharge = item.RatingListItemPercent
                };
                uow.GenericRepository<EF.InspectionRatingListItem>().Insert(subTbl);
                uow.SaveChanges();
            }
            //return tbl;
            return true;
        }

        public bool UpdateRatingList(AddRatingListVM model)
        {

            EF.InspectionRatingList tbl = uow.GenericRepository<EF.InspectionRatingList>().GetById(model.InspectionRatingListID);

            if (tbl != null)
            {
                tbl.InspectionName = model.Name;
                tbl.IsActive = model.IsActive;
                tbl.UpdatedDate = DateTime.Now;
                tbl.UpdatedBy = model.UpdatedBy;

                uow.GenericRepository<EF.InspectionRatingList>().Update(tbl);
                var id = tbl.InspectionRatingListID;



                //update ratingListItem

                var removeRatings = uow.GenericRepository<InspectionRatingListItem>().Table.Where(x => x.InspectionRatingListID == model.InspectionRatingListID);
                foreach (var item in model.RatingListItem)
                {
                    EF.InspectionRatingListItem subtbl = uow.GenericRepository<EF.InspectionRatingListItem>().GetById(item.RatingListItemDetailID);
                    if (subtbl != null)
                    {
                        removeRatings = removeRatings.Where(x => x.InspectionRatingListItemID != item.RatingListItemDetailID);


                        subtbl.InspectionRatingListItemName = item.RatingListItemName;
                        subtbl.InspectionRatingListID = id;
                        subtbl.InspectionRatingListItemDescription = item.RatingListItemDescripttion;
                        subtbl.InspectionRatingListItemPercentOfCharge = item.RatingListItemPercent;

                        uow.GenericRepository<EF.InspectionRatingListItem>().Update(subtbl);
                    }
                    else
                    {
                        EF.InspectionRatingListItem subTbl = new EF.InspectionRatingListItem
                        {
                            InspectionRatingListItemName = item.RatingListItemName,
                            InspectionRatingListID = id,
                            InspectionRatingListItemDescription = item.RatingListItemDescripttion,
                            InspectionRatingListItemPercentOfCharge = item.RatingListItemPercent
                        };
                        uow.GenericRepository<EF.InspectionRatingListItem>().Insert(subTbl);
                    }

                }

                foreach (var item in removeRatings)
                {
                    uow.GenericRepository<InspectionRatingListItem>().Delete(item);
                }
                uow.SaveChanges();
                return true;
            }
            else
            {
                throw new Exception("Rating List not found to update");
            }
        }

        public bool DeleteInspection(int id)
        {
            EF.InspectionRatingList tbl = uow.GenericRepository<EF.InspectionRatingList>().GetById(id);

            if (tbl != null)
            {
                tbl.IsEnable = false;

                uow.GenericRepository<EF.InspectionRatingList>().Update(tbl);
                uow.SaveChanges();

                return true;
            }
            else
            {
                throw new Exception("Inspection not found to delete.");
            }
        }

        public bool DeleteGenerateInspection(int id)
        {
            EF.GenerateInspection tbl = uow.GenericRepository<EF.GenerateInspection>().GetById(id);

            if (tbl != null)
            {
                tbl.IsEnable = false;

                uow.GenericRepository<EF.GenerateInspection>().Update(tbl);
                uow.SaveChanges();

                return true;
            }
            else
            {
                throw new Exception("Inspection not found to delete.");
            }
        }
        //inspectionfields

        public List<RatingListItem> getRatingListItemsById(int id)
        {
            var db = uow.Context;
            List<RatingListItem> result = (from a in db.InspectionRatingListItems
                                           where a.InspectionRatingListID == id
                                           select new RatingListItem
                                           {
                                               RatingListItemName = a.InspectionRatingListItemName,
                                               RatingListItemDescripttion = a.InspectionRatingListItemDescription,
                                               RatingListItemPercent = a.InspectionRatingListItemPercentOfCharge,
                                               RatingListItemDetailID = a.InspectionRatingListItemID
                                           }).ToList();
            if (result != null)
            {
                return result;
            }
            else
            {
                throw new Exception("Rating List not found");
            }
        }

        public List<InspectionFieldsVM> getInspectionFields()
        {
            var db = uow.Context;

            var result = (from a in db.InspectionFields
                          join b in db.InspectionRatingLists
                          on a.InspectionRatingListID equals b.InspectionRatingListID
                          where a.IsEnable == true
                          select new InspectionFieldsVM
                          {
                              INspectionFieldID = a.InspectionFieldsID,
                              ModelName = a.ModelName,
                              ShortLabel = a.ShortLabel,
                              ratingList = b.InspectionName,
                              IsActive = a.IsActive,
                              ratingListID = a.InspectionRatingListID
                          }).ToList();
            return result;
        }


        public List<InpectionFieldsSelected> getInspectionFieldsSelected()
        {
            return uow.GenericRepository<InpectionFieldsSelected>().GetAll().ToList();
        }

        public AddInspectionFieldsVM getInspectionFieldById(int id)
        {
            var db = uow.Context;

            var result = (from a in db.InspectionFields
                          join b in db.InspectionRatingLists
                          on a.InspectionRatingListID equals b.InspectionRatingListID
                          where a.InspectionFieldsID == id
                          select new AddInspectionFieldsVM
                          {
                              INspectionFieldID = a.InspectionFieldsID,
                              ModelName = a.ModelName,
                              ShortLabel = a.ShortLabel,
                              LongLabel = a.LongLabel,
                              ratingListItemID = a.InspectionRatingListID,
                              ratingList = b.InspectionName,
                              IsActive = a.IsActive,
                              AllowNotes = a.AllowNotes,
                              AllowImages = a.AllowImage,
                              AllowExternalIdentifier = a.AllowExternalIdentifier,
                              AssociatedWithMonetary = a.AssociatedWithMonetary
                          }).Where(x => x.IsActive == true).FirstOrDefault();
            return result;
        }

        public bool AddInspectionFIeld(AddInspectionFieldsVM model)
        {
            //insert insepectionList
            EF.InspectionField tbl = new EF.InspectionField
            {
                ModelName = model.ModelName,
                IsActive = model.IsActive,
                CreatedDate = DateTime.Now,
                CreatedBy = model.CreatedBy,
                ShortLabel = model.ShortLabel,
                LongLabel = model.LongLabel,
                InspectionRatingListID = model.ratingListItemID,
                IsEnable = true,
                AllowNotes = model.AllowNotes,
                AllowImage = model.AllowImages,
                AllowExternalIdentifier = model.AllowExternalIdentifier,
                AssociatedWithMonetary = model.AssociatedWithMonetary
            };
            uow.GenericRepository<EF.InspectionField>().Insert(tbl);
            uow.SaveChanges();
            return true;
        }

        public bool UpdateInspectionField(AddInspectionFieldsVM model)
        {
            EF.InspectionField tbl = uow.GenericRepository<EF.InspectionField>().GetById(model.INspectionFieldID);

            if (tbl != null)
            {
                tbl.ModelName = model.ModelName;
                tbl.IsActive = model.IsActive;
                tbl.ShortLabel = model.ShortLabel;
                tbl.InspectionRatingListID = model.ratingListItemID;
                tbl.UpdatedDate = DateTime.Now;
                tbl.UpdatedBy = model.UpdatedBy;
                tbl.AllowNotes = model.AllowNotes;
                tbl.AllowImage = model.AllowImages;
                tbl.AllowExternalIdentifier = model.AllowExternalIdentifier;
                tbl.AssociatedWithMonetary = model.AssociatedWithMonetary;

                uow.GenericRepository<EF.InspectionField>().Update(tbl);
            }
            uow.SaveChanges();

            return true;
        }

        public bool DeleteInspectionField(int id)
        {
            EF.InspectionField tbl = uow.GenericRepository<EF.InspectionField>().GetById(id);

            if (tbl != null)
            {
                tbl.IsEnable = false;

                uow.GenericRepository<EF.InspectionField>().Update(tbl);
                uow.SaveChanges();

                return true;
            }
            else
            {
                throw new Exception("Inspection field not found to delete.");
            }
        }

        //inspections

        public List<InspectionsVM> getInspections()
        {
            var db = uow.Context;
            var result = (from a in db.Inspections
                          join b in db.InspectionTypes on a.InspectionType equals b.InspctionTypeID
                          join c in db.InspectionToCompareAgainsts on a.InspectionToCompareAgainst equals c.InspectionCompareAgainstID
                          where a.IsEnable == true
                          select new InspectionsVM
                          {
                              InspectionName = a.InspectionName,
                              InspectionType = b.InspectionTypeName,
                              InspectionID = a.InspectionID,
                              CompareTo = c.InpsectionToCompareAgainst,
                              IsActive = a.IsActive,
                          }).ToList();
            return result;
        }

        public List<MaintenanceVM> MaintenanceRequest()
        {


            var check = uow.GenericRepository<InspectionRatingData>().Table.ToList().Where(x => x.IsActive == true).ToList();
            List<MaintenanceVM> MVM = new List<MaintenanceVM>();
            foreach (var item in check)
            {
                MaintenanceVM maintenance = new MaintenanceVM();

                string inspectionName = uow.GenericRepository<GenerateInspection>().Table.Where(x => x.ID == item.GeneratedInspectionID).FirstOrDefault().Inspection.InspectionName;


                maintenance.GeneratedInspectionID = item.GeneratedInspectionID;
                maintenance.InspectionName = inspectionName;
                maintenance.Note = item.RatingNote;
                maintenance.Image = item.RatingimageUrl;

                MVM.Add(maintenance);
            }


            //var result = (from a in db.InspectionRatingDatas
            //              join b in db.GenerateInspections on a.GeneratedInspectionID equals b.InspectionID
            //              //join c in db.InspectionRatingLists on a.ratingListID equals c.InspectionRatingListID
            //              where a.IsEnable == true
            //              select new MaintenanceVM
            //              {
            //                  GeneratedInspectionID = a.GeneratedInspectionID,
            //                  InspectionName = b.Inspection.InspectionName,
            //                  Note = a.RatingNote,
            //                  Image = a.RatingimageUrl,
            //              }).ToList();
            return MVM;
        }

        public InspectionViewVM GenerateViewRequest(int id)
        {
            var data = uow.GenericRepository<GenerateInspection>().Table.Where(x => x.IsEnable == true).Where(x => x.ID == id).FirstOrDefault();

            InspectionViewVM MNT = new InspectionViewVM();


            MNT.InspectionName = data.Inspection.InspectionName;
            MNT.CreatedDate = data.CreatedDate;
            MNT.EffectiveDays = data.Inspection.InspectionValidForDays;
            MNT.BedSpace = data.BedSpace.BedName;
            MNT.MaintananceRemarks = data.Maintenance_Remarks;

            string staffstatus = uow.GenericRepository<Status>().Table.Where(x => x.ID == data.Staff_Status).FirstOrDefault().Name;
            MNT.StaffStatus = staffstatus;

            string studentstatus = uow.GenericRepository<Status>().Table.Where(x => x.ID == data.Student_Status).FirstOrDefault().Name;
            MNT.StudentStatus = studentstatus;

            string maintannancestatus = uow.GenericRepository<Status>().Table.Where(x => x.ID == data.Maintenance_Status).FirstOrDefault().Name;
            MNT.MaintannaceStatus = maintannancestatus;

            var savedData = uow.GenericRepository<InspectionRatingData>().Table.Where(x => x.GeneratedInspectionID == id).Where(x => x.IsActive == true).ToList();



            List<InspectionList> ift = new List<InspectionList>();



            foreach (var item in savedData)
            {
                InspectionList list = new InspectionList();

                list.FieldName = item.InspectionField.ModelName;
                list.SelectedRating = item.InspectionRatingListItem.InspectionRatingListItemName;
                list.Note = item.RatingNote;
                list.RecomendedCharge = item.InspectionRatingListItem.InspectionRatingListItemPercentOfCharge.ToString();
                list.ImageAttached = item.RatingimageUrl;

                ift.Add(list);
            }

            MNT.IFL = ift;


            return MNT;
        }
        public List<Status> getStaus()
        {
            return uow.GenericRepository<Status>().GetAll().ToList();
        }


        public AddInspectionsVM getInspectionById(int id)
        {
            var db = uow.Context;
            var result = (from a in db.Inspections
                          where a.InspectionID == id
                          select new AddInspectionsVM
                          {
                              InspectionName = a.InspectionName,
                              InspectionTypeInt = a.InspectionType,
                              InspectionID = a.InspectionID,
                              CompareToInt = a.InspectionToCompareAgainst,
                              InspectionDescription = a.InspectionDescription,
                              IsActive = a.IsActive,
                              CreatedBy = a.CreatedBy,
                              InspectionValidDays = a.InspectionValidForDays,
                              InspectionFieldForSlect = a.InpectionFieldsSelecteds.Select(x => new InspectionFieldForSlect
                              {
                                  InspectionFieldSelctID = x.InspectionFieldID,
                                  inspectionID = x.InspectionID,
                                  InspectionFieldSelectedValue = x.InspectionFieldValue
                              }).ToList()
                          }).FirstOrDefault();
            if (result != null)
            {
                return result;
            }
            else
            {
                throw new Exception("Inspection not found");
            }
        }

        public bool AddInspectionsVM(AddInspectionsVM model)
        {
            //insert insepectionList
            EF.Inspection tbl = new EF.Inspection
            {
                InspectionName = model.InspectionName,
                InspectionDescription = model.InspectionDescription,
                IsActive = model.IsActive,
                IsEnable = true,
                InspectionValidForDays = model.InspectionValidDays,
                InspectionToCompareAgainst = model.CompareToInt,
                InspectionType = model.InspectionTypeInt,
                CreatedDate = model.CreatedDate,
                CreatedBy = model.CreatedBy

            };
            uow.GenericRepository<EF.Inspection>().Insert(tbl);
            uow.SaveChanges();
            var id = tbl.InspectionID;

            //insert ratingListItem
            foreach (var item in model.InspectionFieldForSlect)
            {
                EF.InpectionFieldsSelected subTbl = new EF.InpectionFieldsSelected
                {
                    InspectionID = id,
                    InspectionFieldValue = Convert.ToInt32(item.InspectionFieldSelectedValue)
                };
                uow.GenericRepository<EF.InpectionFieldsSelected>().Insert(subTbl);
                uow.SaveChanges();
            }
            //return tbl;
            return true;
        }

        public bool GenerateInspectionsVM(GenetateInspectionVM model)
        {
            try
            {
                //insert Generate insepection List
                EF.GenerateInspection tbl = new EF.GenerateInspection
                {
                    InspectionID = model.InspectionID,
                    BedSpaceID = model.BedSpaceID,
                    Remarks = model.Remarks,
                    Staff_Status = model.Staff_Status,
                    Student_Status = model.Student_Status,
                    Maintenance_Status = model.Maintenance_Status,
                    CreatedDate = model.CreatedDate,
                    CreatedBy = model.CreatedBy,
                    IsEnable = model.IsEnable

                };
                uow.GenericRepository<EF.GenerateInspection>().Insert(tbl);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;

            }
        }

        public bool ADDInspectionDetails(InspectionRatingDataVM model)
        {
            try
            {
                //insert Generate insepection List
                EF.InspectionRatingData tbl = new EF.InspectionRatingData
                {
                    ID = model.ID,
                    AssignedFieldID = model.AssignedFieldID,
                    ratingListID = model.ratingListID,
                    GeneratedInspectionID = model.GeneratedInspectionID,
                    SelectetRatinglistitemID = model.SelectetRatinglistitemID,
                    RatingNote = model.RatingNote,
                    RatingimageUrl = model.RatingimageUrl,
                    IsEnable = model.IsEnable,
                    IsActive = model.IsActive


                };
                uow.GenericRepository<EF.InspectionRatingData>().Insert(tbl);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;

            }
        }

        public List<GenetatedInspectionsVM> getGeneratedInspections()
        {
            var db = uow.Context;
            var result = (from a in db.GenerateInspections
                          join p in db.Status
                          on a.Maintenance_Status equals p.ID
                          into per
                          from status in per.DefaultIfEmpty()
                          where a.IsEnable == true

                          select new GenetatedInspectionsVM
                          {
                              ID = a.ID,
                              InspectionID = a.InspectionID,
                              InspectionName = a.Inspection.InspectionName,
                              InspectionType = a.Inspection.InspectionType1.InspectionTypeName,
                              BedSpaceName = a.BedSpace.BedName,
                              BedSpaceID = a.BedSpaceID,
                              CreatedDate = a.CreatedDate,
                              CreatedBy = a.CreatedBy,
                              UpdatedDate = a.UpdatedDate,
                              UpdatedBy = a.UpdatedBy,
                              Staff_Status = a.Status.Name,
                              Student_Status = a.Status1.Name,
                              Maintenance_Status = a.Status2.Name,
                              Remarks = a.Remarks,

                              Maintenance_Remarks = a.Maintenance_Remarks
                          }).ToList();
            return result;
        }


        public GenerateInspection getGeneratedInspectionsbyid(int id)
        {
            try
            {

                var result = uow.GenericRepository<GenerateInspection>().GetAll().Where(x => x.ID == id).FirstOrDefault();
                if (result.InspectionID > 0)
                {
                    return result;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                return null;

            }


        }
        public GenerateInspection UpdateGenerateInspectionsVM(UpdateGenetatedInspectionsVM model)
        {
            try
            {
                var GeneratedInspections = getGeneratedInspectionsbyid(model.ID);

                GeneratedInspections.ID = model.ID;

                if (model.Staff_Status != 0)
                    GeneratedInspections.Staff_Status = model.Staff_Status;


                if (model.Student_Status != 0)
                    GeneratedInspections.Student_Status = model.Student_Status;


                if (model.Maintenance_Status != 0)
                    GeneratedInspections.Maintenance_Status = model.Maintenance_Status;


                if (model.Maintenance_Remarks != null)
                    GeneratedInspections.Maintenance_Remarks = model.Maintenance_Remarks;

                GeneratedInspections.UpdatedDate = DateTime.Now;
                GeneratedInspections.UpdatedBy = Common.Globals.User.Email;

                uow.GenericRepository<EF.GenerateInspection>().Update(GeneratedInspections);
                uow.SaveChanges();
                return GeneratedInspections;
            }

            catch (Exception ex)
            {
                return null;

            }

        }




        public bool UpdateInspection(AddInspectionsVM model)
        {
            EF.Inspection tbl = uow.GenericRepository<EF.Inspection>().GetById(model.InspectionID);

            if (tbl != null)
            {
                tbl.InspectionName = model.InspectionName;
                tbl.InspectionDescription = model.InspectionDescription;
                tbl.IsActive = model.IsActive;
                tbl.InspectionValidForDays = model.InspectionValidDays;
                tbl.InspectionToCompareAgainst = model.CompareToInt;
                tbl.InspectionType = model.InspectionTypeInt;
                //tbl.CreatedDate = model.CreatedDate;
                //tbl.CreatedBy = model.CreatedBy;
                tbl.UpdateDate = model.UpdatedDate;
                tbl.UpdatedBy = model.UpdatedBy;

                uow.GenericRepository<EF.Inspection>().Update(tbl);

                var id = tbl.InspectionID;

                var removeInspectionFields = uow.GenericRepository<InpectionFieldsSelected>().Table.Where(x => x.InspectionID == model.InspectionID);

                foreach (var item in model.InspectionFieldForSlect)
                {
                    EF.InpectionFieldsSelected subtbl = uow.GenericRepository<EF.InpectionFieldsSelected>().GetById(item.InspectionFieldSelctID);
                    if (subtbl != null)
                    {
                        removeInspectionFields = removeInspectionFields.Where(x => x.InspectionFieldID != item.InspectionFieldSelctID);


                        subtbl.InspectionFieldValue = item.InspectionFieldSelectedValue;
                        subtbl.InspectionID = id;

                        uow.GenericRepository<EF.InpectionFieldsSelected>().Update(subtbl);
                    }
                    else
                    {
                        EF.InpectionFieldsSelected subTbl = new EF.InpectionFieldsSelected
                        {
                            InspectionID = id,
                            InspectionFieldValue = Convert.ToInt32(item.InspectionFieldSelectedValue),
                            InspectionFieldID = Convert.ToInt32(item.InspectionFieldID)
                        };
                        uow.GenericRepository<EF.InpectionFieldsSelected>().Insert(subTbl);
                    }
                }

                foreach (var item in removeInspectionFields)
                {
                    uow.GenericRepository<InpectionFieldsSelected>().Delete(item);
                }
                uow.SaveChanges();
                return true;
            }
            else
            {
                throw new Exception("Inspection not found to update");
            }
        }

        public bool DeleteInspections(int id)
        {
            EF.Inspection tbl = uow.GenericRepository<EF.Inspection>().GetById(id);

            if (tbl != null)
            {
                tbl.IsEnable = false;

                uow.GenericRepository<EF.Inspection>().Update(tbl);
                uow.SaveChanges();

                return true;
            }
            else
            {
                throw new Exception("Inspection not found to delete.");
            }
        }

        public List<DTO.ViewModels.InspectionViewModels.InspectionTypeVM> getInspectionTypes()
        {
            var db = uow.Context;
            var result = (from a in db.InspectionTypes
                          select new InspectionTypeVM
                          {
                              InspectionTypeID = a.InspctionTypeID,
                              InspectionTypeName = a.InspectionTypeName
                          }).ToList();
            return result;
        }

        public List<DTO.ViewModels.InspectionViewModels.InspectionToCompareAgainstVM> getInspectionToCompareAgainst()
        {
            var db = uow.Context;
            var result = (from a in db.InspectionToCompareAgainsts
                          select new InspectionToCompareAgainstVM
                          {
                              InspectionComapareAgainstID = a.InspectionCompareAgainstID,
                              InspectionCompareAgainstName = a.InpsectionToCompareAgainst
                          }).ToList();
            return result;
        }



    }
}
