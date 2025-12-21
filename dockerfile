# Stage 1: Runtime (Sử dụng Image ASP.NET 9.0 siêu nhẹ để chạy app)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
# .NET 8/9 mặc định dùng cổng 8080
EXPOSE 8080
EXPOSE 8081

# Stage 2: Build (Dùng SDK 9.0 để biên dịch)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy các file .csproj và restore (giúp cache layer tốt hơn)
COPY ["DrinkShop.WebApi/DrinkShop.WebApi.csproj", "DrinkShop.WebApi/"]
COPY ["DrinkShop.Application/DrinkShop.Application.csproj", "DrinkShop.Application/"]
COPY ["DrinkShop.Domain/DrinkShop.Domain.csproj", "DrinkShop.Domain/"]
COPY ["DrinkShop.Infrastructure/DrinkShop.Infrastructure.csproj", "DrinkShop.Infrastructure/"]

RUN dotnet restore "./DrinkShop.WebApi/DrinkShop.WebApi.csproj"

# Copy toàn bộ code còn lại
COPY . .
WORKDIR "/src/DrinkShop.WebApi"
RUN dotnet build "./DrinkShop.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DrinkShop.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DrinkShop.WebApi.dll"]