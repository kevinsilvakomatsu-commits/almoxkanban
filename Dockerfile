# Etapa 1: Build e Publicacao
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj e restaurar dependencias
COPY ["AlmoxKanban.csproj", "./"]
RUN dotnet restore "./AlmoxKanban.csproj"

# Copiar restante dos arquivos e publicar em modo Release
COPY . .
RUN dotnet publish "AlmoxKanban.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Ambiente de Execucao (Runtime enxuto)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Criar diretorios necessarios para o banco SQLite e uploads
RUN mkdir -p /app/Data /app/wwwroot/uploads /app/wwwroot/templates

# Copiar os arquivos compilados da etapa anterior
COPY --from=build /app/publish .

# Variaveis de ambiente
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AlmoxKanban.dll"]