using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cloudsoft.Web.Models;
using Cloudsoft.Core.Storage;

namespace Cloudsoft.Web.Controllers;

/* public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData["HeroImageUrl"] = _imageService.GetImageUrl("images/hero.png");
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
} */

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IImageService _imageService;

    public HomeController(ILogger<HomeController> logger, IImageService imageService)
    {
        _logger = logger;
        _imageService = imageService;
    }

    public IActionResult Index()
    {
        ViewData["HeroImageUrl"] = _imageService.GetImageUrl("hero.png");
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

    public IActionResult About()
    {
        ViewData["HeroImageUrl"] = _imageService.GetImageUrl("hero.png");
        return View();
    }
}
