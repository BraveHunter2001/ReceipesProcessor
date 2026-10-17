using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;

namespace Infrastructure;

public class S3StorageProvider
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3StorageProvider(IConfiguration configuration)
    {
        var s3Settings = configuration.GetSection("S3Settings");

        var config = new AmazonS3Config
        {
            ServiceURL = s3Settings["ServiceUrl"],
            ForcePathStyle = true // Обязательно для MinIO/локального тестирования
        };

        _s3Client = new AmazonS3Client(s3Settings["AccessKey"], s3Settings["SecretKey"], config);
        _bucketName = s3Settings["BucketName"] ?? throw new ArgumentNullException("Configs.S3Settings.BucketName");
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        using var fileTransferUtility = new TransferUtility(_s3Client);

        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = fileStream,
            Key = fileName, // Имя файла в облаке (например, "receipts/abc-123.pdf")
            BucketName = _bucketName,
            ContentType = contentType
        };

        await fileTransferUtility.UploadAsync(uploadRequest);

        // Возвращаем путь к файлу (или его ключ), который потом сохраним в PostgreSQL
        return fileName;
    }

    public async Task<(Stream FileStream, string ContentType)> GetFileAsync(string fileName)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName
            };

            GetObjectResponse response = await _s3Client.GetObjectAsync(request);
            return (response.ResponseStream, response.Headers.ContentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"Файл '{fileName}' не найден в хранилище S3.", fileName, ex);
        }
    }
}
