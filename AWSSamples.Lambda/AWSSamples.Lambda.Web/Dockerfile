FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["AWSSamples.Lambda/AWSSamples.Lambda.Web/AWSSamples.Lambda.Web.csproj", "AWSSamples.Lambda/AWSSamples.Lambda.Web/"]
RUN dotnet restore "AWSSamples.Lambda/AWSSamples.Lambda.Web/AWSSamples.Lambda.Web.csproj"
COPY . .
WORKDIR "/src/AWSSamples.Lambda/AWSSamples.Lambda.Web"
RUN dotnet build "AWSSamples.Lambda.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AWSSamples.Lambda.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AWSSamples.Lambda.Web.dll"]
