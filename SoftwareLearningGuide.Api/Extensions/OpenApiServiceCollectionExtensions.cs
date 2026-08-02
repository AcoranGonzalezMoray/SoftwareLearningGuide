namespace SoftwareLearningGuide.Api.Extensions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

public static class OpenApiServiceCollectionExtensions {
    public const string CognitoSecuritySchemeName = "CognitoOAuth2";

    public static IServiceCollection AddCustomOpenApi(this IServiceCollection services) {
        services.AddOpenApi("v1", ConfigureDocument);
        services.AddOpenApi("v2", ConfigureDocument);

        return services;
    }

    private static void ConfigureDocument(OpenApiOptions options) {
        options.AddDocumentTransformer((document, context, cancellationToken) => {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes[CognitoSecuritySchemeName] = new OpenApiSecurityScheme {
                Type = SecuritySchemeType.OAuth2,
                Description = "Ingresa tu usuario y contraseña de Cognito para autenticarte automáticamente.",
                Flows = new OpenApiOAuthFlows {
                    Password = new OpenApiOAuthFlow {
                        TokenUrl = new Uri("/api/v1/token", UriKind.Relative)
                    }
                }
            };

            return Task.CompletedTask;
        });

        options.AddOperationTransformer((operation, context, cancellationToken) => {
            var requiresAuth = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<AuthorizeAttribute>()
                .Any();

            if (!requiresAuth)
                return Task.CompletedTask;

            operation.Security ??= new List<OpenApiSecurityRequirement>();

            var securityRequirement = new OpenApiSecurityRequirement {
                [new OpenApiSecuritySchemeReference(CognitoSecuritySchemeName, context.Document)] = new List<string>()
            };

            operation.Security.Add(securityRequirement);

            return Task.CompletedTask;
        });
    }
}