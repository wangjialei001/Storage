#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["WeiCloudStorageAPI/WeiCloudStorageAPI.csproj", "WeiCloudStorageAPI/"]
COPY ["Msg.Core/Msg.Core.csproj", "Msg.Core/"]
COPY ["VideoPackage/HaikangSDK/HaikangSDK.csproj", "VideoPackage/HaikangSDK/"]
RUN dotnet restore "WeiCloudStorageAPI/WeiCloudStorageAPI.csproj"
COPY . .
WORKDIR "/src/WeiCloudStorageAPI"
RUN dotnet build "WeiCloudStorageAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WeiCloudStorageAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeiCloudStorageAPI.dll"]