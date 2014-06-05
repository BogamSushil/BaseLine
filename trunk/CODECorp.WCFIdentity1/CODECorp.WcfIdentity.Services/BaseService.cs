using CODECorp.WcfIdentity.Models;
using CODECorp.WcfIdentity.Services.UserStore;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace CODECorp.WcfIdentity.Services
{
    public class BaseService
    {
        public BaseService()
        {
            this.CreateUserManager();
        }

        /// <summary>
        /// Create an instance of the user manager, inject the OpenAccessUserStore and the Telerik Context and finally the UserValidator
        /// </summary>
        protected void CreateUserManager()
        {
            UserManager<AspNetUser> Manager = new UserManager<AspNetUser>(new OpenAccessUserStore(new EntitiesModel()));

            Manager.UserValidator = new UserValidator<AspNetUser>(Manager)
            {
                AllowOnlyAlphanumericUserNames = false
            };

            this.UserManager = Manager;
        }

        public UserManager<AspNetUser> UserManager { get; private set; }
    }
}