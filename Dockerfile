# Автоматический Dockerfile

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

# Восстанавливаем и собираем все проекты
RUN dotnet restore
RUN dotnet build -c $BUILD_CONFIGURATION

# Публикуем основной проект (CarInsuranceBot)
WORKDIR "/src/CarInsuranceBot"
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Автоматический поиск и запуск DLL
CMD ["sh", "-c", "dotnet $(find . -name '*.dll' -not -path './ref/*' | head -1)"]