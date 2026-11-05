// <copyright file="NotificationsService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Notifications;

using AumoBackend.Data;
using AumoBackend.DTOs;
using Microsoft.EntityFrameworkCore;

public class NotificationsService : INotificationsService
{
    private readonly AppDbContext? context;

    public async Task<IEnumerable<NotificationDto>> GetNotificationsByUserIdAsync(Guid userId, int limit)
    {
        return await this.context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                TargetUrl = n.TargetUrl,
                CreatedAt = n.CreatedAt.ToString("o"),
            })
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(Guid id, Guid userId)
    {
        var notification = await this.context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null)
        {
            return false;
        }

        notification.IsRead = true;
        await this.context.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        await this.context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}
