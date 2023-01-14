using Microsoft.AspNetCore.Mvc;

namespace AWSSamples.Lambda.Web.Controllers;

[ApiController]
[Route("/")]
public class HomeController : ControllerBase
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "Home")]
    public IActionResult Get()
    {
        var now = DateTime.Now;
        _logger.LogInformation("Home page was hit. Time returned {Time}", now);
        return Ok($"Hey, you made it! The current time is: {now}");
    }
}