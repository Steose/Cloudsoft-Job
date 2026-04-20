FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CloudsoftJob.sln .
COPY src/CloudsoftJob.Domain/CloudsoftJob.Domain.csproj src/CloudsoftJob.Domain/
COPY src/CloudsoftJob.Application/CloudsoftJob.Application.csproj src/CloudsoftJob.Application/
COPY src/CloudsoftJob.Infrastructure/CloudsoftJob.Infrastructure.csproj src/CloudsoftJob.Infrastructure/
COPY src/CloudsoftJob.Web/CloudsoftJob.Web.csproj src/CloudsoftJob.Web/
COPY tests/CloudsoftJob.Test/CloudsoftJob.Test.csproj tests/CloudsoftJob.Test/
RUN dotnet restore src/CloudsoftJob.Web/CloudsoftJob.Web.csproj

COPY . .
RUN dotnet restore src/CloudsoftJob.Web/CloudsoftJob.Web.csproj
RUN dotnet publish src/CloudsoftJob.Web/CloudsoftJob.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "CloudsoftJob.Web.dll"]
