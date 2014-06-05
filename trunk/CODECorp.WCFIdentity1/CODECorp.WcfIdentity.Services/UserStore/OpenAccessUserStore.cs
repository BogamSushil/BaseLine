using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using CODECorp.WcfIdentity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;

namespace CODECorp.WcfIdentity.Services.UserStore
{
    /// <summary>
    /// Telerik OpenAccess 2013.3.1014.1 based User Store for the Microsoft ASP.NET Identity membership system.
    /// -----------------------------------------------
    /// This UserStore is  almost identical to the UserStore as implemented in default membership system 
    /// (i.e. Microsoft.AspNet.Identity.EntityFramework.UserStore) but uses the Telerik OpenAccess ORM for the store.
    /// The implementation itself is pretty straightforward.
    /// -----------------------------------------------
    /// Version 1: Max G @ CODECorp 12/12/2013 - First Version
    /// Version 1.1: Max G @ CODECorp 14/12/2013 - Added DeleteAsync method. Forgot it first time round. Whoops!
    /// </summary>
    public class OpenAccessUserStore : IUserStore<AspNetUser>, IUserPasswordStore<AspNetUser>, IUserClaimStore<AspNetUser>,
                                        IUserRoleStore<AspNetUser>, IUserSecurityStampStore<AspNetUser>, IUserLoginStore<AspNetUser>
    {
        private bool _disposed;

        public OpenAccessUserStore()
        {
            this.DisposeContext = true;
        }

        public OpenAccessUserStore(EntitiesModel Context)
        {
            if (Context == null)
            {
                throw new ArgumentNullException("context");
            }
            this.Context = Context;
            this.AutoSaveChanges = true;
            this.DisposeContext = true;
        }

        public EntitiesModel Context { get; set; }

        public bool AutoSaveChanges { get; set; }

        public bool DisposeContext { get; set; }

        public Task AddLoginAsync(AspNetUser user, UserLoginInfo login)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (login == null)
            {
                throw new ArgumentNullException("login");
            }

            AspNetUserLogin identityUserLogin = new AspNetUserLogin();
            identityUserLogin.AspNetUser = user;
            identityUserLogin.ProviderKey = login.ProviderKey;
            identityUserLogin.LoginProvider = login.LoginProvider;
            this.Context.Add(identityUserLogin);

            return Task.FromResult<int>(0);
        }

