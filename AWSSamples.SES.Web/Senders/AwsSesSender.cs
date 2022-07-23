using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;

namespace AWSSamples.SES.Web.Senders;

public class AwsSesSender : ISender
{
    private readonly IAmazonSimpleEmailService _client;
    private readonly ILogger<AwsSesSender> _logger;

    public AwsSesSender(IAmazonSimpleEmailService client, ILogger<AwsSesSender> logger)
    {
        _client = client;
        _logger = logger;
    }

    public SendResponse Send(IFluentEmail email, CancellationToken? token = null)
    {
        return SendAsync(email, token).GetAwaiter().GetResult();
    }

    public async Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null)
    {
        var response = new SendResponse();
        var mailMessage = CreateMailMessage(email);
        if (token.HasValue && token.GetValueOrDefault().IsCancellationRequested)
        {
            response.ErrorMessages.Add("Message was cancelled by cancellation token.");
            return response;
        }

        try
        {
            await _client.SendEmailAsync(mailMessage);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "AWS SES sending failed");
            response.ErrorMessages.Add(e.Message);
        }

        return response;
    }

    private static SendEmailRequest CreateMailMessage(IFluentEmail email)
    {
        var data = email.Data;
        var sendRequest = new SendEmailRequest
        {
            Source = data.FromAddress.EmailAddress,
            // if available use the source arn for authorisation
            Destination = new Destination
            {
                // TODO: have email addresses that are RFC standard with names
                ToAddresses = data.ToAddresses.Select(x => x.EmailAddress).ToList(),
                BccAddresses = data.BccAddresses.Select(x => x.EmailAddress).ToList(),
                CcAddresses = data.CcAddresses.Select(x => x.EmailAddress).ToList(),
            },
            Message = new Message
            {
                Subject = new Content
                {
                    Charset = "UTF-8",
                    Data = data.Subject
                },
                Body = new Body
                {
                    Html = new Content
                    {
                        Charset = "UTF-8",
                        Data = data.Body
                    },
                    Text = new Content
                    {
                        Charset = "UTF-8",
                        Data = string.IsNullOrWhiteSpace(data.PlaintextAlternativeBody) ? "" : data.PlaintextAlternativeBody,
                    },
                }
            }
        };
        return sendRequest;
    }
}