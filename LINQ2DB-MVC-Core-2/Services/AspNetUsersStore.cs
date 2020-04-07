using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LINQ2DB_MVC_Core_2.Data;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Identity;

namespace LINQ2DB_MVC_Core_2.Services
{
    public class AspNetUsersStore : UserStore<AspNetUsers>, IUserTwoFactorRecoveryCodeStore<AspNetUsers>
    {

        private IConnectionFactory moFactory;
        // This leans heavily on understanding the Linq2DB Identity code: -
        //      (see: https://github.com/linq2db/LinqToDB.Identity)
        public AspNetUsersStore(IConnectionFactory factory, IdentityErrorDescriber identityErrorDescriber)
            : base(factory, identityErrorDescriber)
        {
            moFactory = factory;
        }

        // Two Factor Authentication: BEGIN
        private const string InternalLoginProvider = "[AspNetUserStore]";
        private const string RecoveryCodeTokenName = "RecoveryCodes";
        // This leans heavily on understanding the Microsoft DotNet Core Identity code: -
        //      (see: https://github.com/dotnet/aspnetcore/tree/2.1.3)
        public async Task<int> CountCodesAsync(AspNetUsers user, CancellationToken cancellationToken)
        {
            // TwoFactorAuth logic copied from UserBaseStore: -
            //  https://github.com/dotnet/aspnetcore/blob/2.1.3/src/Identity/Extensions.Stores/src/UserStoreBase.cs.
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            using (var _db = moFactory.GetConnection())
            {
                var mergedCodes = await GetTokenAsync(_db, user, InternalLoginProvider, RecoveryCodeTokenName, cancellationToken) ?? "";
                if (mergedCodes.Length > 0)
                {
                    return mergedCodes.Split(';').Length;
                }
            }
            return 0;
        }

        public async Task<bool> RedeemCodeAsync(AspNetUsers user, string code, CancellationToken cancellationToken)
        {
            // TwoFactorAuth logic copied from UserBaseStore: -
            //  https://github.com/dotnet/aspnetcore/blob/2.1.3/src/Identity/Extensions.Stores/src/UserStoreBase.cs.
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (code == null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            var mergedCodes = await GetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, cancellationToken) ?? "";
            var splitCodes = mergedCodes.Split(';');
            if (splitCodes.Contains(code))
            {
                var updatedCodes = new List<string>(splitCodes.Where(s => s != code));
                await ReplaceCodesAsync(user, updatedCodes, cancellationToken);
                return true;
            }
            return false;
        }

        public async Task ReplaceCodesAsync(AspNetUsers user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
        {
            // TwoFactorAuth logic copied from UserBaseStore: -
            //  https://github.com/dotnet/aspnetcore/blob/2.1.3/src/Identity/Extensions.Stores/src/UserStoreBase.cs.
            var mergedCodes = string.Join(";", recoveryCodes);
            await SetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, mergedCodes, cancellationToken);
            return;
        }
        // Two Factor Authentication: END

        protected override LinqToDB.Identity.IdentityUserToken<string> CreateUserToken(AspNetUsers user, string loginProvider, string name, string value)
        {
            return new AspNetUserTokens()
            {
                UserId = user.Id,
                LoginProvider = loginProvider,
                Name = name,
                Value = value
            };
        }

        protected override async Task<IList<Claim>> GetClaimsAsync(LinqToDB.Data.DataConnection db, AspNetUsers user, CancellationToken cancellationToken)
        {
            var result = await
                            db
                                .GetTable<AspNetUserClaims>()
                                .Where(uc => uc.UserId.Equals(user.Id))
                                .Select(c => c.ToClaim())
                                .ToListAsync(cancellationToken);
            return result;
        }

