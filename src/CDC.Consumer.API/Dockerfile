FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CDC.Consumer.API/CDC.Consumer.API.csproj", "CDC.Consumer.API/"]
COPY ["CDC.Infrastructure/CDC.Infrastructure.csproj", "CDC.Infrastructure/"]
COPY ["CDC.Application/CDC.Application.csproj", "CDC.Application/"]
COPY ["CDC.Domain/CDC.Domain.csproj", "CDC.Domain/"]
RUN dotnet restore "CDC.Consumer.API/CDC.Consumer.API.csproj"
COPY . .
WORKDIR "/src/CDC.Consumer.API"
RUN dotnet build "CDC.Consumer.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CDC.Consumer.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CDC.Consumer.API.dll"]
