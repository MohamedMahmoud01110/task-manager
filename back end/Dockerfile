FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copy only the csproj files first so "restore" is cached when code changes
COPY src/TaskManager.Domain/*.csproj         src/TaskManager.Domain/
COPY src/TaskManager.Application/*.csproj    src/TaskManager.Application/
COPY src/TaskManager.Infrastructure/*.csproj src/TaskManager.Infrastructure/
COPY src/TaskManager.Api/*.csproj            src/TaskManager.Api/
RUN dotnet restore src/TaskManager.Api/TaskManager.Api.csproj
COPY src/ src/
RUN dotnet publish src/TaskManager.Api/TaskManager.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskManager.Api.dll"]