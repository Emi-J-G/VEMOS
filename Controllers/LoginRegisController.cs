using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VEMOS.Models;

namespace VEMOS.Controllers;

public class LoginRegisController : Controller
{
    private readonly ILogger<LoginRegisController> _logger;

    public LoginRegisController(ILogger<LoginRegisController> logger)
    {
        _logger = logger;
    }

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
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
