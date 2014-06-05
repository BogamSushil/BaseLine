using CODECorp.WcfIdentity.DataContract.Views.Claim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using CODECorp.WcfIdentity.DataContract.Views.ClaimIdentity;
using CODECorp.WcfIdentity.DataContract.Utilities;
using CODECorp.WcfIdentity.DataContract.Views.Login;
using Microsoft.AspNet.Identity;

namespace CODECorp.WcfIdentity.Web.Extensions
{
    public static class Extensions
    {
        private const string UserNameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
        private const string RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

        /// <summary>
        /// Convert from View to Claim Identity
        /// </summary>
        /// <param name="ClaimIdentity">Claim Identity View</param>
        /// <returns>Instance of System.Security.Claims.ClaimsIdentity</returns>
        public static ClaimsIdentity ConvertToClaimsIdentity(this ClaimIdentityView ClaimIdentity)
        {
            string authenticationType = EnumStringValue.GetStringValue(ClaimIdentity.AuthenticationType);
            ClaimsIdentity identity = new ClaimsIdentity(authenticationType, UserNameClaimType, RoleClaimType);

            foreach (ClaimView item in ClaimIdentity.ClaimViewList)
                identity.AddClaim(new Claim(item.Type, item.Value, item.ValueType));

            return identity;
        }

        /// <summary>
        /// Convert from View to User Login Info
        /// </summary>
        /// <param name="LoginViewList">IEnumerable of Login View</param>
        /// <returns>Instance of IList<Microsoft.AspNet.Identity.LoginView></returns>
        public static IList<UserLoginInfo> ConvertToUserLoginInfoList(this IEnumerable<LoginView> LoginViewList)
        {
            List<UserLoginInfo> result = new List<UserLoginInfo>();

            foreach (LoginView item in LoginViewList)
                result.Add(item.ConvertToUserLoginInfo());

            return result;
        }

        /// <summary>
        /// Convert from View to User Login Info 
        /// </summary>
        /// <param name="LoginView">Login View</param>
        /// <returns>Instance of Microsoft.AspNet.Identity.LoginView</returns>
        public static UserLoginInfo ConvertToUserLoginInfo(this LoginView LoginView)
        {
            return new UserLoginInfo(LoginView.LoginProvider, LoginView.ProviderKey);
        }
    }
}