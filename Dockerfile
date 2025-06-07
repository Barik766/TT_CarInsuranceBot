# Dockerfile для использования с solution файлом

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем solution файл и все csproj файлы
COPY *.sln ./
COPY CarInsuranceBot/*.csproj ./CarInsuranceBot/
COPY CarInsuranceBot.Core/*.csproj ./CarInsuranceBot.Core/
COPY CarInsuranceBot.Infrastructure/*.csproj ./CarInsuranceBot.Infrastructure/
COPY CarInsuranceBot.Application/*.csproj ./CarInsuranceBot.Application/

# Восстанавливаем зависимости
RUN dotnet restore

# Копируем остальные файлы
COPY . .

# Собираем решение
RUN dotnet build -c $BUILD_CONFIGURATION --no-restore

FROM build AS publisher
ARG BUILD_CONFIGURATION=Release
# Публикуем основной проект
RUN dotnet publish "CarInsuranceBot/CarInsuranceBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publisher /app/publish .
ENTRYPOINT ["dotnet", "CarInsuranceBot.dll"]