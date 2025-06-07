# Простой Dockerfile без оптимизации кэша

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем все файлы проекта
COPY . .

# Восстанавливаем и собираем основной проект
RUN dotnet restore "./CarInsuranceBot/CarInsuranceBot.csproj"
RUN dotnet build "./CarInsuranceBot/CarInsuranceBot.csproj" -c $BUILD_CONFIGURATION --no-restore

FROM build AS publisher
ARG BUILD_CONFIGURATION=Release
# Публикуем основной проект
RUN dotnet publish "./CarInsuranceBot/CarInsuranceBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publisher /app/publish .

# Запускаем основное приложение
ENTRYPOINT ["dotnet", "CarInsuranceBot.dll"]