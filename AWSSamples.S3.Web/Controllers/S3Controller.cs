using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;

namespace AWSSamples.S3.Web.Controllers;

[ApiController]
public class S3Controller : ControllerBase
{
    private readonly IAmazonS3 _s3Client;
    private readonly IConfiguration _configuration;

    public S3Controller(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _configuration = configuration;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var location = $"uploads/{file.FileName}";
        await using var stream = file.OpenReadStream();
        var putRequest = new PutObjectRequest
        {
            Key = location,
            BucketName = _configuration.GetValue("S3:BucketName", "Missing"),
            InputStream = stream,
            AutoCloseStream = true,
            ContentType = file.ContentType
        };
        var response = await _s3Client.PutObjectAsync(putRequest);
        return Ok(response);
    }
}