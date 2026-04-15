namespace Vktun.Engine.Document;

internal static class DocumentContentTypeMatcher
{
    public static bool IsPdf(string? contentType)
    {
        return Matches(contentType, "application/pdf");
    }

    public static bool IsHtml(string? contentType)
    {
        return Matches(contentType, "text/html");
    }

    public static bool IsPlainText(string? contentType)
    {
        return Matches(contentType, "text/plain");
    }

    private static bool Matches(string? contentType, string expected)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType
            .Split(';', 2, StringSplitOptions.TrimEntries)[0]
            .Equals(expected, StringComparison.OrdinalIgnoreCase);
    }
}
