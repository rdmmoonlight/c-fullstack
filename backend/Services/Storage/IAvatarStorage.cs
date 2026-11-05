// <copyright file="IAvatarStorage.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Storage
{
    public sealed record AvatarObject(Stream Stream, string ContentType);

    public interface IAvatarStorage
    {
        bool IsConfigured { get; }

        Task<string> UploadAvatarAsync(string fileName, Stream content, CancellationToken cancellationToken = default);

        Task<AvatarObject?> GetAvatarAsync(string fileName, CancellationToken cancellationToken = default);
    }
}
