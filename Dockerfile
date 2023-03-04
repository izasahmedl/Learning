#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get update && \
    apt-get install build-essential curl file git ruby-full locales --no-install-recommends -y && \
    rm -rf /var/lib/apt/lists/*

RUN localedef -i en_US -f UTF-8 en_US.UTF-8

RUN useradd -m -s /bin/bash linuxbrew && \
    echo 'linuxbrew ALL=(ALL) NOPASSWD:ALL' >>/etc/sudoers

USER linuxbrew
RUN sh -c "$(curl -fsSL https://raw.githubusercontent.com/Linuxbrew/install/master/install.sh)"

USER root
ENV PATH="/home/linuxbrew/.linuxbrew/bin:${PATH}"

RUN  apt-get update && apt-get install -y wget && rm -rf /var/lib/apt/lists/*

RUN apt-get update -yq 
RUN apt-get install -y zip

RUN wget https://github.com/Azure/kubelogin/releases/download/v0.0.9/kubelogin-linux-amd64.zip
RUN unzip kubelogin-linux-amd64.zip
RUN mv bin/linux_amd64/kubelogin /usr/bin

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Learning.API/Learning.API.csproj", "Learning.API/"]
COPY ["Learning.Common/Learning.Common.csproj", "Learning.Common/"]
RUN dotnet restore "Learning.API/Learning.API.csproj"
COPY . .
WORKDIR "/src/Learning.API"
RUN dotnet build "Learning.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Learning.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Learning.API.dll"]