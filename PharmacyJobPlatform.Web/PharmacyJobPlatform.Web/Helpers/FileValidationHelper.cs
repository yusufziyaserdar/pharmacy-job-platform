namespace PharmacyJobPlatform.Web.Helpers
{
    public static class FileValidationHelper
    {
        public static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public static readonly HashSet<string> AllowedCvExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".txt", ".doc", ".docx"
        };

        public static bool HasAllowedExtension(IFormFile? file, HashSet<string> allowedExtensions)
        {
            if (file == null || file.Length == 0)
            {
                return true;
            }

            var extension = Path.GetExtension(file.FileName);
            return !string.IsNullOrWhiteSpace(extension) && allowedExtensions.Contains(extension);
        }
    }
}
