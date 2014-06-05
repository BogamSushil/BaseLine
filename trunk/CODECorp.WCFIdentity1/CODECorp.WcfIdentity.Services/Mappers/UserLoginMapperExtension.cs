using CODECorp.WcfIdentity.DataContract.Views.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CODECorp.WcfIdentity.Models;
using CODECorp.WcfIdentity.DataContract.Views.Login;
using Microsoft.AspNet.Identity;

namespace CODECorp.WcfIdentity.Services.Mappers
{
    public static class UserLoginMapperExtension
    {
        public static IList<LoginView> ConvertToLoginViewList(this IEnumerable<UserLoginInfo> UserLoginInfoList)
        {
            List<LoginView> result = new List<LoginView>();

            foreach (UserLoginInfo item in UserLoginInfoList)
                result.Add(item.ConvertToLoginView());

            return result;
        }

        public static LoginView ConvertToLoginView(this UserLoginInfo UserLoginInfo)
        {
            return new LoginView()
            {
                LoginProvider = UserLoginInfo.LoginProvider,
                ProviderKey = UserLoginInfo.ProviderKey
            };
        }

    }
}