using PMS.Common.Classes;
using PMS.DTO.ViewModels.FeedbackViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.Correspondence;
using PMS.Services.Services.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.Services.Services.Feedback
{
    public class FeedbackService : IFeedbackService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly ICorrespondenceService correspondenceService;
        private readonly IEmailService emailService;
        public FeedbackService(UnitOfWork<PMSEntities> _uow, ICorrespondenceService _correspondenceService, IEmailService _emailService)
        {
            uow = _uow;
            correspondenceService = _correspondenceService;
            emailService = _emailService;

        }

        public List<FeedbackViewModels> GetQuestions()
        {
            var questions = uow.GenericRepository<EF.Question>().Table.Select(x => new FeedbackViewModels
            {

                QuestionId = x.Id,
                Question = x.Question1

            }).ToList();
            return questions;
        }
        public bool Add(FeedbackViewModels feedbackViewModels)
        {
            feedbackViewModels.Email = "feedback@themyriad.com";
            //feedbackViewModels.Email = "sheikzain1355@gmail.com";
            var personid = feedbackViewModels.FeedbackDetailVM.Select(x => x.PersonId).FirstOrDefault();
            var placementid = feedbackViewModels.FeedbackDetailVM.Select(x => x.PlacementId).FirstOrDefault();
            var person = uow.GenericRepository<EF.Booking>().Table.Where(x => x.PersonID == personid).Select(x => new FeedbackViewModels
            {
                PersonName = x.Person.FullName,
                LocationId = x.LocationID??0
            }).FirstOrDefault();
            try
            {
                foreach (var item in feedbackViewModels.FeedbackDetailVM)
                {
                    var feedback = new EF.Feedback()
                    {
                        PlacementId = item.PlacementId,
                        PersonID = item.PersonId,
                        QuestionId = item.QuestionId,
                        Description = item.description,
                        Rating = item.rating
                    };
                    uow.GenericRepository<EF.Feedback>().Insert(feedback);
                    uow.SaveChanges();
                }
                var NotifyEmail = correspondenceService.GetEmailMessagesByActionId((int)Enumeration.CorrespondenceAction.FeedbackEmail,person.LocationId);
                if (NotifyEmail != null)
                {
                    var Request = HttpContext.Current.Request;

                    var body = NotifyEmail.EmailMessageBody;
                    body = body.Replace("[[Subject]]", person.PersonName);
                    body = body.Replace("{{ConfirmationLink}}", Request.Url.GetLeftPart(UriPartial.Authority) + Request.ApplicationPath + "Feedback/FeedbackSetting/FeedbackEmail?encIds=" + personid + "," + placementid);

                    emailService.SendEmailAsync(Convert.ToString(NotifyEmail.EmailMessageSubject), body, true, feedbackViewModels.Email, NotifyEmail.EmailMessageSenderID);

                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool check(int Personid, int placementid)
        {
            var res = uow.GenericRepository<EF.Feedback>().GetAll().Where(x => x.PersonID == Personid && x.PlacementId == placementid).FirstOrDefault();
            if (res != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public List<FeedbackDetailVM> GetRating(int personid, int placementid)
        {
            var res = uow.GenericRepository<EF.Feedback>().Table.Where(x => x.PersonID == personid && x.PlacementId == placementid).Select(x => new FeedbackDetailVM
            {
                Question = x.Question.Question1,
                description = x.Description,
                rating = x.Rating

            }).ToList();
            return res;
        }


    }
}
