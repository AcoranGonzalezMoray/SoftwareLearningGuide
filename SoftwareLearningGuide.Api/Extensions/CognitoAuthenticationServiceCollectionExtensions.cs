using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SoftwareLearningGuide.Api.Options;

namespace SoftwareLearningGuide.Api.Extensions {
    public static class CognitoRoles {
        public const string Admin = "admin";
        public const string Normal = "normal";
    }

    public static class CognitoPolicies {
        public const string RequireAdminRole = "RequireAdminRole";
        public const string RequireNormalRole = "RequireNormalRole";
    }

    public static class CognitoAuthenticationServiceCollectionExtensions {
        public static IServiceCollection AddCognitoAuthentication(this IServiceCollection services, IConfiguration configuration) {
            var cognitoOptions = configuration.GetCognitoOptions();

            // Se registran SIEMPRE los servicios de autenticación/autorización:
            // Program.cs llama a UseAuthentication()/UseAuthorization() de forma incondicional
            // y el middleware necesita estos servicios registrados o la app no arranca.
            services.AddAuthorization();

            if (!cognitoOptions.Enabled || string.IsNullOrEmpty(cognitoOptions.UserPoolId) || string.IsNullOrEmpty(cognitoOptions.ClientId))
                return services;

            var issuer = $"https://cognito-idp.{cognitoOptions.Region}.amazonaws.com";
            var authority = string.IsNullOrEmpty(cognitoOptions.ServiceUrl)
                ? $"{issuer}/{cognitoOptions.UserPoolId}"
                : $"{cognitoOptions.ServiceUrl}/{cognitoOptions.UserPoolId}";

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.Authority = authority;
                    options.RequireHttpsMetadata = string.IsNullOrEmpty(cognitoOptions.ServiceUrl);
                    options.TokenValidationParameters = new TokenValidationParameters {
                        ValidateIssuer = true,
                        ValidIssuer = $"{issuer}/{cognitoOptions.UserPoolId}",
                        ValidateAudience = true,
                        ValidAudience = cognitoOptions.ClientId,
                        AudienceValidator = ValidateCognitoAudience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });

            services.AddAuthorization(options => {
                options.AddPolicy(CognitoPolicies.RequireNormalRole, policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireClaim("cognito:groups", CognitoRoles.Admin, CognitoRoles.Normal));

                options.AddPolicy(CognitoPolicies.RequireAdminRole, policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireClaim("cognito:groups", CognitoRoles.Admin));
            });

            return services;
        }

        // Cognito emite dos tipos de token: el access token lleva el claim "client_id"
        // (no "aud"), mientras que el id token lleva "aud". Aceptamos ambos validando
        // que cualquiera de ellos coincida con el ClientId configurado.
        private static bool ValidateCognitoAudience(IEnumerable<string> audiences, SecurityToken securityToken, TokenValidationParameters validationParameters) {
            var clientId = securityToken switch {
                JwtSecurityToken jwt => jwt.Claims.FirstOrDefault(claim => claim.Type == "client_id")?.Value,
                JsonWebToken jwt when jwt.TryGetPayloadValue<string>("client_id", out var value) => value,
                _ => null
            };

            if (!string.IsNullOrEmpty(clientId))
                return string.Equals(clientId, validationParameters.ValidAudience, StringComparison.Ordinal);

            return audiences?.Contains(validationParameters.ValidAudience) == true;
        }
    }
}
