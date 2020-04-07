using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LINQ2DB_MVC_Core_2.Areas.Identity.Models;
using System.Collections.Generic;

namespace LINQ2DB_MVC_Core_2.Extensions
{
    /// <summary>
    /// Gets the icon for a particular 3rd party auth provider.
    /// </summary>
    public static class PageModelHelpers
    {
        public static string ProviderIcon(this PageModel poPage, string psProvider)
        {
            var sIconSource = "";
            switch (psProvider.ToLowerInvariant())
            {
                case "google":
                    sIconSource = "~/images/google.jpg";
                    break;
                case "facebook":
                    sIconSource = "~/images/facebook.png";
                    break;
                case "microsoft":
                    sIconSource = "~/images/microsoft.png";
                    break;
            }
            if (sIconSource.Length > 0)
            {
                sIconSource = poPage.Url.Content(sIconSource);
            }
            return sIconSource;
        }

        public static List<PrettyAuthScheme> GetPrettyAuthSchemeList(this PageModel poPage, IList<AuthenticationScheme> poList)
        {
            var poResultList = new List<PrettyAuthScheme>();
            foreach (var extLogin in poList)
            {
                poResultList.Add(GetPrettyAuthScheme(poPage, extLogin));
            }

            return poResultList;
        }
        /// <summary>
        /// Extension to add the IconSource field to an AuthenticationScheme object.
        /// </summary>
        public static PrettyAuthScheme GetPrettyAuthScheme(this PageModel poPage, AuthenticationScheme poAuthScheme)
        {
            return new PrettyAuthScheme(poAuthScheme.Name, poAuthScheme.DisplayName, poAuthScheme.HandlerType, poPage.ProviderIcon(poAuthScheme.Name));
        }

        public static List<PrettyLoginInfo> GetPrettyLoginInfoList(this PageModel poPage, IList<UserLoginInfo> poList)
        {
            var poResultList = new List<PrettyLoginInfo>();
            foreach (var userLoginInfo in poList)
            {
                poResultList.Add(GetPrettyLoginInfo(poPage, userLoginInfo));
            }

            return poResultList;
        }
        /// <summary>
        /// Extension to add the IconSource field to a UserLoginInfo object.
        /// </summary>
        public static PrettyLoginInfo GetPrettyLoginInfo(this PageModel poPage, UserLoginInfo poLogin)
        {
            return new PrettyLoginInfo(poLogin.LoginProvider, poLogin.ProviderKey, poLogin.ProviderDisplayName, poPage.ProviderIcon(poLogin.LoginProvider));
        }
    }
}
