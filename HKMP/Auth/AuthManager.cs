using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Hkmp.Logging;
using Newtonsoft.Json;

namespace Hkmp.Auth;

/// <summary>
/// Class that manages authentication such as sending HTTP requests to the authentication server and handling received
/// responses.
/// </summary>
internal class AuthManager {
    /// <summary>
    /// Base URI for the authentication endpoint. TODO: change to configurable value in global settings
    /// </summary>
    private const string BaseAuthUri = "http://localhost:8888";
    /// <summary>
    /// Uri path of the sign-in endpoint.
    /// </summary>
    private const string SignInUriPath = "/auth/signin";
    /// <summary>
    /// Uri path of the session refresh endpoint.
    /// </summary>
    private const string SessionRefreshUriPath = "/auth/session/refresh";
    /// <summary>
    /// Uri path of the client join endpoint.
    /// </summary>
    private const string ClientJoinUriPath = "/client/join";
    /// <summary>
    /// Uri path of the server join endpoint.
    /// </summary>
    private const string ServerJoinUriPath = "/server/join";

    /// <summary>
    /// Name of the header that denotes that we use header based authentication for SuperTokens.
    /// </summary>
    private const string AuthHeaderName = "st-auth-mode";

    /// <summary>
    /// The HTTP client used to send requests to the authentication server.
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// The currently used access token for authentication.
    /// </summary>
    private string _accessToken;
    /// <summary>
    /// The currently used refresh token for refreshing the session if the access token is expired.
    /// </summary>
    private string _refreshToken;

