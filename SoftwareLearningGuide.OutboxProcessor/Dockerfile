FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SoftwareLearningGuide.Core.Business/SoftwareLearningGuide.Core.Business.csproj SoftwareLearningGuide.Core.Business/
COPY SoftwareLearningGuide.Application/SoftwareLearningGuide.Application.csproj SoftwareLearningGuide.Application/
COPY SoftwareLearningGuide.Application.Command/SoftwareLearningGuide.Application.Command.csproj SoftwareLearningGuide.Application.Command/
COPY SoftwareLearningGuide.Infraestructure.Data/SoftwareLearningGuide.Infraestructure.Data.csproj SoftwareLearningGuide.Infraestructure.Data/
COPY SoftwareLearningGuide.Contracts/SoftwareLearningGuide.Contracts.csproj SoftwareLearningGuide.Contracts/
COPY SoftwareLearningGuide.OutboxProcessor/SoftwareLearningGuide.OutboxProcessor.csproj SoftwareLearningGuide.OutboxProcessor/

RUN dotnet restore SoftwareLearningGuide.OutboxProcessor/SoftwareLearningGuide.OutboxProcessor.csproj

COPY . .
RUN dotnet publish SoftwareLearningGuide.OutboxProcessor/SoftwareLearningGuide.OutboxProcessor.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SoftwareLearningGuide.OutboxProcessor.dll"]
