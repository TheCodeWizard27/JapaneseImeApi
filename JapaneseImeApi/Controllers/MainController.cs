using Microsoft.AspNetCore.Mvc;

namespace JapaneseImeApi.Controllers;

[ApiController]
[Route("[controller]")]
public class MainController(ILogger<MainController> logger) : ControllerBase
{

    private readonly ILogger<MainController> _logger = logger;

    [HttpGet(Name = "Get")]
    public string Get()
    {
        return "Oke";
    }
}
