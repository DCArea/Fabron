FROM mcr.microsoft.com/dotnet/sdk:6.0.202 AS build
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
COPY src/Fabron.FunctionalTests/*.csproj ./src/Fabron.FunctionalTests/
COPY src/Fabron.TestRunner/*.csproj ./src/Fabron.TestRunner/
COPY src/Fabron.Providers.PostgreSQL/*.csproj ./src/Fabron.Providers.PostgreSQL/
RUN dotnet restore

# copy everything else and build app
COPY . .
WORKDIR /source/src/FabronService
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0.2
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "FabronService.dll"]
