FROM mcr.microsoft.com/dotnet/sdk:6.0.300 AS build
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
COPY src/Fabron.Server/*.csproj ./src/Fabron.Server/
COPY src/Fabron.FunctionalTests/*.csproj ./src/Fabron.FunctionalTests/
COPY src/Fabron.TestRunner/*.csproj ./src/Fabron.TestRunner/
COPY src/Fabron.Providers.PostgreSQL/*.csproj ./src/Fabron.Providers.PostgreSQL/
COPY perf/Benchmarks/Benchmarks.csproj ./perf/Benchmarks/
RUN dotnet restore

# copy everything else and build app
COPY . .
WORKDIR /source/src/FabronService
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0.3
WORKDIR /app
ENV Logging__Console__FormatterName="ejson"
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "FabronService.dll"]
