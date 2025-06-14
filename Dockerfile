FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
COPY . /app
COPY neofetch /usr/bin/neofetch
WORKDIR /app
RUN dotnet publish -c Release -o /app --self-contained

FROM alpine AS run
WORKDIR /app
COPY --from=build /app /app
