// <copyright file="GeneralLedgersController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Controllers.Reports;

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AumoBackend.Services.Reports.GeneralLedgers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("/reports/general-ledgers")]
[Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]
public class GeneralLedgersController : ControllerBase
{
    private readonly IGeneralLedgersService glService;

    public GeneralLedgersController(IGeneralLedgersService glService) => this.glService = glService;

    [HttpGet("permanent")]
    public async Task<IActionResult> GetPermanentGeneralLedger()
    {
        var userId = this.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return this.Unauthorized(new { success = false, message = "User identity invalid." });
        }

        var result = await this.glService.GetPermanentLedgersAsync(userId);
        if (!result.Success || result.Data == null)
        {
            return this.NotFound(new { success = false, hasPeriodSelected = false, message = result.Message });
        }

        return this.Ok(new { success = true, hasPeriodSelected = true, selectedPeriodName = result.Data.SelectedPeriodName, isTemporary = false, accounts = result.Data.Accounts });
    }

    [HttpGet("temporary")]
    public async Task<IActionResult> GetTemporaryGeneralLedger()
    {
        var userId = this.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return this.Unauthorized(new { success = false, message = "User identity invalid." });
        }

        var result = await this.glService.GetTemporaryLedgersAsync(userId);
        if (!result.Success || result.Data == null)
        {
            return this.NotFound(new { success = false, hasPeriodSelected = false, message = result.Message });
        }

        return this.Ok(new
        {
            success = true,
            hasPeriodSelected = true,
            selectedPeriodName = result.Data.SelectedPeriodName,
            isTemporary = true,
            netIncomeBeforeClosing = result.Data.NetIncomeBeforeClosing,
            accounts = result.Data.Accounts,
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshLedgerData()
    {
        var userId = this.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return this.Unauthorized(new { success = false, message = "User identity invalid." });
        }

        var result = await this.glService.RefreshGeneralLedgersAsync(userId);
        if (!result.Success)
        {
            return this.BadRequest(result);
        }

        return this.Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var id = this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub");
        return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
    }
}
