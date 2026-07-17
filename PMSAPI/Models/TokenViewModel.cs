using System.Collections.Generic;

namespace PMSAPI.Models
{
    /// <summary>
    /// Token response returned after successful PMS user login.
    /// </summary>
    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<int> AssignedLocations { get; set; }
    }
}
