using Amazon.ECR;
using Amazon.ECR.Model;
using Microsoft.AspNetCore.Mvc;

namespace AWSSamples.ECR.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class ECRTestController : ControllerBase
{
    private readonly IAmazonECR _client;
    public ECRTestController(IAmazonECR client)
    {
        _client = client;
    }

    [HttpGet(Name = "GetToken")]
    public async Task<IActionResult> GetToken()
    {
        var result = await _client.GetAuthorizationTokenAsync(new GetAuthorizationTokenRequest());
        return Ok(result);
    }
}