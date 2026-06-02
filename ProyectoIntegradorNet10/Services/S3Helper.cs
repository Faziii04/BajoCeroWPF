using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace ProyectoIntegradorNet10.Services
{
    /// <summary>
    /// Helper for uploading files to Supabase S3-compatible storage.
    /// </summary>
    public static class S3Helper
    {
        private const string AccessKey = "aed96d6182b091e6a52107f61cd6a02c";
        private const string SecretKey = "439e8a9213a60d5dd3d43209297301b58acb7511593baf25061caf644874e5d7";
        private const string ServiceUrl = "https://kipxcnfckvulzsjukbws.storage.supabase.co/storage/v1/s3";
        private const string Region = "us-east-2";
        private const string BucketName = "images";
        private const string ProductsPrefix = "products";
        private const string EmployeesPrefix = "employees_pfp";
        private const string ClientsPrefix = "clients_pfp";
        private const string FamiliesPrefix = "families";
        private const string PagosPrefix = "pagos";
        private const string ImagesUrlBase = "https://kipxcnfckvulzsjukbws.supabase.co/storage/v1/object/public";

        private static readonly Lazy<AmazonS3Client> _s3Client = new(() =>
        {
            var credentials = new BasicAWSCredentials(AccessKey, SecretKey);
            var config = new AmazonS3Config
            {
                ServiceURL = ServiceUrl,
                AuthenticationRegion = Region,
                ForcePathStyle = true // Required for Supabase routing compatibility
            };
            return new AmazonS3Client(credentials, config);
        });

        private static AmazonS3Client Client => _s3Client.Value;

        /// <summary>
        /// Uploads a local image file to Supabase S3 storage.
        /// The file is stored as "employees_pfp/{ci}.jpg" in the "images" bucket.
        /// </summary>
        /// <param name="ci">The employee CI (used as the filename).</param>
        /// <param name="localFilePath">The full local path to the image file.</param>
        /// <returns>The public URL of the uploaded image, or null if the upload failed.</returns>
        public static async Task<string?> UploadEmployeeImageAsync(string ci, string localFilePath)
        {
            if (string.IsNullOrEmpty(ci))
                throw new ArgumentException("CI cannot be null or empty.", nameof(ci));

            if (string.IsNullOrEmpty(localFilePath) || !File.Exists(localFilePath))
                throw new FileNotFoundException("Local image file not found.", localFilePath);

            // Determine file extension from the source file
            string extension = Path.GetExtension(localFilePath)?.ToLowerInvariant() ?? ".jpg";
            string key = $"{EmployeesPrefix}/{ci}{extension}";

            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = BucketName,
                    Key = key,
                    FilePath = localFilePath
                };

                PutObjectResponse response = await Client.PutObjectAsync(putRequest);

                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    // Return the public URL
                    // Supabase S3 public URL format: {ServiceURL}/storage/v1/object/public/{bucket}/{key}
                    return $"{ImagesUrlBase}/{BucketName}/{key}";
                }

                return null;
            }
            catch (AmazonS3Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"S3 upload error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Uploads a client's profile image to Supabase S3 storage.
        /// The file is stored as "clients_pfp/{ci}.jpg" in the "images" bucket.
        /// </summary>
        /// <param name="ci">The client CI (used as the filename).</param>
        /// <param name="localFilePath">The full local path to the image file.</param>
        /// <returns>The public URL of the uploaded image, or null if the upload failed.</returns>
        public static async Task<string?> UploadClientImageAsync(string ci, string localFilePath)
        {
            if (string.IsNullOrEmpty(ci))
                throw new ArgumentException("CI cannot be null or empty.", nameof(ci));

            if (string.IsNullOrEmpty(localFilePath) || !File.Exists(localFilePath))
                throw new FileNotFoundException("Local image file not found.", localFilePath);

            // Determine file extension from the source file
            string extension = Path.GetExtension(localFilePath)?.ToLowerInvariant() ?? ".jpg";
            string key = $"{ClientsPrefix}/{ci}{extension}";

            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = BucketName,
                    Key = key,
                    FilePath = localFilePath
                };

                PutObjectResponse response = await Client.PutObjectAsync(putRequest);

                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    // Return the public URL
                    return $"{ImagesUrlBase}/{BucketName}/{key}";
                }

                return null;
            }
            catch (AmazonS3Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"S3 upload error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Uploads a product image to Supabase S3 storage.
        /// The file is stored as "products/{id}.jpg" in the "images" bucket.
        /// </summary>
        /// <param name="id">The product ID (used as the filename).</param>
        /// <param name="localFilePath">The full local path to the image file.</param>
        /// <returns>The public URL of the uploaded image, or null if the upload failed.</returns>
        public static async Task<string?> UploadProductImageAsync(int id, string localFilePath)
        {
            if (id <= 0)
                throw new ArgumentException("Product ID must be greater than zero.", nameof(id));

            if (string.IsNullOrEmpty(localFilePath) || !File.Exists(localFilePath))
                throw new FileNotFoundException("Local image file not found.", localFilePath);

            // Determine file extension from the source file
            string extension = Path.GetExtension(localFilePath)?.ToLowerInvariant() ?? ".jpg";
            string key = $"{ProductsPrefix}/{id}{extension}";

            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = BucketName,
                    Key = key,
                    FilePath = localFilePath
                };

                PutObjectResponse response = await Client.PutObjectAsync(putRequest);

                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    // Return the public URL
                    return $"{ImagesUrlBase}/{BucketName}/{key}";
                }

                return null;
            }
            catch (AmazonS3Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"S3 upload error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes an employee's image from Supabase S3 storage.
        /// </summary>
        /// <param name="ci">The employee CI (used as the filename).</param>
        public static async Task DeleteEmployeeImageAsync(string ci)
        {
            if (string.IsNullOrEmpty(ci)) return;

            try
            {
                // Try common extensions
                string[] extensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                foreach (var ext in extensions)
                {
                    string key = $"{EmployeesPrefix}{ci}{ext}";
                    var deleteRequest = new DeleteObjectRequest
                    {
                        BucketName = BucketName,
                        Key = key
                    };
                    await Client.DeleteObjectAsync(deleteRequest);
                }
            }
            catch (AmazonS3Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"S3 delete error: {ex.Message}");
                // Don't throw — deletion is best-effort
            }
        }

        /// <summary>
        /// Deletes a client's image from Supabase S3 storage.
        /// </summary>
        /// <param name="ci">The client CI (used as the filename).</param>
        public static async Task DeleteClientImageAsync(string ci)
        {
            if (string.IsNullOrEmpty(ci)) return;

            try
            {
                // Try common extensions
                string[] extensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                foreach (var ext in extensions)
                {
                    string key = $"{ClientsPrefix}{ci}{ext}";
                    var deleteRequest = new DeleteObjectRequest
                    {
                        BucketName = BucketName,
                        Key = key
                    };
                    await Client.DeleteObjectAsync(deleteRequest);
                }
            }
            catch (AmazonS3Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"S3 delete error: {ex.Message}");
                // Don't throw — deletion is best-effort
            }
        }

        /// <summary>
        /// Deletes a product's image from Supabase S3 storage.
        /// </summary>
        /// <param name="id">The product ID (used as the filename).</param>
        public static async Task DeleteProductImageAsync(int id)
        {
            if (id <= 0) return;

            try
            {
                // Try common extensions
                string[] extensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                foreach (var ext in extensions)
                {
                    string key = $"{ProductsPrefix}/{id}{ext}";
                    var deleteRequest = new DeleteObjectRequest
                    {
                        BucketName = BucketName,
                        Key = key
                    };
                    await Client.DeleteObjectAsync(deleteRequest);
                }
            }
            catch (AmazonS3Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"S3 delete error: {ex.Message}");
                // Don't throw — deletion is best-effort
            }
        }
    }
}
