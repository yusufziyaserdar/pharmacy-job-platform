using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.SignalR
{
    public class NameIdentifierUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;
        }
    }
}