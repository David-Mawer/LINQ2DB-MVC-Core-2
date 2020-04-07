using Microsoft.AspNetCore.Identity;

namespace LINQ2DB_MVC_Core_2.Areas.Identity.Models
{
    public class PrettyLoginInfo : UserLoginInfo
    {
        public readonly string IconSource;

        public PrettyLoginInfo(string loginProvider, string providerKey, string displayName, string psIconSource) : base(loginProvider, providerKey, displayName)
        {
            IconSource = psIconSource;
        }
    }
}
