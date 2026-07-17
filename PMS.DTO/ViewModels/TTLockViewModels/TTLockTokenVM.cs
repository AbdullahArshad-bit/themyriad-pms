using Newtonsoft.Json;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels.TTLockViewModels
{
    public class TTLockTokenVM
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int AccessTokenExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }

    public class ApiResponse<T>
    {
        public T Data { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class OperationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ICCardResponse
    {
        public List<ICCardItem> list { get; set; }
        [JsonProperty("cardId")]
        public int CardId { get; set; }
    }

    public class RoomWrapper
    {
        [JsonProperty("rooms")]
        public List<RoomDetails> Rooms { get; set; }
    }

    public class RoomDetails
    {
        [JsonProperty("rm")]
        public string RoomNumber { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("untildate")]
        public DateTime UntilDate { get; set; }
    }

    public class CheckInResponse
    {
        public int Result { get; set; }
        public string Msg { get; set; }
        public CheckInData Data { get; set; }
    }

    public class CheckInData
    {
        public string Tid { get; set; }
        public string CID { get; set; }
    }

    public class MesserschmittResponse
    {
        public int Result { get; set; }
        public string Msg { get; set; }
    }




}
