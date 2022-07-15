FROM mcr.microsoft.com/dotnet/sdk:6.0.301 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY .editorconfig .
COPY Directory.Build.props .
COPY global.json .
COPY nuget.config .
COPY perf/**/*.csproj .
COPY src/**/*.csproj .
COPY src/Directory.Build.props ./src/
RUN dotnet sln list | grep ".csproj" \
    | while read -r line; do \
    mkdir -p $(dirname $line); \
    mv $(basename $line) $(dirname $line); \
    done;
RUN dotnet restore

COPY . .
WORKDIR /source/src/FabronService
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0.3
WORKDIR /app
ENV Logging__Console__FormatterName="ejson"
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "FabronService.dll"]
