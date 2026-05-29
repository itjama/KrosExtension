FROM node:24-alpine AS client-build
WORKDIR /src/ClientApp
COPY ClientApp/package*.json ./
RUN npm ci
COPY ClientApp/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS server-build
WORKDIR /src
COPY ../../Directory.Build.props ./
COPY KrosExtension.Web.csproj ./KrosExtension.Web/
RUN dotnet restore ./KrosExtension.Web/KrosExtension.Web.csproj
COPY . ./KrosExtension.Web/
COPY --from=client-build /src/ClientApp/dist ./KrosExtension.Web/ClientApp/dist
RUN dotnet publish ./KrosExtension.Web/KrosExtension.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=server-build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "KrosExtension.Web.dll"]
