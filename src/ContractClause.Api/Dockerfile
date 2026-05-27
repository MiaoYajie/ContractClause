FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY nuget.config ./
COPY ContractClause.sln ./
COPY src/ContractClause.Domain/ src/ContractClause.Domain/
COPY src/ContractClause.Application/ src/ContractClause.Application/
COPY src/ContractClause.Infrastructure/ src/ContractClause.Infrastructure/
COPY src/ContractClause.Api/ src/ContractClause.Api/
RUN dotnet restore src/ContractClause.Api/ContractClause.Api.csproj
RUN dotnet publish src/ContractClause.Api/ContractClause.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContractClause.Api.dll"]
