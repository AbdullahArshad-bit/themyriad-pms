using PMS.DTO.ViewModels.FeedbackViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.Feedback
{
    public interface IFeedbackService
    {
        List<FeedbackViewModels> GetQuestions();
        bool Add(FeedbackViewModels feedbackViewModels);
        bool check(int Personid, int placementid);
        List<FeedbackDetailVM> GetRating(int personid, int placementid);

    }
}
