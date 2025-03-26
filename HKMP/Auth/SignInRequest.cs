using System.Collections.Generic;
using Newtonsoft.Json;

namespace Hkmp.Auth;

/// <summary>
/// A sign-in request that can be sent to the authentication server.
/// </summary>
internal class SignInRequest {
    /// <summary>
    /// The name of the parameter for the email.
    /// </summary>
    private const string EmailParameterName = "email";
    /// <summary>
    /// The name of the parameter for the password.
    /// </summary>
    private const string PasswordParameterName = "password";
    
    /// <summary>
    /// List of parameters for the request. This is necessary to properly serialize the request into the format
    /// that the authentication server requires.
    /// </summary>
    [JsonProperty("formFields")]
    private List<SignInRequestParameters> _parameters;
    
    /// <summary>
    /// The email for the request.
    /// </summary>
    [JsonIgnore]
    public string Email { get; set; }
    /// <summary>
    /// The password for the request.
    /// </summary>
    [JsonIgnore]
    public string Password { get; set; }

    /// <summary>
    /// Serialize this request to a string.
    /// </summary>
    /// <returns>Serialized sign-in request as a string.</returns>
    public string Serialize() {
        _parameters = [
            new SignInRequestParameters {
                Id = EmailParameterName,
                Value = Email
            },

            new SignInRequestParameters {
                Id = PasswordParameterName,
                Value = Password
            }
        ];

        return JsonConvert.SerializeObject(this);
    }

    /// <summary>
    /// Simple class that represents a sign-in request parameter (such as email or password).
    /// </summary>
    private class SignInRequestParameters {
        /// <summary>
        /// The ID that indicates what the parameter means.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
        /// <summary>
        /// The value of the parameter, either the email or the password value.
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
