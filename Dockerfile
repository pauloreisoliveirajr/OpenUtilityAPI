FROM node:24-alpine AS frontend
WORKDIR /source/frontend/playground
COPY frontend/playground/ ./
RUN npm install --global pnpm@11.25.0 && pnpm install --frozen-lockfile && pnpm build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
COPY --from=frontend /source/src/OpenUtilityAPI.Api/wwwroot ./src/OpenUtilityAPI.Api/wwwroot
RUN dotnet publish src/OpenUtilityAPI.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "OpenUtilityAPI.Api.dll"]
