FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DocumentService/DocumentService.csproj", "DocumentService/"]
COPY ["SharedKernel/SharedKernel.csproj", "SharedKernel/"]
RUN dotnet restore "DocumentService/DocumentService.csproj"
COPY . .
WORKDIR "/src/DocumentService"
RUN dotnet build "DocumentService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocumentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocumentService.dll"]
