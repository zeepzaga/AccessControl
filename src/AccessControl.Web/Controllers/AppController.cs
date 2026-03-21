using System.Diagnostics;
using AccessControl.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.Web.Controllers;

public abstract class AppController : Controller
{
    protected void SetScreenError(string userMessage, Exception exception)
    {
        if (exception is ApiClientException apiException)
        {
            foreach (var entry in apiException.ValidationErrors)
            {
                var key = string.IsNullOrWhiteSpace(entry.Key) ? string.Empty : entry.Key;
                foreach (var error in entry.Value)
                {
                    ModelState.AddModelError(key, error);
                }
            }
        }

        ModelState.AddModelError(string.Empty, userMessage);

        ViewData["ScreenError"] = ErrorViewModel.FromException(
            userMessage,
            exception,
            Activity.Current?.Id ?? HttpContext.TraceIdentifier);
    }
}