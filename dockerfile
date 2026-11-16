# Estágio 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia o csproj e restaura dependências primeiro (otimiza cache de camadas)
COPY ["BankGuard-auth-API.csproj", "./"]
RUN dotnet restore "BankGuard-auth-API.csproj"

# Copia o restante do código e publica em Release
COPY . .
RUN dotnet publish "BankGuard-auth-API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio 2: Runtime leve
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Cria diretório persistente para o SQLite e ajusta permissões
RUN mkdir -p /app/data && chown -R app:app /app
USER app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "BankGuard-auth-API.dll"]
