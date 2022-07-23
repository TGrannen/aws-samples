using Amazon.SimpleEmail;
using AWSSamples.SES.Web.Senders;
using FluentEmail.Core.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var fromEmail = builder.Configuration.GetValue("SmtpConfig:DefaultFromAddress", "fromemail@test.test");
var host = builder.Configuration.GetValue("SmtpConfig:Host", "localhost");
var port = builder.Configuration.GetValue("SmtpConfig:Port", 25);

builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSimpleEmailService>();

builder.Services
    .AddFluentEmail(fromEmail)
    .AddRazorRenderer()
    .AddSmtpSender(host, port);

// Comment out to test with using SMTP sending instead of SDK
builder.Services.AddScoped<ISender, AwsSesSender>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();