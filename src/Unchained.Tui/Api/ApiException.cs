using System.Net;

namespace Unchained.Tui.Api;

public class ApiException : Exception
{
    public ApiError Error { get; }
    public HttpStatusCode StatusCode { get; }

    public ApiException(string message, ApiError error, HttpStatusCode statusCode) : base(message)
    {
        Error = error;
        StatusCode = statusCode;
    }
}
