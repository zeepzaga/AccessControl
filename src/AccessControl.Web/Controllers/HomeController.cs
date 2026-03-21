using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AccessControl.Web.Models;

namespace AccessControl.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var model = ErrorViewModel.FromException(
            "Во время обработки запроса произошла ошибка. Подробности можно развернуть ниже.",
            feature?.Error,
            requestId);

        model.Title = "Что-то пошло не так";
        return View(model);
    }
}