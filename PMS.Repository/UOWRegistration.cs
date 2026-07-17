using Ninject;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Repository
{
    public static class UOWRegistration
    {
        internal static IKernel Globalkernel { get; private set; }
        public static void BindAll(IKernel kernel)
        {
            kernel.Bind<PMSEntities>().ToSelf();
            kernel.Bind<IUnitOfWork<PMSEntities>>().To<UnitOfWork<PMSEntities>>();
            kernel.Bind<UnitOfWork<PMSEntities>>().To<UnitOfWork<PMSEntities>>();
            //ServiceRegistration.BindAll(kernel);

            Globalkernel = kernel;
        }
    }
}
