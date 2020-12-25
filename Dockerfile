FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY VATSIM.Network.Dataserver/*.csproj ./VATSIM.Network.Dataserver/
COPY Models/*.csproj ./Models/
WORKDIR /app/VATSIM.Network.Dataserver
RUN dotnet restore

# copy and publish app and libraries
WORKDIR /app/
COPY VATSIM.Network.Dataserver/. ./VATSIM.Network.Dataserver/
WORKDIR /app/VATSIM.Network.Dataserver
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/VATSIM.Network.Dataserver/out ./
ENTRYPOINT ["dotnet", "VATSIM.Network.Dataserver.dll"]