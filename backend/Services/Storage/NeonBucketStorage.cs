namespace AumoBackend.Services.Storage
{
    using System.Net;
    using System.Text.RegularExpressions;
    using Amazon.Runtime;
    using Amazon.S3;
    using Amazon.S3.Model;

    public sealed class NeonBucketStorage : IAvatarStorage, IDisposable
    {
        private const string KeyPrefix = "avatars/";
        private static readonly Regex FileNamePattern =
            new(@"^[A-Za-z0-9_\-]+\.(jpg|jpeg|png|gif|webp)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IConfiguration configuration;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly string bucket;
        private readonly AmazonS3Client? client;

        public bool IsConfigured => this.client != null;

        public NeonBucketStorage(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<NeonBucketStorage> logger)
        {
            this.configuration = configuration;
            this.httpContextAccessor = httpContextAccessor;

            var endpoint = configuration["AWS_ENDPOINT_URL_S3"];
            var accessKey = configuration["AWS_ACCESS_KEY_ID"];
            var secretKey = configuration["AWS_SECRET_ACCESS_KEY"];
            var region = configuration["AWS_REGION"] ?? "ap-southeast-1";
            this.bucket = configuration["S3_BUCKET"] ?? "assets";

            if (string.IsNullOrWhiteSpace(endpoint)
                || string.IsNullOrWhiteSpace(accessKey)
                || string.IsNullOrWhiteSpace(secretKey))
            {
                logger.LogWarning("Peringatan: 'AWS_ENDPOINT_URL_S3', 'AWS_ACCESS_KEY_ID' atau 'AWS_SECRET_ACCESS_KEY' belum dikonfigurasi. Upload avatar dinonaktifkan.");
                return;
            }

            var config = new AmazonS3Config
            {
                ServiceURL = endpoint,
                AuthenticationRegion = region,
                ForcePathStyle = true,

                // Penyimpanan S3-compatible tidak selalu mendukung checksum tambahan SDK terbaru.
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            };

            this.client = new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), config);
        }

        public async Task<string> UploadAvatarAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
        {
            var client = this.RequireClient();
            EnsureValidFileName(fileName);

            await client.PutObjectAsync(
                new PutObjectRequest
                {
                    BucketName = this.bucket,
                    Key = KeyPrefix + fileName,
                    InputStream = content,
                    ContentType = ContentTypeFor(fileName),
                }, cancellationToken);

            return this.BuildPublicUrl(fileName);
        }

        public async Task<AvatarObject?> GetAvatarAsync(string fileName, CancellationToken cancellationToken = default)
        {
            var client = this.RequireClient();
            if (!FileNamePattern.IsMatch(fileName))
            {
                return null;
            }

            try
            {
                var response = await client.GetObjectAsync(
                    new GetObjectRequest
                    {
                        BucketName = this.bucket,
                        Key = KeyPrefix + fileName,
                    }, cancellationToken);

                return new AvatarObject(response.ResponseStream, ContentTypeFor(fileName));
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        private string BuildPublicUrl(string fileName)
        {
            var baseUrl = this.configuration["PUBLIC_API_URL"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                var request = this.httpContextAccessor.HttpContext?.Request
                    ?? throw new InvalidOperationException("Set PUBLIC_API_URL: konteks HTTP tidak tersedia untuk membentuk URL avatar.");
                baseUrl = $"{request.Scheme}://{request.Host}";
            }

            return $"{baseUrl.TrimEnd('/')}/api/v1/avatars/{fileName}";
        }

        private static string ContentTypeFor(string fileName) =>
            Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream",
            };

        private static void EnsureValidFileName(string fileName)
        {
            if (!FileNamePattern.IsMatch(fileName))
            {
                throw new ArgumentException("Nama file avatar tidak valid.", nameof(fileName));
            }
        }

        private AmazonS3Client RequireClient() =>
            this.client ?? throw new InvalidOperationException("Neon bucket belum dikonfigurasi di server.");

        public void Dispose() => this.client?.Dispose();
    }
}
