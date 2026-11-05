// <copyright file="AvatarsController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Controllers
{
    using AumoBackend.Services.Storage;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [AllowAnonymous]
    [Route("/avatars")]
    public class AvatarsController : ControllerBase
    {
        private readonly IAvatarStorage avatarStorage;

        public AvatarsController(IAvatarStorage avatarStorage)
        {
            this.avatarStorage = avatarStorage;
        }

        [HttpGet("{fileName}")]
        public async Task<IActionResult> Get(string fileName, CancellationToken cancellationToken)
        {
            if (!this.avatarStorage.IsConfigured)
            {
                return this.NotFound();
            }

            var avatar = await this.avatarStorage.GetAvatarAsync(fileName, cancellationToken);
            if (avatar == null)
            {
                return this.NotFound();
            }

            this.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
            this.Response.Headers["X-Content-Type-Options"] = "nosniff";
            return this.File(avatar.Stream, avatar.ContentType);
        }
    }
}
