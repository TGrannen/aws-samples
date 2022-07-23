using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

var bucketName = builder.Configuration.GetValue("S3:BucketName", "Missing");
app.MapGet("Keys", async ([FromServices] IAmazonS3 s3) => { return (await GetS3Objects(bucketName, s3)).Select(x => x.Key); });
app.MapGet("Objects", async ([FromServices] IAmazonS3 s3) => await GetS3Objects(bucketName, s3));
app.MapPost("CreateBucket", async ([FromServices] IAmazonS3 s3) =>
{
    var bucketExists = await s3.DoesS3BucketExistAsync(bucketName);
    if (bucketExists)
    {
        return Results.BadRequest($"Bucket {bucketName} already exists.");
    }

    await s3.PutBucketAsync(bucketName);
    return Results.Ok($"Bucket {bucketName} created.");
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

async Task<List<S3Object>> GetS3Objects(string? name, IAmazonS3 amazonS3)
{
    var s3Objects = new List<S3Object>();
    var request = new ListObjectsRequest { BucketName = name };
    ListObjectsResponse response;
    do
    {
        response = await amazonS3.ListObjectsAsync(request);
        s3Objects.AddRange(response.S3Objects);
        request.Marker = response.NextMarker;
    } while (response.IsTruncated);

    return s3Objects;
}