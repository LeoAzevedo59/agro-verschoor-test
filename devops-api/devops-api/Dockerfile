FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY devops-api.csproj .
RUN dotnet restore "devops-api.csproj"
COPY . .
RUN dotnet publish "devops-api.csproj" -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /publish .

ENTRYPOINT ["dotnet", "devops-api.dll"]