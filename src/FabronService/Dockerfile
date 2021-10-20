FROM mcr.microsoft.com/dotnet/sdk:6.0.100-rc.2 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY .editorconfig .
COPY Directory.Build.props .
COPY global.json .
COPY nuget.config .
COPY src/FabronService/*.csproj ./src/FabronService/
COPY src/FabronService.FunctionalTests/*.csproj ./src/FabronService.FunctionalTests/
COPY src/Fabron.Core/*.csproj ./src/Fabron.Core/
COPY src/Fabron.Core.Test/*.csproj ./src/Fabron.Core.Test/
COPY src/Fabron.Mando/*.csproj ./src/Fabron.Mando/
COPY src/Fabron.Server/*.csproj ./src/Fabron.Server/
RUN dotnet restore

# copy everything else and build app
COPY . .
WORKDIR /source/src/FabronService
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0.0-rc.2
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "FabronService.dll"]