        protected override async Task<IList<string>> GetRolesAsync(LinqToDB.Data.DataConnection db, AspNetUsers user, CancellationToken cancellationToken)
        {
            var userId = user.Id;
            var query = from userRole in db.GetTable<AspNetUserRoles>()
                        join role in db.GetTable<AspNetRoles>() on userRole.RoleId equals role.Id
                        where userRole.UserId.Equals(userId)
                        select role.Name;

            return await query.ToListAsync(cancellationToken);
        }

        protected override async Task<string> GetTokenAsync(LinqToDB.Data.DataConnection db, AspNetUsers user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            var entry = await db
                .GetTable<AspNetUserTokens>()
                .Where(_ => _.UserId.Equals(user.Id) && _.LoginProvider == loginProvider && _.Name == name)
                .FirstOrDefaultAsync(cancellationToken);

            return entry?.Value;
        }

        protected override async Task SetTokenAsync(LinqToDB.Data.DataConnection db, AspNetUsers user, string loginProvider, string name, string value, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                var q = db.GetTable<AspNetUserTokens>()
                    .Where(_ => _.UserId.Equals(user.Id) && _.LoginProvider == loginProvider && _.Name == name);

                var token = q.FirstOrDefault();

                if (token == null)
                {
                    db.Insert((AspNetUserTokens)CreateUserToken(user, loginProvider, name, value));
                }
                else
                {
                    token.Value = value;
                    q.Set(_ => _.Value, value)
                        .Update();
                }
            }, cancellationToken);
        }

        protected override async Task<AspNetUsers> FindByLoginAsync(LinqToDB.Data.DataConnection db, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            var q = from ul in db.GetTable<AspNetUserLogins>()
                    join u in db.GetTable<AspNetUsers>() on ul.UserId equals u.Id
                    where ul.LoginProvider == loginProvider && ul.ProviderKey == providerKey
                    select u;

            return await q.FirstOrDefaultAsync(cancellationToken);
        }

        protected override async Task RemoveTokenAsync(LinqToDB.Data.DataConnection db, AspNetUsers user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
                    db.GetTable<AspNetUserTokens>()
                        .Where(_ => _.UserId.Equals(user.Id) && _.LoginProvider == loginProvider && _.Name == name)
                        .Delete(),
                cancellationToken);
        }

        protected override async Task RemoveClaimsAsync(LinqToDB.Data.DataConnection db, AspNetUsers user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                var q = db.GetTable<AspNetUserClaims>();
                var userId = Expression.PropertyOrField(Expression.Constant(user, typeof(AspNetUsers)), nameof(user.Id));
                var equals = typeof(string).GetMethod(nameof(IEquatable<string>.Equals), new[] { typeof(string) });
                var uc = Expression.Parameter(typeof(AspNetUserClaims));
                Expression body = null;
                var ucUserId = Expression.PropertyOrField(uc, nameof(IIdentityUserClaim<string>.UserId));
                var userIdEquals = Expression.Call(ucUserId, @equals, userId);

                foreach (var claim in claims)
                {
                    var cl = Expression.Constant(claim);

                    var claimValueEquals = Expression.Equal(
                        Expression.PropertyOrField(uc, nameof(IIdentityUserClaim<string>.ClaimValue)),
                        Expression.PropertyOrField(cl, nameof(Claim.Value)));
                    var claimTypeEquals =
                        Expression.Equal(
                            Expression.PropertyOrField(uc, nameof(IIdentityUserClaim<string>.ClaimType)),
                            Expression.PropertyOrField(cl, nameof(Claim.Type)));

                    var predicatePart = Expression.And(Expression.And(userIdEquals, claimValueEquals), claimTypeEquals);

                    body = body == null ? predicatePart : Expression.Or(body, predicatePart);
                }

                if (body != null)
                {
                    var predicate = Expression.Lambda<Func<AspNetUserClaims, bool>>(body, uc);

                    q.Where(predicate).Delete();
                }
            }, cancellationToken);
        }

        protected override async Task<IList<AspNetUsers>> UsersForClaimAsync(LinqToDB.Data.DataConnection db, Claim claim, CancellationToken cancellationToken)
        {
            var query = from userclaims in db.GetTable<AspNetUserClaims>()
                        join user in db.GetTable<AspNetUsers>() on userclaims.UserId equals user.Id
                        where userclaims.ClaimValue == claim.Value
                              && userclaims.ClaimType == claim.Type
                        select user;

            return await query.ToListAsync(cancellationToken);
        }

        protected override async Task RemoveLoginAsync(LinqToDB.Data.DataConnection db, AspNetUsers user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
                    db
                        .GetTable<AspNetUserLogins>()
                        .Delete(
                            userLogin =>
                                userLogin.UserId.Equals(user.Id) && userLogin.LoginProvider == loginProvider &&
                                userLogin.ProviderKey == providerKey),
                cancellationToken);
        }

        protected override async Task<IList<UserLoginInfo>> GetLoginsAsync(LinqToDB.Data.DataConnection db, AspNetUsers user, CancellationToken cancellationToken)
        {
            var userId = user.Id;
            return await db
                .GetTable<AspNetUserLogins>()
                .Where(l => l.UserId.Equals(userId))
                .Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName))
                .ToListAsync(cancellationToken);
        }

        protected override Task AddToRoleAsync(LinqToDB.Data.DataConnection db, AspNetUsers user, string normalizedRoleName, CancellationToken cancellationToken)
        {
            // Roles disabled
            return Task.CompletedTask;
        }

        protected override Task RemoveFromRoleAsync(LinqToDB.Data.DataConnection db, AspNetUsers user, string normalizedRoleName, CancellationToken cancellationToken)
        {
            // Roles disabled
            return Task.CompletedTask;
        }

        protected override Task<bool> IsInRoleAsync(LinqToDB.Data.DataConnection db, AspNetUsers user, string normalizedRoleName, CancellationToken cancellationToken)
        {
            // Roles disabled
            return Task.FromResult(false);
        }

        protected override Task<IList<AspNetUsers>> GetUsersInRoleAsync(LinqToDB.Data.DataConnection db, string normalizedRoleName, CancellationToken cancellationToken)
        {
            // Roles disabled
            return Task.FromResult<IList<AspNetUsers>>(null);
        }

        protected override async Task ReplaceClaimAsync(AspNetUsers user, Claim claim, Claim newClaim, CancellationToken cancellationToken, LinqToDB.Data.DataConnection db)
        {
            await Task.Run(() =>
            {
                var q = db
                    .GetTable<AspNetUserClaims>()
                    .Where(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type);

                q.Set(_ => _.ClaimValue, newClaim.Value)
                    .Set(_ => _.ClaimType, newClaim.Type)
                    .Update();
            }, cancellationToken);
        }

        protected override LinqToDB.Identity.IdentityUserRole<string> CreateUserRole(AspNetUsers user, LinqToDB.Identity.IdentityRole role)
        {
            return new AspNetUserRoles
            {
                UserId = user.Id,
                RoleId = role.Id
            };
        }

        protected override LinqToDB.Identity.IdentityUserClaim<string> CreateUserClaim(AspNetUsers user, Claim claim)
        {
            var userClaim = new AspNetUserClaims { UserId = user.Id };
            userClaim.InitializeFromClaim(claim);
            return userClaim;
        }

        protected override LinqToDB.Identity.IdentityUserLogin<string> CreateUserLogin(AspNetUsers user, UserLoginInfo login)
        {
            return new AspNetUserLogins()
            {
                UserId = user.Id,
                LoginProvider = login.LoginProvider,
                ProviderDisplayName = login.ProviderDisplayName,
                ProviderKey = login.ProviderKey
            };
        }

        protected override async Task AddLoginAsync(LinqToDB.Data.DataConnection db, AspNetUsers user, UserLoginInfo login, CancellationToken cancellationToken)
        {
            await Task.Run(() => db.Insert((AspNetUserLogins)CreateUserLogin(user, login)), cancellationToken);
        }
    }

}
