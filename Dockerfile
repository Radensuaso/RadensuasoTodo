# Use the official .NET SDK image to build and publish the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install EF Core CLI tools globally
RUN dotnet tool install -g dotnet-ef

# Add the dotnet tools to PATH
ENV PATH="$PATH:/root/.dotnet/tools"

# Copy the solution file and restore any dependencies
COPY RadensuasoTodo.sln .
COPY RadensuasoTodo.Api/*.csproj RadensuasoTodo.Api/
RUN dotnet restore

# Copy the entire project and build the app
COPY . .
WORKDIR /src/RadensuasoTodo.Api
RUN dotnet publish -c Release -o /app/publish

# Use the official .NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the published app from the build stage
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "RadensuasoTodo.Api.dll"]
