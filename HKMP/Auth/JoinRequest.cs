using Newtonsoft.Json;

namespace Hkmp.Auth;

/// <summary>
/// A join request that can be used by either client or server to send to the authentication server. The token included
/// in the request is used to validate that a given client has a valid session with the authentication server before
/// joining a server.
/// </summary>
internal class JoinRequest {
    /// <summary>
    /// The token for the request that was issued from the server that is to be joined.
    /// </summary>
    [JsonProperty("token")]
    public string Token { get; set; }
    
    /// <summary>
    /// Serialize this request to a string.
    /// </summary>
    /// <returns>Serialized join request as a string.</returns>
    public string Serialize() {
        return JsonConvert.SerializeObject(this);
    }
}
