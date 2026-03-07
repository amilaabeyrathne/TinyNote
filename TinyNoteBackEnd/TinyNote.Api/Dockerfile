FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["TinyNote.Api/TinyNote.Api.csproj", "TinyNote.Api/"]
RUN dotnet restore "TinyNote.Api/TinyNote.Api.csproj"
COPY . .
WORKDIR "/src/TinyNote.Api"
RUN dotnet build "TinyNote.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TinyNote.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TinyNote.Api.dll"]
