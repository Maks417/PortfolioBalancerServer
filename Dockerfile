FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
RUN apk add --no-cache curl

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ["PortfolioBalancerServer.csproj", "./"]
RUN dotnet restore "PortfolioBalancerServer.csproj"
COPY . .
RUN dotnet build "PortfolioBalancerServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PortfolioBalancerServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PortfolioBalancerServer.dll"]
