using AWSSamples.SES.Web.Templates;
using FluentEmail.Core;
using Microsoft.AspNetCore.Mvc;

namespace AWSSamples.SES.Web.Controllers;

[ApiController]
public class FluentEmailExampleController : ControllerBase
{
    private readonly IFluentEmail _fluentEmail;
    private readonly IFluentEmailFactory _emailFactory;

    public FluentEmailExampleController(IFluentEmail fluentEmail, IFluentEmailFactory emailFactory)
    {
        _fluentEmail = fluentEmail;
        _emailFactory = emailFactory;
    }

    [HttpPost("Single")]
    public async Task<IActionResult> SendSingleEmail(string? fromAddress = null)
    {
        var email = _fluentEmail
            .To("test@test.test")
            .Subject("Test email")
            .Body("This is a single email");

        if (fromAddress != null)
        {
            email.SetFrom(fromAddress);
        }

        var result = await email.SendAsync();
        return result.Successful ? Ok(result) : BadRequest(result);
    }

    [HttpPost("Rendered")]
    public async Task<IActionResult> Rendered()
    {
        var template = "Dear @Model.Name, You are totally @Model.Compliment.";
        var model = new
        {
            Name = "Luke Lowrey",
            Position = "Developer",
            Message = "Hi Luke, this is an email message",
            Compliment = "Wow this is cool"
        };
        var email = _fluentEmail
            .To("test@test.test")
            .Subject("Test email")
            .UsingTemplate(template, model);

        var result = await email.SendAsync();
        return result.Successful ? Ok(result) : BadRequest(result);
    }

    [HttpPost("RenderedFromFile")]
    public async Task<IActionResult> RenderedFromFile()
    {
        var email = _fluentEmail
            .To("test@test.test")
            .Subject("Fancy email")
            .UsingTemplateFromFile("Templates/FancyEmailTemplate.cshtml", new FancyEmailTemplate { Name = "James", Value = 560 });

        var result = await email.SendAsync();
        return result.Successful ? Ok(result) : BadRequest(result);
    }

    [HttpPost("Multiple")]
    public async Task<IActionResult> SendMultipleEmail()
    {
        var email1 = _emailFactory
            .Create()
            .To("test@test.test")
            .Subject("Test email 1")
            .Body("This is the first email");

        await email1.SendAsync();

        var email2 = _emailFactory
            .Create()
            .To("test@test.test")
            .Subject("Test email 2")
            .Body("This is the second email");

        await email2.SendAsync();

        var result = await email2.SendAsync();
        return result.Successful ? Ok(result) : BadRequest(result);
    }
}