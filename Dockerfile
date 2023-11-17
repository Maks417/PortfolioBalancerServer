FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0.100-alpine3.18 AS build

WORKDIR /src
COPY ["PortfolioBalancerServer.csproj", "PortfolioBalancerServer/"]

RUN dotnet restore "PortfolioBalancerServer/PortfolioBalancerServer.csproj"

WORKDIR "/src/PortfolioBalancerServer"
COPY . .

RUN dotnet build "PortfolioBalancerServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PortfolioBalancerServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PortfolioBalancerServer.dll"]