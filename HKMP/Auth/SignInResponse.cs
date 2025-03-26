using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Hkmp.Auth;

/// <summary>
/// A sign-in response received from the authentication server.
/// </summary>
internal class SignInResponse {
    /// <summary>
    /// The status of the sign-in.
    /// </summary>
    [JsonProperty("status")]
    public SignInStatus Status { get; set; }
    
    /// <summary>
    /// The reason if the sign-in was not allowed.
    /// </summary>
    [JsonProperty("reason")]
    public string Reason { get; set; }
    
    /// <summary>
    /// The message if there was a general error with the request.
    /// </summary>
    [JsonProperty("message")]
    public string Message { get; set; }
    
    /// <summary>
    /// List of sign-in form fields if the request has errors in any of the fields.
    /// </summary>
    [JsonProperty("formFields")]
    public List<SignInFormField> FormFields { get; set; }
    
    /// <summary>
    /// The sign-in user if the request succeeded.
    /// </summary>
    [JsonProperty("user")]
    public SignInUser User { get; set; }

    /// <summary>
    /// Enumeration of different statuses for the sign-in.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum SignInStatus {
        /// <summary>
        /// The request was successful.
        /// </summary>
        OK,
        /// <summary>
        /// Wrong credentials were supplied.
        /// </summary>
        WRONG_CREDENTIALS_ERROR,
        /// <summary>
        /// Some of the fields contained an error.
        /// </summary>
        FIELD_ERROR,
        /// <summary>
        /// A general error occurred while handling the request.
        /// </summary>
        GENERAL_ERROR,
        /// <summary>
        /// Sign-in is not allowed.
        /// </summary>
        SIGN_IN_NOT_ALLOWED
    }

    /// <summary>
    /// Class for a sign-in user if the request was a success.
    /// </summary>
    public class SignInUser {
        /// <summary>
        /// The ID of the user.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
        /// <summary>
        /// The email of the user.
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; }
        /// <summary>
        /// The time when the user joined as a unix timestamp.
        /// </summary>
        [JsonProperty("timeJoined")]
        public ulong TimeJoined { get; set; }
        /// <summary>
        /// List of tenant IDs that this user belongs to.
        /// </summary>
        [JsonProperty("tenantIds")]
        public List<string> TenantIds { get; set; }
    }

    /// <summary>
    /// Class that represents a sign-in form field with an error.
    /// </summary>
    public class SignInFormField {
        /// <summary>
        /// The ID of the form field that has the error.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
        /// <summary>
        /// The error for the field as a string.
        /// </summary>
        [JsonProperty("error")]
        public string Error { get; set; }
    }
}
