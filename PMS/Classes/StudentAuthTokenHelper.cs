using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace PMS.Classes
{
    public static class StudentAuthTokenHelper
    {
        private const char Separator = '|';

        public static string GenerateToken(int userId, int personId, string email, int expireMinutes)
        {
            var expiresUtc = DateTime.UtcNow.AddMinutes(expireMinutes);
            var payload = string.Join(Separator.ToString(), userId, personId, email ?? string.Empty, expiresUtc.Ticks);
            var signature = ComputeSignature(payload);
            var token = payload + Separator + signature;
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
        }

        public static bool TryValidateToken(string accessToken, out int userId, out int personId, out string email)
        {
            userId = 0;
            personId = 0;
            email = null;

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return false;
            }

            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(accessToken));
                var parts = decoded.Split(Separator);
                if (parts.Length != 5)
                {
                    return false;
                }

                var payload = string.Join(Separator.ToString(), parts[0], parts[1], parts[2], parts[3]);
                if (!parts[4].Equals(ComputeSignature(payload), StringComparison.Ordinal))
                {
                    return false;
                }

                if (!int.TryParse(parts[0], out userId) || !int.TryParse(parts[1], out personId))
                {
                    return false;
                }

                email = parts[2];
                if (!long.TryParse(parts[3], out var expiryTicks))
                {
                    return false;
                }

                return new DateTime(expiryTicks, DateTimeKind.Utc) >= DateTime.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        public static int GetExpireMinutes()
        {
            if (int.TryParse(ConfigurationManager.AppSettings["StudentAuth:ExpireMinutes"], out var expireMinutes) && expireMinutes > 0)
            {
                return expireMinutes;
            }

            return 43200;
        }

        public static int GetAuthenticatedUserId(HttpRequestMessage request)
        {
            return TryGetAuthenticatedUser(request, out var userId, out _, out _) ? userId : 0;
        }

        public static int GetAuthenticatedPersonId(HttpRequestMessage request)
        {
            return TryGetAuthenticatedUser(request, out _, out var personId, out _) ? personId : 0;
        }

        public static bool TryGetAuthenticatedUser(HttpRequestMessage request, out int userId, out int personId, out string email)
        {
            userId = 0;
            personId = 0;
            email = null;

            var token = GetBearerToken(request);
            return TryValidateToken(token, out userId, out personId, out email);
        }

        public static string GetBearerToken(HttpRequestMessage request)
        {
            if (request == null)
            {
                return null;
            }

            var authHeader = request.Headers.Authorization;
            if (authHeader != null)
            {
                if (!string.IsNullOrWhiteSpace(authHeader.Parameter)
                    && string.Equals(authHeader.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
                {
                    return CleanToken(authHeader.Parameter);
                }

                // Swagger often sends "Authorization: <token>" with no Bearer prefix.
                // Web API then puts the entire token in Scheme and leaves Parameter empty.
                if (string.IsNullOrWhiteSpace(authHeader.Parameter)
                    && !string.IsNullOrWhiteSpace(authHeader.Scheme)
                    && !IsStandardAuthScheme(authHeader.Scheme))
                {
                    return CleanToken(authHeader.Scheme);
                }
            }

            if (request.Headers.TryGetValues("Authorization", out var authorizationValues))
            {
                var raw = CleanToken(authorizationValues.FirstOrDefault());
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    if (raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        return CleanToken(raw.Substring(7));
                    }

                    return raw;
                }
            }

            if (request.Headers.TryGetValues("X-Student-Token", out var studentTokenValues))
            {
                return CleanToken(studentTokenValues.FirstOrDefault());
            }

            return null;
        }

        private static bool IsStandardAuthScheme(string scheme)
        {
            return string.Equals(scheme, "Basic", StringComparison.OrdinalIgnoreCase)
                || string.Equals(scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
                || string.Equals(scheme, "Digest", StringComparison.OrdinalIgnoreCase)
                || string.Equals(scheme, "Negotiate", StringComparison.OrdinalIgnoreCase);
        }

        private static string CleanToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            return token.Trim().Trim('"');
        }

        private static string ComputeSignature(string payload)
        {
            var key = ConfigurationManager.AppSettings["StudentAuth:Key"];
            if (string.IsNullOrWhiteSpace(key))
            {
                key = "themyriad-student-auth-key";
            }

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            }
        }
    }
}
