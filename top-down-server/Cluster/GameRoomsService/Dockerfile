FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["GameRoomsService/GameRoomsService.csproj", "GameRoomsService/"]
RUN dotnet restore "GameRoomsService/GameRoomsService.csproj"
COPY . .
WORKDIR "/src/GameRoomsService"
RUN dotnet build "GameRoomsService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GameRoomsService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameRoomsService.dll"]
