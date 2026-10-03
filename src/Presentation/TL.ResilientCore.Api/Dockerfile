FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/Core/TL.ResilientCore.Domain/TL.ResilientCore.Domain.csproj", "src/Core/TL.ResilientCore.Domain/"]
COPY ["src/Core/TL.ResilientCore.Application/TL.ResilientCore.Application.csproj", "src/Core/TL.ResilientCore.Application/"]
COPY ["src/Infrastructure/TL.ResilientCore.Infrastructure/TL.ResilientCore.Infrastructure.csproj", "src/Infrastructure/TL.ResilientCore.Infrastructure/"]
COPY ["src/Presentation/TL.ResilientCore.Api/TL.ResilientCore.Api.csproj", "src/Presentation/TL.ResilientCore.Api/"]

RUN dotnet restore "src/Presentation/TL.ResilientCore.Api/TL.ResilientCore.Api.csproj"

COPY src/ src/

WORKDIR "/src/src/Presentation/TL.ResilientCore.Api"
RUN dotnet publish "TL.ResilientCore.Api.csproj" -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/nightly/aspnet:9.0-chiseled-extra AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TL.ResilientCore.Api.dll"]