    public AuthManager() {
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Send a sign-in request to the authentication server with the given email and password as credentials.
    /// </summary>
    /// <param name="email">The email address as a string.</param>
    /// <param name="password">The password as a string.</param>
    public async Task SendSignInRequest(string email, string password) {
        try {
            var signInRequest = new SignInRequest {
                Email = email,
                Password = password
            };

            var result = await SendRequest(
                SignInUriPath, 
                HttpMethod.Post, 
                AuthType.None, 
                signInRequest.Serialize()
            );

            if (result.ResultType == SendRequestResult.Type.Other) {
                Logger.Error($"Received non-ok (200) status code from sign-in response: {result.Response.StatusCode}, body:\n{result.ResponseBody}");
                return;
            }

            if (result.ResultType == SendRequestResult.Type.Error) {
                Logger.Error($"Exception from result of sending sign-in request to '{BaseAuthUri}{SignInUriPath}':\n{result.ErrorMessage}");
                return;
            }
            
            Logger.Info($"Sign-in response body:\n{result.ResponseBody}");

            var signInResponse = JsonConvert.DeserializeObject<SignInResponse>(result.ResponseBody);

            Logger.Info($"SignInResponse status: {signInResponse.Status}");
            if (signInResponse.Status == SignInResponse.SignInStatus.OK) {
                Logger.Info(
                    $"  User: id: {signInResponse.User.Id}, " +
                    $"email: {signInResponse.User.Email}, " +
                    $"timeJoined: {signInResponse.User.TimeJoined}, " +
                    $"tenantIds: {string.Join(", ", signInResponse.User.TenantIds)}"
                );
            } else if (signInResponse.Status == SignInResponse.SignInStatus.SIGN_IN_NOT_ALLOWED) {
                Logger.Info($"  Reason: {signInResponse.Reason}");
            } else if (signInResponse.Status == SignInResponse.SignInStatus.FIELD_ERROR) {
                Logger.Info("  FormFields:");
                foreach (var formField in signInResponse.FormFields) {
                    Logger.Info($"    id: {formField.Id}, error: {formField.Error}");
                }
            } else if (signInResponse.Status == SignInResponse.SignInStatus.GENERAL_ERROR) {
                Logger.Info($"  Message: {signInResponse.Message}");
            }
        } catch (Exception e) {
            Logger.Error($"Exception while trying to send sign-in request:\n{e}");
        }
    }

    /// <summary>
    /// Send a request to the given URI path of the authentication server.
    /// </summary>
    /// <param name="uriPath">The URI path to send the request to.</param>
    /// <param name="method">The HTTP method of the request.</param>
    /// <param name="authType">The type of authentication to use for the request.</param>
    /// <param name="requestBody">The request body as a string.</param>
    /// <param name="retry">Whether this is a retried request, meaning that it will not try to refresh expired access
    /// tokens.</param>
    /// <returns>The result of sending the request.</returns>
    private async Task<SendRequestResult> SendRequest(
        string uriPath, 
        HttpMethod method, 
        AuthType authType, 
        string requestBody = null,
        bool retry = false
    ) {
        try {
            using var request = new HttpRequestMessage();
            request.RequestUri = new Uri($"{BaseAuthUri}{uriPath}");
            request.Method = method;

            request.Headers.Add(AuthHeaderName, "header");
            if (authType == AuthType.Access) {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            } else if (authType == AuthType.Refresh) {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _refreshToken);
            }

            if (requestBody != null) {
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            }

            using var response = await _httpClient.SendAsync(request);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK) {
                HandleHttpResponseTokens(response);

                return new SendRequestResult {
                    ResultType = SendRequestResult.Type.Success,
                    Response = response,
                    ResponseBody = responseBody
                };
            }

            // For requests that use the access token for authentication and receive a 401, we do some
            // specific handling, such as refreshing the session
            if (authType == AuthType.Access && response.StatusCode == HttpStatusCode.Unauthorized && !retry) {
                // We have received a status code of 401 (Unauthorized), which means that our access token is expired.
                // So we send a new request to refresh the access token with our refresh token
                var refreshResult = await SendRequest(
                    SessionRefreshUriPath,
                    HttpMethod.Post,
                    AuthType.Refresh
                );

                // If the refresh result is an error, we simply return it for the calling method to handle
                if (refreshResult.ResultType == SendRequestResult.Type.Error) {
                    return refreshResult;
                }

                // If this refresh request was a success, we retry the original request to this method and return the
                // result. Note the additional flag that this new request is a 'retry', which means that if it comes
                // back as a 401 (Unauthorized) again, we simply skip trying to refresh the session, because that
                // did not work.
                if (refreshResult.ResultType == SendRequestResult.Type.Success) {
                    return await SendRequest(
                        uriPath,
                        method,
                        authType,
                        requestBody,
                        true
                    );
                }
                
                // If the refresh result is another 401 (Unauthorized), the refresh token has expired as well, and
                // the user has to log in again, so we return that
                if (refreshResult.ResultType == SendRequestResult.Type.Other) {
                    return new SendRequestResult {
                        ResultType = SendRequestResult.Type.SessionInvalid,
                        Response = response,
                        ResponseBody = responseBody
                    };
                }
            }

            return new SendRequestResult {
                ResultType = SendRequestResult.Type.Other,
                Response = response,
                ResponseBody = responseBody
            };
        } catch (Exception e) {
            return new SendRequestResult {
                ResultType = SendRequestResult.Type.Error,
                ErrorMessage = e.ToString()
            };
        }
    }

    /// <summary>
    /// Send a client join request to the authentication server that indicates that the client wants to join a certain
    /// server.
    /// </summary>
    /// <param name="token">The token to be included in the request as a string.</param>
    /// <returns>Whether the request succeeded or not.</returns>
    private async Task<bool> SendClientJoinRequest(string token) {
        try {
            var joinRequest = new JoinRequest {
                Token = token
            };

            var result = await SendRequest(
                ClientJoinUriPath,
                HttpMethod.Post,
                AuthType.Access,
                joinRequest.Serialize()
            );

            if (result.ResultType == SendRequestResult.Type.Error) {
                Logger.Error($"Exception from result of sending client join request:\n{result.ErrorMessage}");
                return false;
            }

            if (result.ResultType == SendRequestResult.Type.Other) {
                Logger.Error($"Received non-ok (200) status code from client join response: {result.Response.StatusCode}, body:\n{result.ResponseBody}");
                return false;
            }

            return true;
        } catch (Exception e) {
            Logger.Error($"Exception while trying to send client join request:\n{e}");
            return false;
        }
    }

    /// <summary>
    /// Send a server join request to the authentication server that validates whether a client has a valid session
    /// and can thus join the server.
    /// </summary>
    /// <param name="token">The token to be included in the request as a string.</param>
    /// <returns>The join response that indicates whether the request succeeded and additional details.</returns>
    private async Task<JoinResponse> SendServerJoinRequest(string token) {
        try {
            var joinRequest = new JoinRequest {
                Token = token
            };

            var result = await SendRequest(
                ServerJoinUriPath,
                HttpMethod.Post,
                AuthType.None,
                joinRequest.Serialize()
            );

            if (result.ResultType == SendRequestResult.Type.Error) {
                Logger.Error($"Exception from result of sending server join request:\n{result.ErrorMessage}");
                return new JoinResponse {
                    Success = false
                };
            }
            
            if (result.ResultType == SendRequestResult.Type.Other) {
                Logger.Error($"Received non-ok (200) status code from server join response: {result.Response.StatusCode}, body:\n{result.ResponseBody}");
                return new JoinResponse {
                    Success = false
                };
            }
            
            Logger.Info($"Server join request response body:\n{result.ResponseBody}");

            return JsonConvert.DeserializeObject<JoinResponse>(result.ResponseBody);
        } catch (Exception e) {
            Logger.Error($"Exception while trying to send server join request:\n{e}");
            return new JoinResponse {
                Success = false
            };
        }
    }

    /// <summary>
    /// Handles a received HTTP response by checking for a new access/refresh token and storing it.
    /// This method assumes that the HTTP status code was 200 (OK).
    /// </summary>
    /// <param name="response">The HTTP response instance.</param>
    private void HandleHttpResponseTokens(HttpResponseMessage response) {
        if (response.Headers.TryGetValues("st-access-token", out var headerValues)) {
            var headerArray = headerValues.ToArray();
            if (headerArray.Length > 0) {
                if (headerArray.Length > 1) {
                    Logger.Warn("HTTP response contained more than one entry of 'st-access-token'");
                } else {
                    _accessToken = headerArray[0];
                }                
            }
        }

        if (response.Headers.TryGetValues("st-refresh-token", out headerValues)) {
            var headerArray = headerValues.ToArray();
            if (headerArray.Length > 0) {
                if (headerArray.Length > 1) {
                    Logger.Warn("HTTP response contained more than one entry of 'st-refresh-token'");
                } else {
                    _refreshToken = headerArray[0];
                }                
            }
        }
    }

    /// <summary>
    /// Enumeration of different auth types for sending requests.
    /// </summary>
    private enum AuthType {
        /// <summary>
        /// No authentication included.
        /// </summary>
        None,
        /// <summary>
        /// Authentication with the access token.
        /// </summary>
        Access,
        /// <summary>
        /// Authentication with the refresh token.
        /// </summary>
        Refresh
    }
}
