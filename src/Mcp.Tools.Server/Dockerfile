FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Mcp.Tools.Server.csproj", "./"]
RUN dotnet restore "Mcp.Tools.Server.csproj"
COPY . .
RUN dotnet publish "Mcp.Tools.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mcp.Tools.Server.dll"]
