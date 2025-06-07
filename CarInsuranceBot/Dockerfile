# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем файлы проекта с правильными путями
COPY ["CarInsuranceBot/CarInsuranceBot.csproj", "CarInsuranceBot/"]
COPY ["CarInsuranceBot.Core/CarInsuranceBot.Core.csproj", "CarInsuranceBot.Core/"]
COPY ["CarInsuranceBot.Infrastructure/CarInsuranceBot.Infrastructure.csproj", "CarInsuranceBot.Infrastructure/"]

# Восстанавливаем зависимости
RUN dotnet restore "CarInsuranceBot/CarInsuranceBot.csproj"

# Копируем все файлы проекта
COPY . .

# Собираем проект
WORKDIR "/src/CarInsuranceBot"
RUN dotnet build "CarInsuranceBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/CarInsuranceBot"
RUN dotnet publish "CarInsuranceBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CarInsuranceBot.dll"]