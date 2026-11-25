FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Só os .csproj primeiro: o restore dos pacotes fica em cache enquanto os projetos não mudam.
# (Ao criar um projeto novo, acrescente a linha COPY dele aqui.)
COPY ["src/Api/LojaApi.csproj", "src/Api/"]
COPY ["src/Domain/LojaApi.Domain.csproj", "src/Domain/"]
COPY ["src/Data/LojaApi.Data.csproj", "src/Data/"]
COPY ["src/Application/LojaApi.Application.csproj", "src/Application/"]
RUN dotnet restore src/Api/LojaApi.csproj
COPY . .
RUN dotnet build src/Api/LojaApi.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish src/Api/LojaApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LojaApi.dll"]
