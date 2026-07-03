using Microsoft.AspNetCore.Mvc;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;
using System;

namespace TrackerPlus.Web.Controllers.Admin.Dealer;

public class DealerController : AdminBaseController
{
    private readonly IDealerService _dealerService;

    public DealerController(IDealerService dealerService)
    {
        _dealerService = dealerService;
    }

    public async Task<IActionResult> Index(string? keyword, string? status)
    {
        var dealers = await _dealerService.GetDealersAsync(keyword, status);
        ViewBag.Keyword = keyword ?? string.Empty;
        ViewBag.Status  = status  ?? string.Empty;
        return View(dealers);
    }

    [HttpGet]
    public async Task<IActionResult> GetDealerJson(int id)
    {
        var dealer = await _dealerService.GetDealerAsync(id);
        if (dealer == null) return NotFound();
        return Json(new
        {
            tbKey              = dealer.TbKey,
            userCode           = dealer.UserCode,
            userName           = dealer.UserName,
            userID             = dealer.UserID,
            userPW             = dealer.UserPW,
            status             = dealer.Status,
            userMemo           = dealer.UserMemo,
            tel                = dealer.Tel,
            email              = dealer.Email,
            contact            = dealer.Contact,
            webtitle           = dealer.Webtitle,
            serviceEmail       = dealer.ServiceEmail,
            serviceSenderTitle = dealer.ServiceSenderTitle
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAjax(
        int tbKey, string? userCode, string? userName, string? userID,
        string? userPW, string? status, string? userMemo, string? tel,
        string? email, string? contact, string? webtitle,
        string? serviceEmail, string? serviceSenderTitle)
    {
        var existing = await _dealerService.GetDealerAsync(tbKey);
        if (existing == null)
            return Json(new { success = false, message = "找不到經銷商" });

        existing.UserCode           = userCode?.Trim()           ?? existing.UserCode;
        existing.UserName           = userName?.Trim()           ?? existing.UserName;
        existing.UserID             = userID?.Trim()             ?? existing.UserID;
        existing.UserMemo           = userMemo                   ?? string.Empty;
        existing.Tel                = tel                        ?? string.Empty;
        existing.Email              = email                      ?? string.Empty;
        existing.Contact            = contact                    ?? string.Empty;
        existing.Webtitle           = webtitle                   ?? string.Empty;
        existing.ServiceEmail       = serviceEmail               ?? string.Empty;
        existing.ServiceSenderTitle = serviceSenderTitle         ?? string.Empty;
        existing.Status             = status == "Y" ? "Y" : "N";

        if (!string.IsNullOrEmpty(userPW))
            existing.UserPW = userPW.Trim();

        var result = await _dealerService.UpdateDealerAsync(existing);
        if (!result.Success)
            return Json(new { success = false, message = result.Message });

        return Json(new { success = true, statusLabel = existing.Status == "Y" ? "啟用" : "停用" });
    }

    [HttpGet]
    public async Task<IActionResult> Popup(int id = 0, string mode = "view")
    {
        TrackerPlus.Core.Models.Dealer dealer;
        if (id > 0)
        {
            var found = await _dealerService.GetDealerAsync(id);
            if (found == null) return NotFound();
            dealer = found;
        }
        else
        {
            dealer = new TrackerPlus.Core.Models.Dealer { Status = "Y", CreateDate = DateTime.Now };
        }
        ViewBag.Mode = mode;
        return View(dealer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAjax(
        string? userCode, string? userName, string? userID,
        string? userPW, string? status, string? userMemo, string? tel,
        string? email, string? contact, string? webtitle,
        string? serviceEmail, string? serviceSenderTitle)
    {
        var dealer = new TrackerPlus.Core.Models.Dealer
        {
            UserCode           = userCode?.Trim()           ?? string.Empty,
            UserName           = userName?.Trim()           ?? string.Empty,
            UserID             = userID?.Trim()             ?? string.Empty,
            UserPW             = userPW?.Trim()             ?? string.Empty,
            Status             = status == "Y" ? "Y" : "N",
            UserMemo           = userMemo                   ?? string.Empty,
            Tel                = tel                        ?? string.Empty,
            Email              = email                      ?? string.Empty,
            Contact            = contact                    ?? string.Empty,
            Webtitle           = webtitle                   ?? string.Empty,
            ServiceEmail       = serviceEmail               ?? string.Empty,
            ServiceSenderTitle = serviceSenderTitle         ?? string.Empty,
        };
        var result = await _dealerService.CreateDealerAsync(dealer);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAjax(int tbKey)
    {
        var result = await _dealerService.DeleteDealerAsync(tbKey);
        return Json(new { success = result.Success, message = result.Message });
    }
}
