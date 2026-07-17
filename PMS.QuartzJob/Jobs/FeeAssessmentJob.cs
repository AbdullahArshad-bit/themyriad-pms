using PMS.Services.Services.FeeAssessment;
using PMS.Services.Services.Invoicings;
using PMS.Services.Services.Person;
using PMS.Services.Services.Service;
using PMS.Services.Services.Tax;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.QuartzJob.Jobs
{
    //public class FeeAssessmentJob : IJob
    //{
    //    private readonly IInvoicingService invoicingService;
    //    private readonly IPersonService personService;
    //    private readonly IServicesService servicesService;
    //    private readonly ITaxService taxService;
    //    private readonly IFeeAssessmentJobService feeAssessmentJobService;

    //    public FeeAssessmentJob(IInvoicingService invoicingService, IPersonService personService, IServicesService servicesService, ITaxService taxService, IFeeAssessmentJobService feeAssessmentJobService)
    //    {
    //        this.invoicingService = invoicingService;
    //        this.personService = personService;
    //        this.servicesService = servicesService;
    //        this.taxService = taxService;
    //        this.feeAssessmentJobService = feeAssessmentJobService;
    //    }
    //}
}
