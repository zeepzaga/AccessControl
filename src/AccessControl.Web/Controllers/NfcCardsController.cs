using AccessControl.Domain.Entities;
using AccessControl.Domain.Enums;
using AccessControl.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessControl.Web.Controllers;

public class NfcCardsController : Controller
{
    private readonly ApiClient _api;

    public NfcCardsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(
        [FromQuery] string? q = null,
        [FromQuery] string? cardType = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] string sort = "uid",
        [FromQuery] bool desc = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var cards = await _api.GetCardsAsync(q, cardType, isActive, employeeId);
        var model = PagedListViewModel<NfcCard>.Create(Sort(cards, sort, desc), page, pageSize, sort, desc);

        ViewBag.Query = q;
        ViewBag.CardType = cardType;
        ViewBag.IsActive = isActive;
        ViewBag.EmployeeId = employeeId;
        ViewBag.Sort = model.Sort;
        ViewBag.Desc = model.Desc;
        ViewBag.PageSize = model.PageSize;
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var card = await _api.GetCardAsync(id);
        if (card is null)
        {
            return NotFound();
        }

        if (card.EmployeeId.HasValue)
        {
            var rules = await _api.GetAccessRulesAsync(employeeId: card.EmployeeId);
            ViewBag.AccessRules = rules;
        }

        return View(card);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateLists();
        return View(new NfcCard());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NfcCard card)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLists();
            return View(card);
        }

        await _api.CreateCardAsync(card);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var card = await _api.GetCardAsync(id);
        if (card is null)
        {
            return NotFound();
        }

        await PopulateLists();
        return View(card);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, NfcCard card)
    {
        if (id != card.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateLists();
            return View(card);
        }

        await _api.UpdateCardAsync(card);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var card = await _api.GetCardAsync(id);
        if (card is null)
        {
            return NotFound();
        }

        return View(card);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _api.DeleteCardAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLists()
    {
        var employees = await _api.GetEmployeesAsync();
        ViewBag.EmployeeId = new SelectList(employees, "Id", "FullName");
        ViewBag.CardTypes = new SelectList(new[] { new { Value = CardType.Employee.ToString(), Text = "Сотрудник" }, new { Value = CardType.Guest.ToString(), Text = "Гость" } }, "Value", "Text");
    }

    private static IEnumerable<NfcCard> Sort(IEnumerable<NfcCard> cards, string sort, bool desc)
    {
        Func<NfcCard, object?> keySelector = sort.ToLowerInvariant() switch
        {
            "employee" => card => card.Employee?.FullName,
            "type" => card => card.CardType,
            "status" => card => card.IsActive,
            _ => card => card.Uid
        };

        return desc
            ? cards.OrderByDescending(keySelector).ThenBy(card => card.Uid)
            : cards.OrderBy(keySelector).ThenBy(card => card.Uid);
    }
}