        public Task RemoveLoginAsync(AspNetUser user, UserLoginInfo login)
        {
            Debugger.Launch();
            this.ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (login == null)
            {
                throw new ArgumentNullException("login");
            }

            AspNetUserLogin userLogin = user.AspNetUserLogins.Where(o =>
            {
                if (!(o.LoginProvider == login.LoginProvider) || o.AspNetUser != (object)user)
                {
                    return false;
                }
                return o.ProviderKey == login.ProviderKey;
            }).SingleOrDefault();

            if (userLogin != null)
                this.Context.Delete(userLogin);

            return Task.FromResult<int>(0);
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(AspNetUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            List<UserLoginInfo> userLoginInfos = new List<UserLoginInfo>();

            foreach (AspNetUserLogin login in user.AspNetUserLogins)
                userLoginInfos.Add(new UserLoginInfo(login.LoginProvider, login.ProviderKey));

            return Task.FromResult<IList<UserLoginInfo>>(userLoginInfos);
        }

        public Task<AspNetUser> FindAsync(UserLoginInfo login)
        {
            this.ThrowIfDisposed();
            if (login == null)
            {
                throw new ArgumentNullException("login");
            }

            AspNetUserLogin UserLogin = this.Context.AspNetUserLogins.Where(o => o.LoginProvider == login.LoginProvider && o.ProviderKey == login.ProviderKey).FirstOrDefault();

            return Task.FromResult(UserLogin != null ? UserLogin.AspNetUser : null);
        }

        public Task<IList<Claim>> GetClaimsAsync(AspNetUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            IList<Claim> claims = new List<Claim>();
            foreach (AspNetUserClaim claim in user.AspNetUserClaims)
            {
                claims.Add(new Claim(claim.ClaimType, claim.ClaimValue));
            }
            return Task.FromResult<IList<Claim>>(claims);
        }

        public Task AddClaimAsync(AspNetUser user, Claim claim)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }
            ICollection<AspNetUserClaim> claims = user.AspNetUserClaims;
            AspNetUserClaim identityUserClaim = new AspNetUserClaim();
            identityUserClaim.User_Id = user.Id;
            identityUserClaim.ClaimType = claim.Type;
            identityUserClaim.ClaimValue = claim.Value;
            claims.Add(identityUserClaim);
            return Task.FromResult<int>(0);
        }

        public Task RemoveClaimAsync(AspNetUser user, Claim claim)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }

            List<AspNetUserClaim> list = user.AspNetUserClaims.Where(o =>
            {
                if (o.ClaimValue != claim.Value)
                {
                    return false;
                }
                return o.ClaimType == claim.Type;
            }).ToList();

            foreach (AspNetUserClaim identityUserClaim in list)
            {
                this.Context.Delete(identityUserClaim);
            }

            return Task.FromResult<int>(0);
        }

        public Task AddToRoleAsync(AspNetUser user, string role)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentException("Value Cannot Be Null Or Empty", "role");
            }

            AspNetRole identityRole = this.Context.AspNetRoles.Where(o => o.Name == role).FirstOrDefault();
            if (identityRole == null)
            {
                throw new InvalidOperationException(string.Format("Role not found", role));
            }

            identityRole.AspNetUsers.Add(user);

            return Task.FromResult<int>(0);
        }

        public Task RemoveFromRoleAsync(AspNetUser user, string role)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentException("Value Cannot Be Null Or Empty", "role");
            }

            AspNetRole identityRole = this.Context.AspNetRoles.Where(o => o.Name.ToUpper() == role.ToUpper()).FirstOrDefault();

            if (identityRole != null)
                this.Context.Delete(identityRole);

            return Task.FromResult<int>(0);
        }

        public Task<IList<string>> GetRolesAsync(AspNetUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            List<string> list = this.Context.AspNetRoles.Select(o => o.Name).ToList();

            return Task.FromResult<IList<string>>(list);
        }

        public Task<bool> IsInRoleAsync(AspNetUser user, string role)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentException("Value Cannot Be Null Or Empty", "role");
            }

            return Task.FromResult<bool>(user.AspNetRoles.Any(o => o.Name.ToUpper() == role.ToUpper()));
        }

        public Task SetSecurityStampAsync(AspNetUser user, string stamp)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            user.SecurityStamp = stamp;
            return Task.FromResult<int>(0);
        }

        public Task<string> GetSecurityStampAsync(AspNetUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult<string>(user.SecurityStamp);
        }

        public Task SetPasswordHashAsync(AspNetUser user, string passwordHash)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            user.PasswordHash = passwordHash;
            return Task.FromResult<int>(0);
        }

        public Task<string> GetPasswordHashAsync(AspNetUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult<string>(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(AspNetUser user)
        {
            return Task.FromResult<bool>(user.PasswordHash != null);
        }

        private async Task SaveChanges()
        {
            if (this.AutoSaveChanges)
            {
                await Task.Run(() =>
                    this.Context.SaveChanges());
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.DisposeContext && disposing && this.Context != null)
                this.Context.Dispose();

            this._disposed = true;
            this.Context = null;
        }

        public async Task CreateAsync(AspNetUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
                throw new ArgumentNullException("user");

            this.Context.Add(user);
            await this.SaveChanges();
        }

        public async Task UpdateAsync(AspNetUser user)
        {
            this.ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException("user");

            await this.SaveChanges();
        }

        public async Task DeleteAsync(AspNetUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
                throw new ArgumentNullException("user");

            this.Context.Delete(user);

            await this.SaveChanges();
        }

        public Task<AspNetUser> FindByIdAsync(string userId)
        {
            this.ThrowIfDisposed();
            return Task.FromResult<AspNetUser>(this.Context.AspNetUsers.Where(o => o.Id == userId).SingleOrDefault());
        }

        public Task<AspNetUser> FindByNameAsync(string userName)
        {
            this.ThrowIfDisposed();
            return Task.FromResult<AspNetUser>(this.Context.AspNetUsers.Where(o => o.UserName == userName).SingleOrDefault());
        }

        private void ThrowIfDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }
    }
}