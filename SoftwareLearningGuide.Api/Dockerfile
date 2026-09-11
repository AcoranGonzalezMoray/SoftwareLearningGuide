FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# --- Capa de restore: solo copia los .csproj para aprovechar la caché de NuGet ---
COPY SoftwareLearningGuide.Core.Business/SoftwareLearningGuide.Core.Business.csproj SoftwareLearningGuide.Core.Business/
COPY SoftwareLearningGuide.Contracts/SoftwareLearningGuide.Contracts.csproj SoftwareLearningGuide.Contracts/
COPY SoftwareLearningGuide.Application/SoftwareLearningGuide.Application.csproj SoftwareLearningGuide.Application/
COPY SoftwareLearningGuide.Application.Command/SoftwareLearningGuide.Application.Command.csproj SoftwareLearningGuide.Application.Command/
COPY SoftwareLearningGuide.Application.Query/SoftwareLearningGuide.Application.Query.csproj SoftwareLearningGuide.Application.Query/
COPY SoftwareLearningGuide.Infraestructure.Data/SoftwareLearningGuide.Infraestructure.Data.csproj SoftwareLearningGuide.Infraestructure.Data/
COPY SoftwareLearningGuide.Infraestructure/SoftwareLearningGuide.Infraestructure.csproj SoftwareLearningGuide.Infraestructure/
COPY SoftwareLearningGuide.Api/SoftwareLearningGuide.Api.csproj SoftwareLearningGuide.Api/

RUN dotnet restore SoftwareLearningGuide.Api/SoftwareLearningGuide.Api.csproj

# --- Capa de publicación ---
COPY . .
RUN dotnet publish SoftwareLearningGuide.Api/SoftwareLearningGuide.Api.csproj \
    -c Release -o /app/publish --no-restore

# --- Imagen final ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SoftwareLearningGuide.Api.dll"]