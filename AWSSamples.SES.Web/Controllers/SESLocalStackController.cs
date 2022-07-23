using Medallion.Shell;
using Microsoft.AspNetCore.Mvc;

namespace AWSSamples.SES.Web.Controllers;

[ApiController]
public class SESLocalStackController : ControllerBase
{
    [HttpGet("GetSentEmails")]
    public async Task<IActionResult> GetSentEmails()
    {
        var client = new HttpClient { BaseAddress = new Uri("http://ses.localhost:4566/") };
        var result = await client.GetFromJsonAsync<object>("_localstack/ses");
        return Ok(result);
    }

    [HttpPost("Verify")]
    public async Task<IActionResult> VerifyEmail(string emailAddress)
    {
        var command = Command.Run("aws", "ses", "verify-email-identity", "--email-address", emailAddress,
            "--endpoint-url=http://localhost:4566");
        await command.Task;
        return Ok(command.Result.Success ? await command.StandardOutput.ReadToEndAsync() : await command.StandardError.ReadToEndAsync());
    }
}