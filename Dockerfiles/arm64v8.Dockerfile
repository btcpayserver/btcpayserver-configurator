FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
RUN apt-get update \
	&& apt-get install -qq --no-install-recommends qemu qemu-user-static qemu-user binfmt-support
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY BTCPayServerDockerConfigurator/*.csproj ./BTCPayServerDockerConfigurator/
RUN dotnet restore

# copy everything else and build app
COPY BTCPayServerDockerConfigurator/. ./BTCPayServerDockerConfigurator/
WORKDIR /app

RUN dotnet publish -c Release -o out


FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.0-buster-slim-arm64v8  AS runtime
COPY --from=builder /usr/bin/qemu-aarch64-static /usr/bin/qemu-aarch64-static
RUN apt-get update && apt-get install -y --no-install-recommends iproute2 openssh-client \
    && rm -rf /var/lib/apt/lists/* 
WORKDIR /app
COPY --from=build /app/out ./
ENV LC_ALL en_US.UTF-8
ENV LANG en_US.UTF-8
	
	
EXPOSE 80
EXPOSE 443
COPY Dockerfiles/entrypoint.sh docker-entrypoint.sh
RUN chmod +x docker-entrypoint.sh
ENTRYPOINT ["./docker-entrypoint.sh"]