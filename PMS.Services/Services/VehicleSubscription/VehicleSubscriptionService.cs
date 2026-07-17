using PMS.Common.Classes;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.TransportationViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.LocationContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static PMS.Common.Classes.Enumeration;

namespace PMS.Services.Services.VehicleSubscription
{
    public class VehicleSubscriptionService : IVehicleSubscriptionService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly ILocationContextService locationContextService;
        public VehicleSubscriptionService(UnitOfWork<PMSEntities> _uow, ILocationContextService _locationContextService)
        {
            uow = _uow;
            locationContextService = _locationContextService;
        }
        public bool Add(VehicleSubscriptionVM vehicleSubscriptionVM)
        {
            var subscriptions = uow.GenericRepository<EF.VehicleSubscription>().Table.Where(x => x.StudentId == vehicleSubscriptionVM.StudentID && (x.SubscriptionStatus == (int)Enumeration.SubscriptionsStatusLookup.Active || x.SubscriptionStatus == (int)Enumeration.SubscriptionsStatusLookup.Suspended || x.SubscriptionStatus == (int)Enumeration.SubscriptionsStatusLookup.Pending) && x.IsEnable == true).Select(x => x.FrequencyId).FirstOrDefault();
            if (subscriptions != 0)
            {
                //var subscriptions = uow.GenericRepository<EF.VehicleSubscription>().Table.Where(x=>x.FrequencyId == vehicleSubscriptionVM.FrequencyId && x.StudentId == vehicleSubscriptionVM.StudentID && (x.SubscriptionStatus == (int)Enumeration.SubscriptionsStatusLookup.Active || x.SubscriptionStatus == (int)Enumeration.SubscriptionsStatusLookup.Suspended || x.SubscriptionStatus == (int)Enumeration.SubscriptionsStatusLookup.Pending) && x.IsEnable == true).FirstOrDefault();
                //if (subscriptions != null)
                //{
                    throw new Exception("There is already a Package against this student");
                //}
            }
            

            try
            {
                var subscription = new EF.VehicleSubscription()
                {
                    SubscriptionID = vehicleSubscriptionVM.SubscriptionId,
                    StudentId = vehicleSubscriptionVM.StudentID,
                    VehiclePriceId = vehicleSubscriptionVM.VehiclePriceID,
                    FromDate = vehicleSubscriptionVM.FromDate,
                    ToDate = vehicleSubscriptionVM.ToDate,
                    FrequencyId = vehicleSubscriptionVM.FrequencyId,
                    SubscriptionPrice = vehicleSubscriptionVM.SubscriptionPrice,
                    CreatedBy = PMS.Common.Globals.User.Email,
                    CreatedDate = DateTime.Now,
                    SubscriptionStatus = (int)SubscriptionsStatusLookup.Pending,
                    IsEnable = true,
                    LocationId = vehicleSubscriptionVM.LocationId
                };
                uow.GenericRepository<EF.VehicleSubscription>().Insert(subscription);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public List<VehicleSubscriptionVM> GetAll()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var res = uow.GenericRepository<EF.VehicleSubscription>().GetAll(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId)).Select(x => new VehicleSubscriptionVM
            {
                SubscriptionId = x.SubscriptionID,
                StudentID = x.StudentId,
                VehiclePriceID = x.VehiclePriceId,
                FromDate = x.FromDate,
                ToDate = x.ToDate,
                CreatedDate = x.CreatedDate,
                CreatedBy = x.CreatedBy,
                SubscriptionStatus = x.SubscriptionStatus,
                IsEnable = x.IsEnable,
                StudentName = x.Person.FullName,
                PriceName = x.VehiclePrice.PriceName,
                SubscriptionPrice = x.SubscriptionPrice,
                FrequencyName = x.VehiclePriceLookUp.PriceRate,
                LocationName = x.Location.LocationName
            }).ToList();
            return res;
        }
        public VehicleSubscriptionVM GetById(int id)
        {

            var vehicleSubscription = uow.GenericRepository<EF.VehicleSubscription>().Table.Where(x => x.SubscriptionID == id).Select(x => new VehicleSubscriptionVM
            {

                SubscriptionId = x.SubscriptionID,
                StudentID = x.StudentId,
                VehiclePriceID = x.VehiclePriceId,
                FromDate = x.FromDate,
                ToDate = x.ToDate,
                SubscriptionPrice = x.SubscriptionPrice,
                FrequencyId = x.FrequencyId,
                LocationId = x.LocationId
            }).FirstOrDefault();
            return vehicleSubscription;
        }
        public List<VehiclePriceVM> GetPriceNameByFrequency(int id)
        {
            var prices = uow.GenericRepository<EF.VehiclePrice>().Table.Where(x => x.FrequencyId == id && x.IsActive == true && x.IsEnable == true).Select(x => new VehiclePriceVM
            {
                VehiclePriceId = x.VehiclePriceId,
                PriceName = x.PriceName,

            }).ToList();
            return prices;
        }
        public VehiclePriceVM GetPricesByPriceId(int id)
        {
            var prices = uow.GenericRepository<EF.VehiclePrice>().Table.Where(x => x.VehiclePriceId == id && x.IsActive == true && x.IsEnable == true).Select(x => new VehiclePriceVM
            {
                VehiclePriceId = x.VehiclePriceId,
                Price = x.Price,
                FrequencyId = x.FrequencyId

            }).FirstOrDefault();
            return prices;
        }
        public bool Update(VehicleSubscriptionVM vehicleSubscriptionVM)
        {
            //var subscriptions = uow.GenericRepository<EF.VehicleSubscription>().Table.Where(x => x.FrequencyId == vehicleSubscriptionVM.FrequencyId &&x.SubscriptionID!=vehicleSubscriptionVM.SubscriptionId &&x.StudentId == vehicleSubscriptionVM.StudentID && x.IsEnable == true).Any();
            //if (subscriptions)
            //{
            //    throw new Exception("There is already a Package against this student");
            //}
            try
            {
                var res = uow.GenericRepository<EF.VehicleSubscription>().Table.Where(x => x.SubscriptionID == vehicleSubscriptionVM.SubscriptionId).FirstOrDefault();
                res.StudentId = vehicleSubscriptionVM.StudentID;
                res.VehiclePriceId = vehicleSubscriptionVM.VehiclePriceID;
                res.FromDate = vehicleSubscriptionVM.FromDate;
                res.ToDate = vehicleSubscriptionVM.ToDate;
                res.UpdatedBy = PMS.Common.Globals.User.Email;
                res.UpdatedDate = DateTime.Now;
                res.FrequencyId = vehicleSubscriptionVM.FrequencyId;

                res.SubscriptionPrice = vehicleSubscriptionVM.SubscriptionPrice;
                res.LocationId = vehicleSubscriptionVM.LocationId;
                uow.GenericRepository<EF.VehicleSubscription>().Update(res);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool Delete(int Id)
        {
            try
            {
                var res = uow.GenericRepository<EF.VehicleSubscription>().Table.Where(x => x.SubscriptionID == Id).FirstOrDefault();
                res.IsEnable = false;
                res.SubscriptionStatus = (int)SubscriptionsStatusLookup.Ended;
                uow.GenericRepository<EF.VehicleSubscription>().Update(res);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public OutputInvoicingVM GetStudentPackage(int studentId)
        {

            var res = uow.GenericRepository<EF.VehicleSubscription>().Table.Where(x => x.StudentId == studentId && x.FrequencyId == 2 && x.SubscriptionStatus == (int)Enumeration.SubscriptionsStatusLookup.Active).Select(x => new OutputInvoicingVM
            {
                ServicePrice = x.SubscriptionPrice,
                Occupancy = x.VehiclePrice.PriceName

            }).FirstOrDefault();
            return res;
        }
        public bool Approve(int subscriptionid)
        {
            try
            {
                var res = uow.GenericRepository<EF.VehicleSubscription>().Table.Where(x => x.SubscriptionID == subscriptionid).FirstOrDefault();
                res.SubscriptionStatus = (int)SubscriptionsStatusLookup.Active;
                uow.GenericRepository<EF.VehicleSubscription>().Update(res);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool Suspend(int subscriptionid)
        {
            try
            {
                var res = uow.GenericRepository<EF.VehicleSubscription>().Table.Where(x => x.SubscriptionID == subscriptionid).FirstOrDefault();
                res.SubscriptionStatus = (int)SubscriptionsStatusLookup.Suspended;
                uow.GenericRepository<EF.VehicleSubscription>().Update(res);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool End(int subscriptionid)
        {
            try
            {
                var res = uow.GenericRepository<EF.VehicleSubscription>().Table.Where(x => x.SubscriptionID == subscriptionid).FirstOrDefault();
                res.SubscriptionStatus = (int)SubscriptionsStatusLookup.Ended;
                uow.GenericRepository<EF.VehicleSubscription>().Update(res);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        //for student portal
        public List<VehicleSubscriptionVM> GetSubscription(int personid)
        {
            var res = uow.GenericRepository<EF.VehicleSubscription>().Table.Where(x => x.StudentId == personid && x.SubscriptionStatus== (int)Enumeration.SubscriptionsStatusLookup.Active).Select(x => new VehicleSubscriptionVM
            {
                SubscriptionId=x.SubscriptionID,
                SubscriptionStatus=x.SubscriptionStatus
            }).ToList();
            return res;
        }
    }
}
