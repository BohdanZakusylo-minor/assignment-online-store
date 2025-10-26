FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /app

# Copy the project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Install Entity Framework tools
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy the rest of the application code
COPY . .

# Expose the port your application will run on
EXPOSE 8081

# Use dotnet watch for hot reload in development
ENTRYPOINT ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:8081"]