using Microsoft.AspNetCore.Http;

namespace FidelitasHub.Helpers
{
    public static class EmployeeDocumentStorage
    {
        public const string Aadhaar = "aadhaar";
        public const string Pan = "pan";

        private static readonly string[] AllowedExtensions =
        {
            ".pdf", ".jpg", ".jpeg", ".png"
        };

        public static string NormalizeType(string? documentType)
        {
            return documentType?.Trim().ToLowerInvariant() switch
            {
                Aadhaar => Aadhaar,
                "aadhar" => Aadhaar,
                Pan => Pan,
                _ => string.Empty
            };
        }

        public static bool IsAllowedExtension(string extension)
            => AllowedExtensions.Contains(
                extension,
                StringComparer.OrdinalIgnoreCase);

        public static string GetFolder(string contentRootPath, int employeeId)
        {
            return Path.Combine(
                contentRootPath,
                "App_Data",
                "EmployeeDocuments",
                employeeId.ToString());
        }

        public static string GetDisplayName(string documentType)
            => documentType == Aadhaar ? "Aadhaar Card" : "PAN Card";

        public static string GetFilePrefix(string documentType)
            => documentType == Aadhaar ? "Aadhaar" : "PAN";

        public static string? FindExistingFile(
            string contentRootPath,
            int employeeId,
            string documentType)
        {
            var type = NormalizeType(documentType);
            if (string.IsNullOrEmpty(type))
                return null;

            var folder = GetFolder(contentRootPath, employeeId);
            if (!Directory.Exists(folder))
                return null;

            var prefix = GetFilePrefix(type) + ".";

            return Directory
                .EnumerateFiles(folder)
                .FirstOrDefault(path =>
                    Path.GetFileName(path)
                        .StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        public static void DeleteExisting(
            string contentRootPath,
            int employeeId,
            string documentType)
        {
            var existing = FindExistingFile(
                contentRootPath,
                employeeId,
                documentType);

            if (existing != null && File.Exists(existing))
                File.Delete(existing);
        }

        public static async Task<string> SaveAsync(
            string contentRootPath,
            int employeeId,
            string documentType,
            IFormFile file)
        {
            var type = NormalizeType(documentType);
            if (string.IsNullOrEmpty(type))
                throw new InvalidOperationException("Invalid document type.");

            var extension = Path.GetExtension(file.FileName);
            if (!IsAllowedExtension(extension))
                throw new InvalidOperationException(
                    "Only PDF, JPG, JPEG and PNG files are allowed.");

            var folder = GetFolder(contentRootPath, employeeId);
            Directory.CreateDirectory(folder);

            DeleteExisting(contentRootPath, employeeId, type);

            var fileName = GetFilePrefix(type) + extension.ToLowerInvariant();
            var path = Path.Combine(folder, fileName);

            await using var stream = new FileStream(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            await file.CopyToAsync(stream);

            return path;
        }

        public static string GetContentType(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}
