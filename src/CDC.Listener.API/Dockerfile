FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CDC.Listener.API/CDC.Listener.API.csproj", "CDC.Listener.API/"]
COPY ["CDC.Domain/CDC.Domain.csproj", "CDC.Domain/"]
COPY ["CDC.Application/CDC.Application.csproj", "CDC.Application/"]
RUN dotnet restore "CDC.Listener.API/CDC.Listener.API.csproj"
COPY . .
WORKDIR "/src/CDC.Listener.API"
RUN dotnet build "CDC.Listener.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CDC.Listener.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CDC.Listener.API.dll"]
