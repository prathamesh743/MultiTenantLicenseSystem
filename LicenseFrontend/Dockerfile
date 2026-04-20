FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LicenseFrontend/LicenseFrontend.csproj", "LicenseFrontend/"]
COPY ["SharedKernel/SharedKernel.csproj", "SharedKernel/"]
RUN dotnet restore "LicenseFrontend/LicenseFrontend.csproj"
COPY . .
WORKDIR "/src/LicenseFrontend"
RUN dotnet build "LicenseFrontend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LicenseFrontend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LicenseFrontend.dll"]
