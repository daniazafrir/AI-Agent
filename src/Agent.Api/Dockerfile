FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["Agent.Api/Agent.Api.csproj", "Agent.Api/"]
COPY ["Agent.Knowledge/Agent.Knowledge.csproj", "Agent.Knowledge/"]

RUN dotnet restore "Agent.Api/Agent.Api.csproj"

COPY . .

WORKDIR /src/Agent.Api

# Restore נוסף אחרי COPY
RUN dotnet restore "Agent.Api.csproj"

# בלי --no-restore
RUN dotnet publish "Agent.Api.csproj" \
    --configuration Release \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Agent.Api.dll"]