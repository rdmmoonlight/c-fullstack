// <copyright file="HealthController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Controllers;

using System;
using System.Threading.Tasks;
using AumoBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("/health")]
[Route("/api/v1/health")]
[AumoBackend.Extensions.NoApiPrefix]
[AllowAnonymous]

public class HealthController : ControllerBase
{
    private readonly AppDbContext dbContext;

    public HealthController(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            // Cek koneksi ke database PostgreSQL via AppDbContext
            bool canConnect = await this.dbContext.Database.CanConnectAsync();

            if (canConnect)
            {
                return this.Ok(new
                {
                    status = "online",
                    database = "connected",
                    commit = Environment.GetEnvironmentVariable("RENDER_GIT_COMMIT"),
                    timestamp = DateTime.UtcNow,
                });
            }

            return this.StatusCode(503, new
            {
                status = "offline",
                database = "disconnected",
                timestamp = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            return this.StatusCode(503, new
            {
                status = "offline",
                database = "error",
                message = ex.Message,
                timestamp = DateTime.UtcNow,
            });
        }
    }
}
