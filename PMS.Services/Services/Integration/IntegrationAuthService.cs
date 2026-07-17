using System;
using System.Linq;
using System.Collections.Generic;
using PMS.EF;
using PMS.Repository.UnitOfWork;

namespace PMS.Services.Services.Integration
{
    public interface IIntegrationAuthService
    {
        ClientIntegration AuthenticateClient(string clientId, string clientSecret);
        void StoreAccessToken(ClientIntegration client, string accessToken, DateTime expiresAtUtc);
    }

    /// <summary>
    /// Validates integration clients against the existing ClientIntegration table
    /// (Client_ID / Client_Secret), then persists the issued access token metadata.
    /// </summary>
    public class IntegrationAuthService : IIntegrationAuthService
    {
        private readonly UnitOfWork<PMSEntities> uow;

        public IntegrationAuthService(UnitOfWork<PMSEntities> _uow)
        {
            uow = _uow;
        }

        public ClientIntegration AuthenticateClient(string clientId, string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                return null;
            }

            return uow.GenericRepository<ClientIntegration>().Table
                .FirstOrDefault(c =>
                    c.Client_ID == clientId &&
                    c.Client_Secret == clientSecret);
        }

        public void StoreAccessToken(ClientIntegration client, string accessToken, DateTime expiresAtUtc)
        {
            if (client == null)
            {
                return;
            }

            client.Access_Token = accessToken;
            client.Access_Token_Expiry = expiresAtUtc.ToString("o");
            uow.GenericRepository<ClientIntegration>().Update(client);
            uow.SaveChanges();
        }
    }
}
