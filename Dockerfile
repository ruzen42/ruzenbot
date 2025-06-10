FROM mcr.microsoft.com/dotnet/sdk:9.0 
COPY . /docker
WORKDIR /docker
RUN dotnet publish -c Release -o out --self-contained
CMD ["./out/RuzenBot"]

