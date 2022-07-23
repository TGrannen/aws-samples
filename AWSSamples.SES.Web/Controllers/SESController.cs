using Amazon.SimpleEmail;
using Microsoft.AspNetCore.Mvc;

namespace AWSSamples.SES.Web.Controllers;

[ApiController]
public class SESController : ControllerBase
{
    private readonly IAmazonSimpleEmailService _service;

    public SESController(IAmazonSimpleEmailService service)
    {
        _service = service;
    }

    [HttpGet("Identities")]
    public async Task<IActionResult> ListIdentitiesAsync()
    {
        var result = await _service.ListIdentitiesAsync();
        return Ok(result);
    }

    [HttpGet("VerifiedEmailAddresses")]
    public async Task<IActionResult> ListVerifiedEmailAddressesAsync()
    {
        var result = await _service.ListVerifiedEmailAddressesAsync();
        return Ok(result);
    }
}