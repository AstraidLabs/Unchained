namespace Unchained.Models;

public class GeneratedFileResult
{
    public GeneratedFileResult(byte[] content, string contentType, string fileName)
    {
        Content = content;
        ContentType = contentType;
        FileName = fileName;
    }

    public byte[] Content { get; }
    public string ContentType { get; }
    public string FileName { get; }
}
