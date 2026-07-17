using PMS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.Account
{
    public interface IAccountService
    {
        User AuthenticateUser(string email, string password);
        User GetStudentUserForSession(int userId);

        bool ChangePassword(string oldpassword, string ConfirmPassword);
    }
}
