FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN apk add --no-cache icu-libs openssh-keygen
ENV LC_ALL=en_US.UTF-8
ENV LANG=en_US.UTF-8
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=80
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ["BTCPayServerDockerConfigurator/BTCPayServerDockerConfigurator.csproj", "BTCPayServerDockerConfigurator/"]
RUN dotnet restore "BTCPayServerDockerConfigurator/BTCPayServerDockerConfigurator.csproj"
COPY . .
WORKDIR "/src/BTCPayServerDockerConfigurator"
RUN dotnet build "BTCPayServerDockerConfigurator.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "BTCPayServerDockerConfigurator.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
COPY entrypoint.sh docker-entrypoint.sh
RUN chmod +x docker-entrypoint.sh
ENTRYPOINT ["./docker-entrypoint.sh"]
