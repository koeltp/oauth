namespace OAuth.Infrastructure.Options;

public class UploadOptions
{
    public const string SectionName = "Upload";
    
    public int MaxFileSizeMB { get; set; } = 2;
    public List<string> AllowedExtensions { get; set; } = new List<string> { ".jpg", ".jpeg", ".png", ".gif" };

    public long MaxFileSizeBytes => MaxFileSizeMB * 1024 * 1024;
}