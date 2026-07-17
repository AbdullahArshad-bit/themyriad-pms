using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.EF;
using PMS.DTO.ViewModels.TransportationViewModels;

namespace PMS.Services.Services.StudentPortal.Transportation
{
    class TransportationService : ITransportationService
    {

        private readonly UnitOfWork<PMSEntities> uow;

        public TransportationService(UnitOfWork<PMSEntities> _uow)
        {
            uow = _uow;
        }

        public List<Route> GetAllScheduleDetail(int ScheduleID)
        {
            try
            {
                var SchedueDetails = uow.GenericRepository<Route>().GetAll().Where(x => x.IsEnable == true).ToList();

                return SchedueDetails;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        public List<Route> GetAllRouteDetail(int ScheduleID)
        {
            try
            {
                return uow.GenericRepository<Route>().GetAll().Where(x => x.IsEnable == true).ToList();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public List<RouteStope> GetSingleRouteDetail(int RouteID)
        {
            try
            {
                return uow.GenericRepository<RouteStope>().GetAll().Where(x => x.RouteID == RouteID).ToList();
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public List<EF.Schedule> GetAllScheduleDetail()
        {
            try
            {
                return uow.GenericRepository<EF.Schedule>().GetAll().Where(x => x.IsEnable == true).ToList();
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        public bool RemoveSchedule(int ScheduleID)
        {
            try
            {

                var schedule = uow.GenericRepository<EF.Schedule>().GetAll().Where(x => x.ScheduleID == ScheduleID).FirstOrDefault();

                if (schedule != null)
                {
                    schedule.IsEnable = false;
                    uow.GenericRepository<EF.Schedule>().Update(schedule);
                    uow.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }


        }
        public List<Route> GetDepartureTimeDetail()
        {

            try
            {
                return uow.GenericRepository<Route>().GetAll().Where(x => x.IsEnable == true).ToList();
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public bool RemoveRoute(int RouteID)
        {

            try
            {

                var route = uow.GenericRepository<Route>().GetAll().Where(x => x.RouteID == RouteID).FirstOrDefault();

                if (route != null)
                {
                    route.IsEnable = false;
                    uow.GenericRepository<Route>().Update(route);
                    uow.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public bool AddRoute(AddRouteVM addRouteVM)
        {

            try
            {
                Route Schedule = new Route
                {

                    IsEnable = addRouteVM.IsEnable,
                    DateCreated = addRouteVM.CreatedDate,
                    DepartureTime = addRouteVM.DepartureTime,
                  



                };

                uow.GenericRepository<Route>().Insert(Schedule);
                uow.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }



        public EF.Schedule GetScheduleByName(string scheduleName)
        {

            try
            {
                return uow.GenericRepository<EF.Schedule>().GetAll().Where(x => x.ScheduleName == scheduleName).FirstOrDefault();

            }
            catch (Exception ex)
            {
                return null;
            }

        }

        public EF.Schedule GetScheduleByID(int scheduleID)
        {
            try
            {
                return uow.GenericRepository<EF.Schedule>().GetAll().Where(x => x.ScheduleID == scheduleID).FirstOrDefault();

            }
            catch (Exception ex)
            {
                return null;
            }

        }
        //public bool AddScheduleByName(AddScheduleVM Schedule)
        //{
        //    try
        //    {
        //        EF.Schedule schedule = new EF.Schedule()
        //        {
        //            ScheduleName = Schedule.ScheduleName,
        //            DateCreated = Schedule.CreatedDate,
        //            DateUpdated = Schedule.UpdatedDate,
        //            IsEnable = Schedule.IsEnable
        //        };

        //        uow.GenericRepository<EF.Schedule>().Insert(schedule);
        //        uow.SaveChanges();
        //        return true;

        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }


        //}


        public bool UpdateSchedule(int ScheduleID, string ScheduleName)
        {
            try
            {
                var obj = uow.GenericRepository<EF.Schedule>().GetAll().Where(x => x.ScheduleID == ScheduleID).FirstOrDefault();
                obj.ScheduleName = ScheduleName;
                obj.ScheduleID = ScheduleID;

                uow.GenericRepository<EF.Schedule>().Update(obj);
                uow.SaveChanges();
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }


        }




        public bool UpdateStopByID(UpdateRouteTimeVM updateRouteTimeVM)
        {
            try
            {
                var obj = uow.GenericRepository<RouteStope>().GetAll().Where(x => x.RouteStopID == updateRouteTimeVM.RouteStopID).FirstOrDefault();
                obj.StopTime = updateRouteTimeVM.StopTime;

                uow.GenericRepository<RouteStope>().Update(obj);
                uow.SaveChanges();
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }


        }



        public RouteStope GetStopByID(int RouteStopID)
        {
            try
            {
                var obj = uow.GenericRepository<RouteStope>().GetAll().Where(x => x.RouteStopID == RouteStopID).FirstOrDefault();


                return obj;

            }
            catch (Exception ex)
            {
                return null;
            }


        }

        public bool AddScheduleByName(AddScheduleVM Schedule)
        {
            throw new NotImplementedException();
        }
    }
}
