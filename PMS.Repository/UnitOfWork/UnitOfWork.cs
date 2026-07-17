using PMS.Repository.Repositories.Generic;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Repository.UnitOfWork
{
    public class UnitOfWork<TContext> : IUnitOfWork<TContext>, IDisposable where TContext : DbContext, new()
    {
        private readonly TContext _context;
        private bool _disposed;
        private string _errorMessage = string.Empty;
        private DbContextTransaction _objTran;
        private Dictionary<string, object> _repositories;

        public UnitOfWork()
        {
            
            _context = new TContext();
            _context.Configuration.LazyLoadingEnabled = true;
        }

        public void Dispose()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }
        public TContext Context
        {
            get { return _context; }
        }

        public void CreateTransaction()
        {
            _objTran = _context.Database.BeginTransaction();
        }

        public void Commit()
        {
            _objTran.Commit();
        }

        public void Rollback()
        {
            _objTran.Rollback();
            _objTran.Dispose();
        }

        public void SaveChanges()
        {
            try
            { _context.SaveChanges(); }
            catch (DbEntityValidationException dbEx)
            {
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        _errorMessage += string.Format(
                            "Property: {0} Error: {1}",
                            validationError.PropertyName,
                            validationError.ErrorMessage
                            )
                            + Environment.NewLine;
                    }
                }

                throw new Exception(_errorMessage, dbEx);
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
                if (disposing)
                    _context.Dispose();
            _disposed = true;
        }
        public GenericRepository<T> GenericRepository<T>() where T : class
        {
            if (_repositories == null)
                _repositories = new Dictionary<string, object>();

            var type = typeof(T).Name;
            //DisplayTypeInfo(typeof(T));
            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(GenericRepository<T>);
                var repositoryInstanse = Activator.CreateInstance(repositoryType, _context);

                //var repositoryInstanse = Activator.CreateInstance(
                //    repositoryType.MakeGenericType(typeof(T)),
                //    _context);
                //_repositories.Add(type, repositoryInstanse);

                _repositories.Add(type, repositoryInstanse);
            }

            return (GenericRepository<T>)_repositories[type];
        }
        private static void DisplayTypeInfo(Type t)
        {
            Console.WriteLine("\r\n{0}", t);

            Console.WriteLine("\tIs this a generic type definition? {0}",
                t.IsGenericTypeDefinition);

            Console.WriteLine("\tIs it a generic type? {0}",
                t.IsGenericType);

            Type[] typeArguments = t.GetGenericArguments();
            Console.WriteLine("\tList type arguments ({0}):", typeArguments.Length);
            foreach (Type tParam in typeArguments)
            {
                Console.WriteLine("\t\t{0}", tParam);
            }
        }
    }
}
