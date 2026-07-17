using PMS.DTO.ViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.JobConfuguration
{
    public class JobConfigurationService:IJobConfigurationService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        public JobConfigurationService(UnitOfWork<PMSEntities> _uow)
        {
            uow = _uow;
        }
        public async Task<List<JobConfigurationVM>> GetAll(int CategoryId)
        {
            var List =uow.GenericRepository<EF.JobConfiguration>().Table.Where(x => x.CategoryId == CategoryId)
                .Select(x => new JobConfigurationVM
            {
                    Id=x.Id,
                    CategoryId=x.CategoryId,
                    PropertyValue=x.PropertyValue,
                    PropertyName=x.PropertyName,
                    FirstExecutuonOn=x.FirstExecutionOn,
                    FirstExecutionActionId=(int)x.FirstExecutionActionId,
                    SecondExecutionACtionId=(int)x.SecondExecutionActionId,
                    SecondExecutionOn=x.SecondExecutionOn,
                    IsEmail=x.IsEmail,
                    IsNotify=x.IsNotify
            }).ToList();
            return List;
        }
        public List<DropDownViewModel> GetActiveJobAction()
        {
            var response = uow.GenericRepository<JobActionLookup>().Table.Where(x => x.Status == true).Select(x => new DropDownViewModel
            {
                Id = x.Id,
                Name = x.Name
            }).ToList();
            return response;


        }
        public bool Add(List<JobConfigurationVM> model)
        {
            try
            {
                if (model.Count > 0)
                {
                    var categoryId = model.FirstOrDefault().CategoryId;
                    var previouseList=uow.GenericRepository<JobConfiguration>().Table.Where(x=>x.CategoryId== categoryId).ToList();
                    foreach(var item in previouseList)
                    {
                        uow.GenericRepository<JobConfiguration>().Delete(item);
                    }
                    foreach (var item in model)
                    {
                        var Job = new JobConfiguration
                        {
                            CategoryId = item.CategoryId,
                            FirstExecutionOn = item.FirstExecutuonOn,
                            FirstExecutionActionId = item.FirstExecutionActionId,
                            SecondExecutionActionId = item.SecondExecutionACtionId,
                            SecondExecutionOn = item.SecondExecutionOn,
                            IsEmail = item.IsEmail,
                            IsNotify = item.IsNotify,
                            PropertyName = item.PropertyName,
                            PropertyValue = item.PropertyValue,
                            CreatedOn = DateTime.Now
                        };
                        uow.GenericRepository<JobConfiguration>().Insert(Job);
                    }
                    uow.SaveChanges();
                }
                return true;
            }catch(Exception ex)
            {
                return false;
            }
        }
        public List<JobConfigurationVM> GetById(int Id)
        {
          var model=  uow.GenericRepository<JobConfiguration>().Table.Where(x => x.CategoryId == Id).Select(x => new JobConfigurationVM 
            { 
                CategoryId=x.CategoryId,
                FirstExecutionActionId=(int)x.FirstExecutionActionId,
                FirstExecutuonOn=x.FirstExecutionOn,
                SecondExecutionACtionId=(int)x.SecondExecutionActionId,
                SecondExecutionOn=x.SecondExecutionOn,
                PropertyValue=x.PropertyValue,
                IsEmail=x.IsEmail,
                IsNotify=x.IsNotify

            }).ToList();

            return model;
        }
    }
}
