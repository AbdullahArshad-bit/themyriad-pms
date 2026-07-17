using PMS.Common;
using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.EF;
using PMS.Common.Classes;
using PMS.Services.Services.AuditLogs;

namespace PMS.Services.Services.Account
{
    public class AccountService : IAccountService
    {
        private readonly IAuditLogsService auditLogsService;
        private readonly UnitOfWork<PMS.EF.PMSEntities> uow;
        public AccountService(UnitOfWork<PMS.EF.PMSEntities> _uow, IAuditLogsService _auditLogsService)
        {
            auditLogsService = _auditLogsService;
            uow = _uow;
        }
        public User AuthenticateUser(string email, string password)
        {
            string encryptedPassword = Common.Security.StringCipher.Encrypt(password);
            var u = uow.GenericRepository<UserMaster>().Table.Where(x => x.IsEnable == true 
            && x.Username.Equals(email) 
            && x.Password.Equals(encryptedPassword)).FirstOrDefault();

            if (u != null)
            {
                if (u.IsActive)
                {
                    //Audit Logs for Record Login
                    {

                        EF.AuditLog auditLog = new EF.AuditLog()
                        {
                            AuditType = (int)Enumeration.AuditType.Read,
                            ActionId = (int)Enumeration.CorrespondenceAction.UserLoggedIn,
                            PK = u.ID.ToString(),
                            UserId = u.ID,
                            TableName = "UserLogin",
                            UserName = u.FullName + " - " + u.Email,
                        };
                        auditLogsService.AddAuditLog(auditLog);
                    };

                    return MapUserMaster(u);
                }

                else
                    throw new Exception("Unable to login. User is inactive.");
            }
            else
                throw new Exception("Invalid email or password.");
        }

        public User GetStudentUserForSession(int userId)
        {
            var u = uow.GenericRepository<UserMaster>().Table
                .FirstOrDefault(x => x.ID == userId && x.IsEnable == true && x.IsActive && x.IsStudent == true);

            return u == null ? null : MapUserMaster(u);
        }

        private static User MapUserMaster(UserMaster u)
        {
            var assignedLocations = new List<int>();
            if (!string.IsNullOrWhiteSpace(u.AssignedLocation))
            {
                assignedLocations = u.AssignedLocation.Split(',').Select(int.Parse).ToList();
            }

            return new User
            {
                ID = u.ID,
                Name = u.FullName,
                Username = u.Username,
                Password = u.Password,
                Phone = u.Phone,
                Email = u.Email,
                DOB = u.DOB?.Date,
                CreatedDate = u.CreatedAt.Date,
                IsStudent = u.IsStudent == null ? false : u.IsStudent,
                PersonId = u.PersonID ?? 0,
                ImageUrl = u.ImageUrl,
                AssignedLocations = assignedLocations
            };
        }

        public bool ChangePassword(string oldpassword, string ConfirmPassword)
        {

            string dcryptedOldPassword = oldpassword;
            string dcryptedcurrentPassword = Common.Security.StringCipher.Decrypt(Common.Globals.User.Password);
            string encryptedPassword = Common.Security.StringCipher.Encrypt(ConfirmPassword);
            if (dcryptedOldPassword.Equals(dcryptedcurrentPassword))
            {
                var user = uow.GenericRepository<UserMaster>().Table.Where(x => x.ID == Common.Globals.User.ID).FirstOrDefault();
                user.Password = encryptedPassword;
                uow.GenericRepository<UserMaster>().Update(user);
                uow.SaveChanges();

                //Audit Logs for Record Login
                {

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.ChangeUserPassword,
                        PK = user.ID.ToString(),
                        UserId = user.ID,
                        TableName = "ChangePassword",
                        UserName = user.FullName + " - " + user.Email,
                    };
                    auditLogsService.AddAuditLog(auditLog);
                };

                return true;

            }
            else
            {
                return false;
            }
        }
    }
}
