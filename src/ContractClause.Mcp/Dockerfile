FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY nuget.config ./
COPY ContractClause.sln ./
COPY src/ContractClause.Domain/ src/ContractClause.Domain/
COPY src/ContractClause.Application/ src/ContractClause.Application/
COPY src/ContractClause.Infrastructure/ src/ContractClause.Infrastructure/
COPY src/ContractClause.Mcp/ src/ContractClause.Mcp/
RUN dotnet restore src/ContractClause.Mcp/ContractClause.Mcp.csproj
RUN dotnet publish src/ContractClause.Mcp/ContractClause.Mcp.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV Mcp__Transport=http-sse
ENV Mcp__HttpPort=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContractClause.Mcp.dll"]
