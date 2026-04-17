FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CloudsoftJob.sln .
COPY src/CloudsoftJob.Core/CloudsoftJob.Core.csproj src/CloudsoftJob.Core/
COPY src/CloudsoftJob.Web/CloudsoftJob.Web.csproj src/CloudsoftJob.Web/
COPY src/CloudsoftJob.Test/CloudsoftJob.Test.csproj src/CloudsoftJob.Test/
RUN dotnet restore CloudsoftJob.sln

COPY . .
RUN dotnet publish src/CloudsoftJob.Web/CloudsoftJob.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "CloudsoftJob.Web.dll"]
