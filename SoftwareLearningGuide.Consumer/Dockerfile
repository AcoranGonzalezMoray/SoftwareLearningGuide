FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# --- Capa de restore ---
COPY SoftwareLearningGuide.Contracts/SoftwareLearningGuide.Contracts.csproj SoftwareLearningGuide.Contracts/
COPY SoftwareLearningGuide.Consumer/SoftwareLearningGuide.Consumer.csproj SoftwareLearningGuide.Consumer/

RUN dotnet restore SoftwareLearningGuide.Consumer/SoftwareLearningGuide.Consumer.csproj

# --- Capa de publicación ---
COPY . .
RUN dotnet publish SoftwareLearningGuide.Consumer/SoftwareLearningGuide.Consumer.csproj \
    -c Release -o /app/publish --no-restore

# --- Imagen final ---
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SoftwareLearningGuide.Consumer.dll"]