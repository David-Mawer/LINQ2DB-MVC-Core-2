using Microsoft.AspNetCore.Authentication;
using System;

namespace LINQ2DB_MVC_Core_2.Areas.Identity.Models
{
    public class PrettyAuthScheme : AuthenticationScheme
    {
        public readonly string IconSource;

        public PrettyAuthScheme(string name, string displayName, Type handlerType, string psIconSource) : base(name, displayName, handlerType)
        {
            IconSource = psIconSource;
        }
    }
}
