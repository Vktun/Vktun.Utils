namespace Vktun.Engine.Document;

internal static class DocumentFileNames
{
    public static string EnsureExtension(string? fileName, string contentType)
    {
        var normalizedName = string.IsNullOrWhiteSpace(fileName) ? "document" : fileName.Trim();
        if (!string.IsNullOrWhiteSpace(Path.GetExtension(normalizedName)))
        {
            return normalizedName;
        }

        return normalizedName + GetExtension(contentType);
    }

    private static string GetExtension(string contentType)
    {
        if (DocumentContentTypeMatcher.IsPdf(contentType))
        {
            return ".pdf";
        }

        if (DocumentContentTypeMatcher.IsHtml(contentType))
        {
            return ".html";
        }

        if (DocumentContentTypeMatcher.IsPlainText(contentType))
        {
            return ".txt";
        }

        return ".bin";
    }
}
