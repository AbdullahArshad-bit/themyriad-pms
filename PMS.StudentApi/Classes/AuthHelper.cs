using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace PMS.StudentApi.Classes
{
    public class ApiAuthorize : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            var auth = actionContext.Request.Headers.Authorization;

            if (auth != null)
            {
                var token = auth.Parameter;
                if (AuthHelper.VerifyAndDecodeJwt(token) != null)
                {
                    return true;
                };
            }
            return false;
        }
    }
    public class AuthHelper
    {
        public static string GenerateToken(string email, int UserId, int expireMinutes = 2)
        {

            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["Jwt:Key"]));
            var tokenHandler = new JwtSecurityTokenHandler();

            var now = DateTime.UtcNow;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.Email, email),
                new Claim("Id", UserId.ToString())
                 }),

                Expires = now.AddMinutes(Convert.ToInt32(expireMinutes)),

                SigningCredentials = new SigningCredentials(
                    symmetricKey,
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = ConfigurationManager.AppSettings["Jwt:Issuer"],
                Audience = ConfigurationManager.AppSettings["Jwt:Issuer"]
            };
            var stoken = tokenHandler.CreateToken(tokenDescriptor);
            var token = tokenHandler.WriteToken(stoken);

            return token;
        }

        public static JwtSecurityToken VerifyAndDecodeJwt(string accessToken)
        {
            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = ConfigurationManager.AppSettings["Jwt:Issuer"],
                    ValidAudience = ConfigurationManager.AppSettings["Jwt:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["Jwt:Key"]))
                };
                new JwtSecurityTokenHandler().ValidateToken(accessToken, validationParameters, out var validToken);
                // threw on invalid, so...
                return validToken as JwtSecurityToken;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}