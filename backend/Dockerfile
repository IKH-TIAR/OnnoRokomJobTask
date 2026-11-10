# ---------- build: restore + publish the API ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached unless the project file changes)
COPY SchoolAdmission.csproj ./
RUN dotnet restore SchoolAdmission.csproj

COPY . .
RUN dotnet publish SchoolAdmission.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- test: run the integration tests (docker compose run --rm backend-tests) ----------
# Needs TEST_DB_CONNECTION pointing at a PostgreSQL server; the tests create and drop
# their own temporary database there.
FROM build AS test
WORKDIR /src
RUN dotnet restore tests/SchoolAdmission.Tests/SchoolAdmission.Tests.csproj \
 && dotnet build tests/SchoolAdmission.Tests/SchoolAdmission.Tests.csproj -c Release --no-restore
ENTRYPOINT ["dotnet", "test", "tests/SchoolAdmission.Tests/SchoolAdmission.Tests.csproj", "-c", "Release", "--no-build", "--logger", "console;verbosity=normal"]

# ---------- final: small runtime-only image ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Listen on 8080 inside the container (also the aspnet image default)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Run as the non-root user that the official image provides
USER $APP_UID

ENTRYPOINT ["dotnet", "SchoolAdmission.dll"]
