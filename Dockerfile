# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file and project file(s)
COPY RadensuasoTodo.Api/RadensuasoTodo.Api.csproj RadensuasoTodo.Api/
RUN dotnet restore

# Copy the entire project and the .env file
COPY . .
WORKDIR /src/RadensuasoTodo.Api
RUN dotnet publish -c Release -o /app/publish

# Use the official .NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/.env .env
EXPOSE 80
ENTRYPOINT ["dotnet", "RadensuasoTodo.Api.dll"]
