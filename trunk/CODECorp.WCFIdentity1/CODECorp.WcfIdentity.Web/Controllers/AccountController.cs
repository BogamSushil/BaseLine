using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using CODECorp.WcfIdentity.ServiceContract;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using CODECorp.WcfIdentity.Web.Models;
using CODECorp.WcfIdentity.Web.ServiceChannel;
using CODECorp.WcfIdentity.DataContract.Messages.Membership;
using CODECorp.WcfIdentity.Web.Extensions;

namespace CODECorp.WcfIdentity.Web.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        public AccountController()
        {
            this.Membership = ServiceFactory.Create<IMembershipService>();
        }

        public IMembershipService Membership { get; private set; }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                LoginResponse response = await Membership.LoginAsync(new LoginRequest()
                {
                    UserName = model.UserName,
                    Password = model.Password
                });

                if (response.Success)
                {
                    ClaimsIdentity Identity = response.ClaimIdentity.ConvertToClaimsIdentity();
                    SignInAsync(Identity, model.RememberMe);
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    ModelState.AddModelError("", "Invalid username or password.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                CreateResponse response = await this.Membership.CreateAsync(new CreateRequest()
                {
                    UserName = model.UserName,
                    Password = model.Password,
                    AuthenticationType = DataContract.AuthenticationTypeEnum.ApplicationCookie
                });

                if (response.Success)
                {
                    ClaimsIdentity identity = response.ClaimIdentity.ConvertToClaimsIdentity();
                    SignInAsync(identity, IsPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    AddErrors(response.Errors);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/Disassociate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Disassociate(string loginProvider, string providerKey)
        {
            ManageMessageId? message = null;
            RemoveLoginResponse response = await this.Membership.RemoveLoginAsync(new RemoveLoginRequest()
            {
                UserId = new Guid(User.Identity.GetUserId()),
                LoginProvider = loginProvider,
                ProviderKey = providerKey
            });

            if (response.Success)
            {
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }

            return RedirectToAction("Manage", new { Message = message });
        }

        //
        // GET: /Account/Manage
        public ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            ViewBag.HasLocalPassword = HasPassword();
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Manage(ManageUserViewModel model)
        {
            bool hasPassword = HasPassword();
            ViewBag.HasLocalPassword = hasPassword;
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (hasPassword)
            {
                if (ModelState.IsValid)
                {
                    ChangePasswordResponse response = await this.Membership.ChangePasswordAsync(new ChangePasswordRequest()
                    {
                        UserId = new Guid(User.Identity.GetUserId()),
                        OldPassword = model.OldPassword,
                        NewPassword = model.NewPassword
                    });

                    if (response.Success)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        AddErrors(response.Errors);
                    }
                }
            }
            else
            {
                // User does not have a password so remove any validation errors caused by a missing OldPassword field
                ModelState state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    AddPasswordResponse response = await this.Membership.AddPasswordAsync(new AddPasswordRequest()
                    {
                        UserId = new Guid(User.Identity.GetUserId()),
                        NewPassword = model.NewPassword
                    });

                    if (response.Success)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    else
                    {
                        AddErrors(response.Errors);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            LoginExternalResponse response = await this.Membership.LoginExternalAsync(new LoginExternalRequest()
            {
                LoginProvider = loginInfo.Login.LoginProvider,
                ProviderKey = loginInfo.Login.ProviderKey,
            });

            if (response.Success)
            {
                ClaimsIdentity identity = response.ClaimIdentity.ConvertToClaimsIdentity();
                SignInAsync(identity, IsPersistent: false);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // If the user does not have an account, then prompt the user to create an account
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { UserName = loginInfo.DefaultUserName });
            }
        }

        //
        // POST: /Account/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new ChallengeResult(provider, Url.Action("LinkLoginCallback", "Account"), User.Identity.GetUserId());
        }

        //
        // GET: /Account/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
            }

            AddLoginResponse response = await this.Membership.AddLoginAsync(new AddLoginRequest()
            {
                UserId = new Guid(User.Identity.GetUserId()),
                LoginProvider = loginInfo.Login.LoginProvider,
                ProviderKey = loginInfo.Login.ProviderKey,
            });

            if (response.Success)
            {
                return RedirectToAction("Manage");
            }

            return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {

            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }

                CreateResponse createResponse = await this.Membership.CreateAsync(new CreateRequest()
                {
                    UserName = model.UserName
                });

                if (createResponse.Success)
                {
                    AddLoginResponse addLoginResponse = await this.Membership.AddLoginAsync(new AddLoginRequest()
                    {
                        UserId = new Guid(createResponse.UserId.ToString()),
                        LoginProvider = info.Login.LoginProvider,
                        ProviderKey = info.Login.ProviderKey
                    });

                    if (addLoginResponse.Success)
                    {
                        ClaimsIdentity identity = createResponse.ClaimIdentity.ConvertToClaimsIdentity();
                        SignInAsync(identity, IsPersistent: false);
                        return RedirectToLocal(returnUrl);
                    }
                    else
                    {
                        AddErrors(addLoginResponse.Errors);
                    }
                }

                AddErrors(createResponse.Errors);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut();
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        [ChildActionOnly]
        public ActionResult RemoveAccountList()
        {
            GetLoginsResponse response = this.Membership.GetLogins(new GetLoginsRequest()
            {
                UserId = new Guid(User.Identity.GetUserId())
            });

            if (response.Success)
            {

                ViewBag.ShowRemoveButton = this.HasPassword() || response.LinkedAccounts.Count > 1;

                return (ActionResult)PartialView("_RemoveAccountPartial", response.LinkedAccounts.ConvertToUserLoginInfoList());
            }
            else
            {
                AddErrors(response.Errors);
                return null;
            }
        }



        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void SignInAsync(ClaimsIdentity Identity, bool IsPersistent)
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = IsPersistent }, Identity);
        }

        private void AddErrors(IEnumerable<string> Errors)
        {
            foreach (string error in Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            FindByIdResponse response = this.Membership.FindById(new FindByIdRequest()
            {
                UserId = new Guid(User.Identity.GetUserId())
            });

            if (response.Success)
            {
                return response.UserView.PasswordHash != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        private class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}