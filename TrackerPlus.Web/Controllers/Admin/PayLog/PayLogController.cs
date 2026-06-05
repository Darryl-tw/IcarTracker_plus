using Microsoft.AspNetCore.Mvc;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Web.Controllers.Admin.PayLog;

public class PayLogController : AdminBaseController
{
    private readonly IPayLogService _payLogService;
    private readonly IMemberService _memberService;

    public PayLogController(IPayLogService payLogService, IMemberService memberService)
    {
        _payLogService = payLogService;
        _memberService = memberService;
    }

    public async Task<IActionResult> Index(string? keyword, string? status, DateTime? startDate, DateTime? endDate, int page = 1)
    {
        var filter = new QueryFilter
        {
            Keyword = keyword,
            Status = status,
            StartDate = startDate,
            EndDate = endDate,
            PageIndex = page,
            PageSize = 100
        };
        var result = await _payLogService.GetPayLogsPagedAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var payLog = await _payLogService.GetPayLogAsync(id);
        if (payLog == null) return NotFound();
        return View(payLog);
    }

    [HttpGet]
    public IActionResult Create() => View(new Core.Models.PayLog { CDate = DateTime.Now });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Core.Models.PayLog payLog)
    {
        var result = await _payLogService.CreatePayLogAsync(payLog);
        if (!result.Success) { ViewBag.Error = result.Message; return View(payLog); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Core.Models.PayLog payLog)
    {
        var result = await _payLogService.UpdatePayLogAsync(payLog);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Detail), new { id = payLog.TbKey });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _payLogService.DeletePayLogAsync(id);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int newMemberTbKey)
    {
        var result = await _payLogService.MovePayLogAsync(id, newMemberTbKey);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Index));
    }

    // ── 給首頁底部面板用的 JSON API ──────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetJson(int id)
    {
        var pl = await _payLogService.GetPayLogAsync(id);
        if (pl == null) return Json(new { success = false, message = "找不到訂單" });
        return Json(new
        {
            success  = true,
            tbKey    = pl.TbKey,
            imei     = pl.IMEICODE,
            orderNo  = pl.OrderNo,
            amount   = pl.Amount,
            saleModel= pl.SaleModel,
            sDate    = pl.SDate?.ToString("yyyy-MM-dd"),
            eDate    = pl.EDate?.ToString("yyyy-MM-dd"),
            memo     = pl.Memo,
            saleMemo = pl.SaleMemo,
            valueAddedWeb = pl.ValueAddedWeb
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateJson([FromBody] PayLogJsonRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Imei))
            return Json(new { success = false, message = "IMEI 不可空白" });
        var pl = new Core.Models.PayLog
        {
            IMEICODE     = req.Imei.Trim(),
            OrderNo      = req.OrderNo?.Trim() ?? string.Empty,
            Amount       = req.Amount,
            SaleModel    = req.SaleModel,
            SDate        = req.SDate,
            EDate        = req.EDate,
            Memo         = req.Memo?.Trim() ?? string.Empty,
            ValueAddedWeb= req.ValueAddedWeb,
            Operator     = AdminAccount
        };
        var result = await _payLogService.CreatePayLogAsync(pl);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateJson([FromBody] PayLogJsonRequest req)
    {
        if (req == null || req.TbKey <= 0)
            return Json(new { success = false, message = "未指定訂單" });
        var pl = await _payLogService.GetPayLogAsync(req.TbKey);
        if (pl == null) return Json(new { success = false, message = "找不到訂單" });
        pl.OrderNo       = req.OrderNo?.Trim() ?? pl.OrderNo;
        pl.Amount        = req.Amount;
        pl.SaleModel     = req.SaleModel;
        pl.SDate         = req.SDate;
        pl.EDate         = req.EDate;
        pl.Memo          = req.Memo?.Trim() ?? pl.Memo;
        pl.ValueAddedWeb = req.ValueAddedWeb;
        var result = await _payLogService.UpdatePayLogAsync(pl);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteJson([FromBody] SingleIdRequest req)
    {
        if (req == null || req.Id <= 0)
            return Json(new { success = false, message = "未指定訂單" });
        var result = await _payLogService.DeletePayLogAsync(req.Id);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> MoveJson([FromBody] PayLogMoveRequest req)
    {
        if (req == null || req.Id <= 0 || string.IsNullOrWhiteSpace(req.NewImei))
            return Json(new { success = false, message = "請填入目標 IMEI" });
        var result = await _payLogService.MovePayLogByImeiAsync(req.Id, req.NewImei.Trim());
        return Json(new { success = result.Success, message = result.Message });
    }
}

public class PayLogJsonRequest
{
    public int      TbKey        { get; set; }
    public string?  Imei         { get; set; }
    public string?  OrderNo      { get; set; }
    public decimal  Amount       { get; set; }
    public int      SaleModel    { get; set; }
    public DateTime? SDate       { get; set; }
    public DateTime? EDate       { get; set; }
    public string?  Memo         { get; set; }
    public int      ValueAddedWeb{ get; set; }
}
public class SingleIdRequest   { public int Id  { get; set; } }
public class PayLogMoveRequest { public int Id  { get; set; } public string? NewImei { get; set; } }
