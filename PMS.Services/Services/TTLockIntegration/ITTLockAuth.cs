using PMS.DTO.ViewModels.ApiViewModels;
using PMS.DTO.ViewModels.TTLockViewModels;
using PMS.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.TTLockIntegration
{
    public interface ITTLockAuth
    {
        Task<string> GetAuthTokenAsync();
        Task<OperationResult> RefreshScienerAccessTokenAsync(ClientIntegration clientIntegration);
        Task<long> GetStartDate(string clientId, string accessToken, int lockId, long date);

        Task<int> AddReversedCardNumber(string clientId, string accessToken, int lockId, string cardNumber, long startDate, long endDate, int addType, long date);

        Task<List<RoomDetails>> GetRooms(string baseUrl, string accessToken);

        Task<CheckInResponse> CheckInGuest(string room, string moveIn, string moveOut, string enco);

        Task<MesserschmittResponse> EMSCheckinOLD(string roomName);

        Task<MesserschmittResponse> EMSCheckin(string roomName, string accessToken);

        Task<MesserschmittResponse> CheckOutMesserschmitt(string roomName, string tid, string accessToken);

    }
}
