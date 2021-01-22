#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Source/Service/Glasswall.EBS.Rebuild/Glasswall.EBS.Rebuild.csproj", "Source/Service/Glasswall.EBS.Rebuild/"]
RUN dotnet restore "Source/Service/Glasswall.EBS.Rebuild/Glasswall.EBS.Rebuild.csproj"
COPY . .
WORKDIR "/src/Source/Service/Glasswall.EBS.Rebuild"
RUN dotnet build "Glasswall.EBS.Rebuild.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Glasswall.EBS.Rebuild.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Glasswall.EBS.Rebuild.dll"]