using System.Net.Http;

namespace Hkmp.Auth;

/// <summary>
/// Class for the result of a request that was sent to the authentication server.
/// </summary>
internal class SendRequestResult {
    /// <summary>
    /// The type of the result.
    /// </summary>
    public Type ResultType { get; set; }

    /// <summary>
    /// The HTTP response of the request. Can be null if the request failed and did not yield a response.
    /// </summary>
    public HttpResponseMessage Response { get; set; }
    /// <summary>
    /// The body of the response of the request. Can be null if the request failed and did not yield a response.
    /// </summary>
    public string ResponseBody { get; set; }

    /// <summary>
    /// The error message from the request if the request failed with an error.
    /// </summary>
    public string ErrorMessage { get; set; }

    public enum Type {
        Success,
        SessionInvalid,
        Other,
        Error
    }
}
