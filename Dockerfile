FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

RUN apt-get update && \
    apt-get install -y --no-install-recommends gcc zlib1g-dev && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY . .

RUN dotnet publish -c Release -o /app/publish \
    --self-contained true \
    -p:PublishAot=true \
    -p:StripSymbols=true

FROM ubuntu:24.04
RUN apt-get update && \
    apt-get install -y --no-install-recommends ca-certificates libssl3 && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["./RuzenBot"]


