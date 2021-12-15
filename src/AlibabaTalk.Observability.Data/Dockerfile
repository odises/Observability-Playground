FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine as build

ENV GLIBC_REPO=https://github.com/sgerrand/alpine-pkg-glibc
ENV GLIBC_VERSION=2.28-r0

RUN set -ex && \
    apk --update add libstdc++ curl ca-certificates && \
    for pkg in glibc-${GLIBC_VERSION} glibc-bin-${GLIBC_VERSION}; \
    do curl -sSL ${GLIBC_REPO}/releases/download/${GLIBC_VERSION}/${pkg}.apk -o /tmp/${pkg}.apk; done && \
    apk add --allow-untrusted /tmp/*.apk && \
    rm -v /tmp/*.apk && \
    /usr/glibc-compat/sbin/ldconfig /lib /usr/glibc-compat/lib


WORKDIR /app
COPY . .

RUN dotnet dev-certs https
RUN dotnet dev-certs https --trust
ENTRYPOINT [ "dotnet", "run",  "AlibabaTalk-Observability.Data.dll"]
