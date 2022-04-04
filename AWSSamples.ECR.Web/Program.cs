using Amazon.ECR;
using Amazon.ECR.Model;
using Microsoft.AspNetCore.Mvc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((_, configuration) => configuration.WriteTo.Console());

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAWSService<IAmazonECR>();

var app = builder.Build();

app.MapGet("/GetTokens",
    async ([FromServices] IAmazonECR awsEcr) => await awsEcr.GetAuthorizationTokenAsync(new GetAuthorizationTokenRequest()));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();