using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SoftwareLearningGuide.Api.Options;
using System.Text.Json.Serialization;

namespace SoftwareLearningGuide.Api.Controllers.TokenControllerExample;

/// <summary>
/// Controller de ayuda para obtener un access token de Cognito con usuario y contraseña
/// compatible con OAuth2 Password Flow de Swagger.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TokenController : ControllerBase {
    private readonly IOptions<CloudProvidersConfigurationOptions> _cloudProvidersOptions;

    public TokenController(IOptions<CloudProvidersConfigurationOptions> cloudProvidersOptions) {
        _cloudProvidersOptions = cloudProvidersOptions ?? throw new ArgumentNullException(nameof(cloudProvidersOptions));
    }

    /// <summary>
    /// POST api/v1/token
    /// Intercambia usuario/contraseña por un access token de Cognito.
    /// </summary>
    [HttpPost]
    [Consumes("application/x-www-form-urlencoded")] // Requisito de OAuth2
    [ProducesResponseType(typeof(OAuth2TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetToken([FromForm] OAuth2TokenRequest request, CancellationToken cancellationToken) {
        var aws = _cloudProvidersOptions.Value.AWS;
        var cognito = aws?.Cognito;
        if (cognito is null || !cognito.Enabled || string.IsNullOrEmpty(cognito.ClientId)) {
            return BadRequest(new { error = "Cognito no está configurado: AWS.Cognito.Enabled=false o ClientId vacío" });
        }

        var clientConfig = new AmazonCognitoIdentityProviderConfig {
            RegionEndpoint = RegionEndpoint.GetBySystemName(cognito.Region),
            ServiceURL = string.IsNullOrEmpty(cognito.ServiceUrl) ? null : cognito.ServiceUrl,
            UseHttp = !string.IsNullOrEmpty(cognito.ServiceUrl) && cognito.ServiceUrl.StartsWith("http://")
        };

        AWSCredentials credentials = !string.IsNullOrEmpty(aws?.Credentials.AccessKey)
            ? new BasicAWSCredentials(aws.Credentials.AccessKey, aws.Credentials.AccessSecret)
            : new AnonymousAWSCredentials();

        var client = new AmazonCognitoIdentityProviderClient(credentials, clientConfig);

        try {
            var response = await client.InitiateAuthAsync(new InitiateAuthRequest {
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                ClientId = cognito.ClientId,
                AuthParameters = new Dictionary<string, string> {
                    ["USERNAME"] = request.Username,
                    ["PASSWORD"] = request.Password
                }
            }, cancellationToken);

            // OAuth2 requiere access_token, token_type y expires_in
            return Ok(new OAuth2TokenResponse(
                AccessToken: response.AuthenticationResult.AccessToken,
                TokenType: "Bearer",
                ExpiresIn: response.AuthenticationResult.ExpiresIn,
                RefreshToken: response.AuthenticationResult.RefreshToken,
                IdToken: response.AuthenticationResult.IdToken
            ));
        }
        catch (NotAuthorizedException) {
            return Unauthorized(new { error = "Credenciales inválidas" });
        }
        catch (UserNotFoundException) {
            return Unauthorized(new { error = "Usuario no encontrado" });
        }
        catch (Exception ex) {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record OAuth2TokenRequest(
    [property: FromForm(Name = "username")] string Username,
    [property: FromForm(Name = "password")] string Password
);

public record OAuth2TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int? ExpiresIn,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("id_token")] string? IdToken
);