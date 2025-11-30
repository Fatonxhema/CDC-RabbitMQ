FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CDC.RetryProcessor/CDC.RetryProcessor.csproj", "CDC.RetryProcessor/"]
COPY ["CDC.Infrastructure/CDC.Infrastructure.csproj", "CDC.Infrastructure/"]
COPY ["CDC.Application/CDC.Application.csproj", "CDC.Application/"]
COPY ["CDC.Domain/CDC.Domain.csproj", "CDC.Domain/"]
RUN dotnet restore "CDC.RetryProcessor/CDC.RetryProcessor.csproj"
COPY . .
WORKDIR "/src/CDC.RetryProcessor"
RUN dotnet build "CDC.RetryProcessor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CDC.RetryProcessor.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CDC.RetryProcessor.dll"]
