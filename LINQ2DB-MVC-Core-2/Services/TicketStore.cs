using LINQ2DB_MVC_Core_2.Data;
using LinqToDB;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using System.Threading.Tasks;


namespace LINQ2DB_MVC_Core_2.Services
{
    public class TicketStore : ITicketStore
    {
        private readonly IConnectionFactory moFactory;
        private const String mLoginProvider = "PITHAuth";
        public TicketStore(IConnectionFactory poFactory)
        {
            moFactory = poFactory;
        }

        public async Task RemoveAsync(string key)
        {
            using (var _db = moFactory.GetConnection())
            {
                var ticket = await _db.GetTable<AspNetUserTokens>().SingleOrDefaultAsync(x => x.Name == key && x.LoginProvider == mLoginProvider);
                if (ticket != null)
                {
                    await _db.DeleteAsync(ticket);
                }
            }
        }

        public async Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            using (var _db = moFactory.GetConnection())
            {
                var authenticationTicket = await _db.GetTable<AspNetUserTokens>().SingleOrDefaultAsync(x => x.Name == key && x.LoginProvider == mLoginProvider);
                if (authenticationTicket != null)
                {
                    authenticationTicket.Value = SerializeToString(ticket);
                    //authenticationTicket.LastActivity = DateTimeOffset.UtcNow;
                    //authenticationTicket.Expires = ticket.Properties.ExpiresUtc;
                    await _db.UpdateAsync(authenticationTicket);
                }
            }
        }

        public async Task<AuthenticationTicket> RetrieveAsync(string key)
        {
            using (var _db = moFactory.GetConnection())
            {
                var authenticationTicket = await _db.GetTable<AspNetUserTokens>().SingleOrDefaultAsync(x => x.Name == key && x.LoginProvider == mLoginProvider);
                if (authenticationTicket != null)
                {
                    //authenticationTicket.LastActivity = DateTimeOffset.UtcNow;
                    //await moDataConnection.SaveChangesAsync();

                    return DeserializeFromString(authenticationTicket.Value);
                }
            }

            return null;
        }

        public async Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var userId = string.Empty;
            var nameIdentifier = ticket.Principal.Identity.Name;

            if (ticket.AuthenticationScheme == "Identity.Application")
            {
                userId = nameIdentifier;
            }
            // If using a external login provider like google we need to resolve the userid through the Userlogins
            else if (ticket.AuthenticationScheme == "Identity.External")
            {
                using (var _db = moFactory.GetConnection())
                {
                    userId = (await _db.GetTable<AspNetUserLogins>().SingleAsync(x => x.ProviderKey == nameIdentifier)).UserId;
                }
            }

            var authenticationTicket = new AspNetUserTokens
            {
                Name = nameIdentifier,
                UserId = userId,
                //authenticationTicket.LastActivity = DateTimeOffset.UtcNow;
                Value = SerializeToString(ticket),
                LoginProvider = mLoginProvider
            };

            var expiresUtc = ticket.Properties.ExpiresUtc;
            if (expiresUtc.HasValue)
            {
                //    authenticationTicket.Expires = expiresUtc.Value;
            }

            using (var _db = moFactory.GetConnection())
            {
                await _db.InsertOrReplaceAsync(authenticationTicket);
            }

            return authenticationTicket.Name;
        }

        private string SerializeToString(AuthenticationTicket source)
            => Convert.ToBase64String(TicketSerializer.Default.Serialize(source));

        private AuthenticationTicket DeserializeFromString(string source)
            => source == null ? null : TicketSerializer.Default.Deserialize(Convert.FromBase64String(source));

    }
}
