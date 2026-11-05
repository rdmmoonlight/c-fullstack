// <copyright file="NotificationsController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Controllers;

using System.Security.Claims;
using AumoBackend.Data;
using AumoBackend.DTOs;
using AumoBackend.Validators.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
[ApiController]
[Route("/notifications")]

public class NotificationsController : ControllerBase
{
    private readonly AppDbContext context;
    private readonly IValidator<GetNotificationsQuery> queryValidator;

    public NotificationsController(AppDbContext context, IValidator<GetNotificationsQuery> queryValidator)
    {
        this.context = context;
        this.queryValidator = queryValidator;
    }

    // Ubah return type method helper dari string ke Guid
    private Guid GetCurrentUserId()
    {
        var userIdStr = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdStr, out var userId) ? userId : Guid.Empty;
    }

    // GET: api/v1/notifications
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications([FromQuery] GetNotificationsQuery query)
    {
        var validation = await this.queryValidator.ValidateAsync(query);
        if (!validation.IsValid)
        {
            return this.BadRequest(validation.ToErrorResponse());
        }

        var limit = query.Limit;

        var userId = this.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return this.Unauthorized();
        }

        var notifications = await this.context.Notifications
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

        return this.Ok(notifications);
    }

    // PUT: api/v1/notifications/{id}/read
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = this.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return this.Unauthorized();
        }

        var notification = await this.context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null)
        {
            return this.NotFound();
        }

        notification.IsRead = true;
        await this.context.SaveChangesAsync();

        return this.NoContent();
    }

    // PUT: api/v1/notifications/read-all
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = this.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return this.Unauthorized();
        }

        await this.context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        return this.NoContent();
    }
}
