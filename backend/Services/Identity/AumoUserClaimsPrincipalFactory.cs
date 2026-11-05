// <copyright file="AumoUserClaimsPrincipalFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Identity
{
    using System.Security.Claims;
    using AumoBackend.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Options;

    public class AumoUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>
    {
        public AumoUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            // Tambahkan custom claims di sini jika diperlukan
            // identity.AddClaim(new Claim("TenantId", user.TenantId.ToString()));
            return identity;
        }
    }
}
