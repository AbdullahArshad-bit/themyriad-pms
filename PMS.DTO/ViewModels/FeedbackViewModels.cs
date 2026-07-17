using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels.FeedbackViewModels
{
    public class FeedbackViewModels
    {
        public int QuestionId { get; set; }
        public string Question { get; set; }
        public int FeddbackId { get; set; }
        public int PersonId { get; set; }
        public int PlacementId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int FeedbackDetailId { get; set; }
        public int rating { get; set; }
        public string description { get; set; }
        public string PersonName { get; set; }
        public string Email { get; set; }
        public int LocationId { get; set; }
        public List<FeedbackDetailVM> FeedbackDetailVM{get;set;}
    }
    public class QuestionVM
    {
        public int QuestionId { get; set; }
        public string Question { get; set; }
    }
    public class FeedbackDetailVM
    {
        public string Question { get; set; }
        public int QuestionId { get; set; }
        public int rating { get; set; }
        public string description { get; set; }
        public int PersonId { get; set; }
        public int PlacementId { get; set; }


    }
}
