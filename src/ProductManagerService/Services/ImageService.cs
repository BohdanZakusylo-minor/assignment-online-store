using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace ProductManagerService.Services;

public class ImageService : IDisposable
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<ImageService> _logger;
    private readonly IConfiguration _configuration;
    private const string BucketName = "product-images";

    public ImageService(IConfiguration configuration, ILogger<ImageService> logger)
    {
        _logger = logger;
        _configuration = configuration;
        
        var endpoint = configuration["MinIO:Endpoint"] ?? "localhost:9000";
        var accessKey = configuration["MinIO:AccessKey"] ?? "minioadmin";
        var secretKey = configuration["MinIO:SecretKey"] ?? "minioadmin";
        
        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .Build();
        
        InitializeBucketAsync().Wait();
    }

    private async Task InitializeBucketAsync()
    {
        try
        {
            var found = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(BucketName));
            
            if (!found)
            {
                await _minioClient.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(BucketName));
                _logger.LogInformation($"Created bucket: {BucketName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error initializing bucket {BucketName}");
        }
    }

    public async Task<string> UploadImageAsync(IFormFile imageFile)
    {
        var objectName = $"product-{Guid.NewGuid()}-{imageFile.FileName}";
        
        using var stream = imageFile.OpenReadStream();
        
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(imageFile.Length)
            .WithContentType(imageFile.ContentType);
        
        await _minioClient.PutObjectAsync(putObjectArgs);
        
        var endpoint = _configuration["MinIO:Endpoint"] ?? "localhost:9000";
        var url = $"http://{endpoint}/{BucketName}/{objectName}";
        _logger.LogInformation($"Uploaded image: {objectName}");
        
        return url;
    }

    public async Task<List<string>> UploadImagesAsync(List<IFormFile> files)
    {
        var urls = new List<string>();
        
        foreach (var file in files)
        {
            var url = await UploadImageAsync(file);
            urls.Add(url);
        }
        
        return urls;
    }

    public async Task<bool> DeleteImageAsync(string objectName)
    {
        try
        {
            await _minioClient.RemoveObjectAsync(
                new RemoveObjectArgs()
                    .WithBucket(BucketName)
                    .WithObject(objectName));
            
            _logger.LogInformation($"Deleted image: {objectName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting image: {objectName}");
            return false;
        }
    }

    public void Dispose()
    {
        _minioClient?.Dispose();
    }
}

