# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os projetos para restaurar dependências
COPY ["WebAPI/WebAPI.csproj", "WebAPI/"]
COPY ["Infra/Infra.csproj", "Infra/"]
COPY ["Core/Core.csproj", "Core/"]
RUN dotnet restore "WebAPI/WebAPI.csproj"

# Copia todo o código e publica
COPY . .
WORKDIR /src/WebAPI
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa final (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WebAPI.dll"]