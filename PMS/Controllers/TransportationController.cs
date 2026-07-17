//using PMS.Common.Classes;
//using PMS.Common.Filters;
//using PMS.DTO.ViewModels.TransportationViewModels;
//using PMS.Services.Services.StudentPortal.Transportation;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;
//using System.Web.Mvc;

//namespace PMS.Controllers
//{
//    public class TransportationController : BaseController
//    {
//        ITransportationService transportationService;
//        public TransportationController(ITransportationService _transportationService)
//        {
//            transportationService = _transportationService;
//        }

//        [AuthorizeUser(Roles = AppUserRoles.transportation)]
//        [Route("Add-New-schedule")]
//        public ActionResult AddNew(int ScheduleID = 0)
//        {
//            AddScheduleVM addSchedule = new AddScheduleVM();
//            if (ScheduleID > 0)
//            {
//                var obj = transportationService.GetScheduleByID(ScheduleID);

//                if (obj != null)
//                {
//                    //addSchedule.ScheduleID = obj.ScheduleID;
//                    //addSchedule.ScheduleName = obj.ScheduleName;
//                    //addSchedule.CreatedDate = DateTime.Now;
//                    //addSchedule.UpdatedDate = DateTime.Now;
//                }

//                return View("AddNew", addSchedule);
//            }
//            return View();
//        }

//        [AuthorizeUser(Roles = AppUserRoles.transportation)]
//        [Route("All-Schedules")]
//        public ActionResult AllSchedules()
//        {
//            var AllSchedules = transportationService.GetAllScheduleDetail();
//            var AllRoutesDetail = transportationService.GetAllScheduleDetail();
//            ViewBag.allschedule = AllSchedules;
//            return View();
//        }

//        [AuthorizeUser(Roles = AppUserRoles.transportation)]
//        [Route("Root-Details")]
//        public ActionResult RootDetails(int ScheduleID = 0)
//        {
//            var ScheduleRouteDetails = transportationService.GetAllRouteDetail(ScheduleID);
//            ViewBag.scheduleRouteDetails = ScheduleRouteDetails;

//            return View();
//        }

//        [AuthorizeUser(Roles = AppUserRoles.transportation)]
//        [Route("Single-Root-Details")]
//        public ActionResult SingleRootDetails(int RouteID = 0)
//        {
//            var SingleRouteDetails = transportationService.GetSingleRouteDetail(RouteID);
//            ViewBag.singleRouteDetails = SingleRouteDetails;

//            return View();
//        }

//        [AuthorizeUser(Roles = AppUserRoles.transportation)]
//        [Route("Add-New-Root")]
//        public ActionResult AddNewRoot()
//        {
//            var AllSchedules = transportationService.GetAllScheduleDetail();
//            //ViewBag.allschedule = AllSchedules;

//            var allschedule = AllSchedules.ToList();

//            ViewBag.allschedule = new SelectList(allschedule, "ScheduleID", "ScheduleName");

//            return View();
//        }


//        [AuthorizeUser(Roles = AppUserRoles.transportation)]
//        [HttpGet]
//        [Route("Edit-Route-Stop-Time")]
//        public ActionResult EditRouteStopTime(int RouteStopID = 0)
//        {
//            UpdateRouteTimeVM routeTimeVM = new UpdateRouteTimeVM();

//            if (RouteStopID > 0)
//            {
//                var obj = transportationService.GetStopByID(RouteStopID);

//                routeTimeVM.RouteID = obj.RouteID;
//                routeTimeVM.RouteStopID = obj.RouteStopID;
//                routeTimeVM.StopID = obj.StopID;
//                routeTimeVM.StopNumber = obj.StopNumber;
//                routeTimeVM.StopTime = obj.StopTime;

//                return View("EditRouteStopTime", routeTimeVM);
//            }

//            return View();
//        }


//        [AuthorizeUser(Roles = AppUserRoles.transportation)]
//        [HttpPost]
//        public ActionResult UPDATEroute(UpdateRouteTimeVM updateRouteTime)
//        {
//            UpdateRouteTimeVM routeTimeVM = new UpdateRouteTimeVM();
//            if (updateRouteTime.RouteStopID > 0)
//            {
//                bool result = transportationService.UpdateStopByID(updateRouteTime);

//                if (result != false)
//                {
//                    TempData["success"] = "Time updated succesfully";
//                    return RedirectToAction("AllSchedules");
//                }
//            }

//            ViewBag.error = "Something went wrong, Time not updated.";
//            return RedirectToAction("AllSchedules");
//        }
//        [HttpPost]

//        public ActionResult DeleteRoute(int RouteID)
//        {
//            bool result = transportationService.RemoveRoute(RouteID);

//            return RedirectToAction("AllSchedules");
//        }

//        [AuthorizeUser(Roles = AppUserRoles.transportation)]
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public ActionResult DeleteSchedule(int ScheduleID)
//        {
//            bool result = transportationService.RemoveSchedule(ScheduleID);

//            if (result != false)
//            {
//                TempData["success"] = "Schedule added succesfully";
//                return RedirectToAction("AllSchedules");
//            }

//            ViewBag.error = "Something went wrong, Schedule not removed.";
//            return RedirectToAction("AllSchedules");
//        }


//        [AuthorizeUser(Roles = AppUserRoles.transportation)]
//        [HttpPost]

//        public ActionResult AddRoute(AddRouteVM addRouteVM)
//        {
//            bool result = transportationService.AddRoute(addRouteVM);
//            return RedirectToAction("AllSchedules");
//        }

//        [AuthorizeUser(Roles = AppUserRoles.transportation)]
//        [HttpPost]
//        public ActionResult AddSchedule(AddScheduleVM addScheduleVM)
//        {

//            if (addScheduleVM.ScheduleName != null && addScheduleVM.ScheduleID == 0)
//            {
//                var obj = transportationService.GetScheduleByName(addScheduleVM.ScheduleName);

//                if (obj == null)
//                {


//                    addScheduleVM.CreatedDate = DateTime.Now;
//                    addScheduleVM.UpdatedDate = DateTime.Now;
//                    addScheduleVM.IsEnable = true;

//                    bool check = transportationService.AddScheduleByName(addScheduleVM);


//                    if (check != false)
//                    {
//                        TempData["success"] = "Schedule added succesfully";
//                        return RedirectToAction("AllSchedules");

//                    }
//                    else
//                    {
//                        ViewBag.error = "Something went wrong, Schedule not saved.";
//                    }
//                }


//            }
//            else if (addScheduleVM.ScheduleName != null && addScheduleVM.ScheduleID > 0)
//            {
//                var obj = transportationService.GetScheduleByID(addScheduleVM.ScheduleID);

//                if (obj != null)
//                {




//                    bool check = transportationService.UpdateSchedule(addScheduleVM.ScheduleID, addScheduleVM.ScheduleName);


//                    if (check != false)
//                    {
//                        TempData["success"] = "Schedule updated succesfully";
//                        return RedirectToAction("AllSchedules");

//                    }
//                    else
//                    {
//                        ViewBag.error = "Something went wrong, Schedule not updated.";
//                    }
//                }
//            }

//            return View("AddNew", addScheduleVM);
//        }

//        [AuthorizeUser(Roles = AppUserRoles.transportation)]
//        [HttpPost]
//        public ActionResult EditSchedule(int ScheduleID)
//        {
//            var obj = transportationService.GetScheduleByID(ScheduleID);

//            if (obj != null)
//            {

//                return View("AddNew", obj);


//            }
//            else
//            {
//                return ViewBag.error = "Something went wrong, please try again";
//            }
//        }
//    }
//}