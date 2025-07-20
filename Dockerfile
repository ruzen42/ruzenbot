FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY . /app
RUN dotnet publish -c Release -o /app --self-contained

FROM ubuntu:latest AS final
WORKDIR /app
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
COPY --from=build /app/RuzenBot /bin
RUN apt-get update && \
    apt-get install -y ca-certificates && \
    rm -rf /var/log/apt/lists 
COPY fastfetch /usr/local/bin/fastfetch
CMD ["RuzenBot"]


