using System;
using System.Configuration;
using System.Web;

namespace PMS.Classes
{
    public static class StudentAuthRedirectHelper
    {
        public const string DefaultStudentReturnUrl = "/Student/Home/Index";

        public static string BuildWebSignInUrl(string token, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var baseUrl = ResolveBaseUrl();
            var safeReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? DefaultStudentReturnUrl : returnUrl.Trim();

            return string.Format(
                "{0}/Account/StudentAppSignIn?token={1}&returnUrl={2}",
                baseUrl,
                Uri.EscapeDataString(token),
                Uri.EscapeDataString(safeReturnUrl));
        }

        private static string ResolveBaseUrl()
        {
            var baseUrl = ConfigurationManager.AppSettings["BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                return baseUrl.Trim().TrimEnd('/');
            }

            var request = HttpContext.Current?.Request;
            if (request?.Url != null)
            {
                return request.Url.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            }

            return string.Empty;
        }
    }
}
