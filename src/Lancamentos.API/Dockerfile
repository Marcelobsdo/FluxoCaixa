FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Lancamentos.API/Lancamentos.API.csproj", "src/Lancamentos.API/"]
COPY ["src/Lancamentos.Application/Lancamentos.Application.csproj", "src/Lancamentos.Application/"]
COPY ["src/Lancamentos.Domain/Lancamentos.Domain.csproj", "src/Lancamentos.Domain/"]
COPY ["src/Lancamentos.Infrastructure/Lancamentos.Infrastructure.csproj", "src/Lancamentos.Infrastructure/"]
COPY ["src/Shared.Events/Shared.Events.csproj", "src/Shared.Events/"]

RUN dotnet restore "src/Lancamentos.API/Lancamentos.API.csproj"

COPY . .
WORKDIR "src/Lancamentos.API"

RUN dotnet publish "Lancamentos.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Lancamentos.API.dll"]
