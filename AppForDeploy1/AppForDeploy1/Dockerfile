# build
ARG SDK_VERSION=10.0
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS build
WORKDIR /src
COPY *.csproj /src
RUN dotnet restore
# copy restored dependencies
COPY . /src
RUN dotnet publish --configuration Release --output /app/publish

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:${SDK_VERSION} AS runtime
WORKDIR /result

RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
        && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish /result
EXPOSE 8080
ENTRYPOINT [ "dotnet", "AppForDeploy1.dll" ]