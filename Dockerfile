# Рабочий Dockerfile для CarInsuranceBot

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем все файлы
COPY . .

# Восстанавливаем основной проект (API)
RUN dotnet restore "./CarInsuranceBot/CarInsuranceBot.Api.csproj"

# Собираем проект
RUN dotnet build "./CarInsuranceBot/CarInsuranceBot.Api.csproj" -c $BUILD_CONFIGURATION --no-restore

FROM build AS publisher
ARG BUILD_CONFIGURATION=Release
# Публикуем проект
RUN dotnet publish "./CarInsuranceBot/CarInsuranceBot.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publisher /app/publish .

# Запускаем приложение
ENTRYPOINT ["dotnet", "CarInsuranceBot.Api.dll"]