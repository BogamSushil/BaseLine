using CODECorp.WcfIdentity.DataContract.Views.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CODECorp.WcfIdentity.Models;

namespace CODECorp.WcfIdentity.Services.Mappers
{
    public static class UserMapperExtension
    {
        public static UserView ConvertToUserView(this AspNetUser Entity)
        {
            return new UserView()
            {
                Id = new Guid(Entity.Id),
                FirstName = Entity.FirstName,
                LastName = Entity.LastName,
                PasswordHash = Entity.PasswordHash,
                UserName = Entity.UserName
            };
        }
    }
}