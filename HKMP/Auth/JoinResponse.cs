using Newtonsoft.Json;

namespace Hkmp.Auth;

/// <summary>
/// A join response received from the authentication server after a <see cref="JoinRequest"/> was sent.
/// </summary>
internal class JoinResponse {
    /// <summary>
    /// Whether the request was a success.
    /// </summary>
    [JsonIgnore]
    public bool Success { get; set; }
    
    /// <summary>
    /// The user ID of the client trying to join the server.
    /// </summary>
    [JsonProperty("uuid")]
    public string UserId { get; set; }
}
