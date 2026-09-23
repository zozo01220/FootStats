FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
ENV Database__Path=/data/footstats.db
ENV Uploads__Path=/data/uploads
VOLUME /data

EXPOSE 8080
ENTRYPOINT ["dotnet", "FootStats.Web.dll"]
