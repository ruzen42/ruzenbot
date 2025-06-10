FROM mcr.microsoft.com/dotnet/sdk:9.0 
COPY . /docker
COPY neofetch /bin/
WORKDIR /docker
RUN dotnet publish -c Release -o out 
CMD ["./out/RuzenBot"] 

