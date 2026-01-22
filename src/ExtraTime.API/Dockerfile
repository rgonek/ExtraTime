FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/ExtraTime.API/ExtraTime.API.csproj", "src/ExtraTime.API/"]
COPY ["src/ExtraTime.Infrastructure/ExtraTime.Infrastructure.csproj", "src/ExtraTime.Infrastructure/"]
COPY ["src/ExtraTime.Application/ExtraTime.Application.csproj", "src/ExtraTime.Application/"]
COPY ["src/ExtraTime.Domain/ExtraTime.Domain.csproj", "src/ExtraTime.Domain/"]
RUN dotnet restore "src/ExtraTime.API/ExtraTime.API.csproj"
COPY . .
WORKDIR "/src/src/ExtraTime.API"
RUN dotnet build "ExtraTime.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ExtraTime.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExtraTime.API.dll"]
