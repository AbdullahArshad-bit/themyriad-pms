using Microsoft.IdentityModel.Tokens;
using PMS.Common;
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

namespace PMSAPI.Classes
{
    /// <summary>
    /// Authorization filter for integration endpoints. Validates JWT and restores
    /// the logged-in PMS user into request context (same as staff web app).
    /// </summary>
    public class ApiAuthorize : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            var auth = actionContext.Request.Headers.Authorization;
            if (auth == null || string.IsNullOrWhiteSpace(auth.Parameter))
            {
                return false;
            }

            var token = AuthHelper.VerifyAndDecodeJwt(auth.Parameter);
            if (token == null)
            {
                return false;
            }

            AuthHelper.SetCurrentUserFromToken(token);
            return true;
        }
    }

    public static class AuthHelper
    {
        private static string Key => ConfigurationManager.AppSettings["Jwt:key"];
        private static string Issuer => ConfigurationManager.AppSettings["Jwt:Issuer"];

        public static int DefaultExpireMinutes
        {
            get
            {
                int minutes;
                return int.TryParse(ConfigurationManager.AppSettings["Jwt:ExpireMinutes"], out minutes) ? minutes : 60;
            }
        }

        /// <summary>
        /// Generates a JWT for a PMS user authenticated via email/password.
        /// </summary>
        public static string GenerateToken(User user, int expireMinutes)
        {
            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
            var tokenHandler = new JwtSecurityTokenHandler();
            var now = DateTime.UtcNow;
            var assignedLocations = user.AssignedLocations ?? new List<int>();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
                new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? user.Username ?? string.Empty),
                new Claim("Id", user.ID.ToString()),
                new Claim("assigned_locations", string.Join(",", assignedLocations))
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = now.AddMinutes(expireMinutes),
                SigningCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256Signature),
                Issuer = Issuer,
                Audience = Issuer
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public static string GenerateTokenForClient(string clientId, int expireMinutes)
        {
            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
            var tokenHandler = new JwtSecurityTokenHandler();
            var now = DateTime.UtcNow;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "0"),
                new Claim(ClaimTypes.Name, clientId ?? string.Empty),
                new Claim(ClaimTypes.Email, clientId ?? string.Empty),
                new Claim("Id", "0"),
                new Claim("client_id", clientId ?? string.Empty)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = now.AddMinutes(expireMinutes),
                SigningCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256Signature),
                Issuer = Issuer,
                Audience = Issuer
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
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
                    ValidIssuer = Issuer,
                    ValidAudience = Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key))
                };

                SecurityToken validToken;
                new JwtSecurityTokenHandler().ValidateToken(accessToken, validationParameters, out validToken);
                return validToken as JwtSecurityToken;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Makes Globals.User / location filtering work for API requests.
        /// </summary>
        public static void SetCurrentUserFromToken(JwtSecurityToken token)
        {
            if (token == null || HttpContext.Current == null)
            {
                return;
            }

            var userIdClaim = token.Claims.FirstOrDefault(c => c.Type == "Id" || c.Type == ClaimTypes.NameIdentifier)?.Value;
            int userId;
            if (!int.TryParse(userIdClaim, out userId))
            {
                return;
            }

            var assignedLocationsClaim = token.Claims.FirstOrDefault(c => c.Type == "assigned_locations")?.Value;
            var assignedLocations = new List<int>();
            if (!string.IsNullOrWhiteSpace(assignedLocationsClaim))
            {
                assignedLocations = assignedLocationsClaim
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x =>
                    {
                        int id;
                        return int.TryParse(x.Trim(), out id) ? id : 0;
                    })
                    .Where(x => x > 0)
                    .ToList();
            }

            var email = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            HttpContext.Current.Items["User"] = new User
            {
                ID = userId,
                Name = name,
                Email = email,
                Username = email,
                AssignedLocations = assignedLocations
            };
        }
    }
}
