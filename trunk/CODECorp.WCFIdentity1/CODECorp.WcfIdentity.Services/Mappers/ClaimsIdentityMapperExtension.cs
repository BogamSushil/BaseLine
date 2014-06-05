using System.Security.Claims;
using CODECorp.WcfIdentity.DataContract;
using CODECorp.WcfIdentity.DataContract.Views.Claim;
using CODECorp.WcfIdentity.DataContract.Views.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CODECorp.WcfIdentity.Models;
using CODECorp.WcfIdentity.DataContract.Views.ClaimIdentity;
using CODECorp.WcfIdentity.DataContract.Utilities;
using System.Diagnostics;

namespace CODECorp.WcfIdentity.Services.Mappers
{
    public static class ClaimsIdentityMapperExtension
    {
        public static ClaimIdentityView ConvertToClaimIdentityView(this ClaimsIdentity Identity)
        {
            ClaimIdentityView result = new ClaimIdentityView()
            {
                Name = Identity.Name,
                NameClaimType = Identity.NameClaimType,
                AuthenticationType = (AuthenticationTypeEnum)EnumStringValue.Parse(typeof(AuthenticationTypeEnum), Identity.AuthenticationType),
                RoleClaimType = Identity.RoleClaimType
            };

            foreach (Claim item in Identity.Claims)
                result.ClaimViewList.Add(item.ConvertToClaimView());

            return result;
        }

        public static ClaimView ConvertToClaimView(this Claim Claim)
        {
            return new ClaimView()
            {
                Type = Claim.Type,
                Value = Claim.Value,
                ValueType = Claim.ValueType
            };
        }
    }
}