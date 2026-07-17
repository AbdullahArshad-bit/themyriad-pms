using PMS.Common.Security;
using PMS.DTO.ViewModels.FeedbackViewModels;
using PMS.Services.Services.Feedback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace PMS.Areas.Feedback.Controllers
{
    public class FeedbackSettingController : Controller
    {
        private IFeedbackService feedbackService;
        // GET: Feedback/FeedbackSetting
        public FeedbackSettingController(IFeedbackService _feedbackService)
        {
            feedbackService = _feedbackService;
        }
        public ActionResult Index()
        {
            return View();
        }
        [AllowAnonymous]
        public ActionResult AddFeedback(string encIds)
        {

            string decryptUrl = PMS.Common.Security.StringCipher.DecryptFeedback(encIds);
            string[] hList = decryptUrl.Split(',');
            var PersonId = hList[0].ToString();
            var PlacementId = hList[1].ToString();
            bool res = feedbackService.check(Int16.Parse(PersonId),Int16.Parse(PlacementId));
            if(res == true)
            {
                return RedirectToAction("FeedbackAlreadySubmitted");
            }
            else
            {
            ViewBag.PersonID = hList[0].ToString();
            ViewBag.PlacementId = hList[1].ToString();

            //var personId = StringCipher.Decrypt()
            var questions = feedbackService.GetQuestions();
            ViewBag.QuestionId = questions;
            }
            return View();
        }
        [AllowAnonymous]
        [HttpPost]
        public ActionResult AddFeedback(FeedbackViewModels feedbackViewModels)
        {
            bool res = feedbackService.Add(feedbackViewModels);
            return Json(new { status = true }, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        public ActionResult FeedbackSubmitted()
        {
            return View();
        }
        [AllowAnonymous]
        public ActionResult FeedbackAlreadySubmitted()
        {
            return View();
        }
        public ActionResult ShowComment(int personId,int placementid)
        {
            var res = feedbackService.GetRating(personId,placementid);
            if(res.Count==0)
            {
                ViewBag.error = "Feedback against this student is not submitted yet";
            }
            ViewBag.Rating = res;
            return View();
        }
        public ActionResult FeedbackEmail(string encIds)
        {
            string[] hList = encIds.Split(',');
            var PersonId = hList[0].ToString();
            var PlacementId = hList[1].ToString();

            var res = feedbackService.GetRating(Int16.Parse(PersonId), Int16.Parse(PlacementId));
            ViewBag.Rating = res;
            return View();
        }

    }
}