# See here for image contents: https://github.com/devcontainers/images/tree/main/src/dotnet
FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0

# The base image currently includes a Yarn APT source (dl.yarnpkg.com) but does not
# include the corresponding signing key, which causes apt operations to fail with:
#   NO_PUBKEY 62D54FD4003F6525
# We don't need the Yarn APT repo for this repo (Node feature provides Corepack),
# so remove it to keep apt stable for both feature installs and post-create steps.
RUN set -e; \
    rm -f /etc/apt/sources.list.d/yarn.list || true; \
    rm -f /etc/apt/sources.list.d/yarn.sources || true; \
    apt-get clean && rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

# Remove pre-release / RC SDKs shipped by the base image.
# global.json pins the stable SDK, but the RC wastes ~200 MB of disk.
RUN set -e; \
    for sdk in /usr/share/dotnet/sdk/*-rc* /usr/share/dotnet/sdk/*-preview*; do \
        [ -d "$sdk" ] && echo "Removing RC/preview SDK: $sdk" && rm -rf "$sdk"; \
    done; \
    true

# The base image's common-utils feature sets NVM_DIR, NVM_SYMLINK_CURRENT, etc.
# Node.js / NVM are not used in this repository — clear the variables.
ENV NVM_DIR="" \
    NVM_SYMLINK_CURRENT="" \
    NVM_CD_FLAGS="" \
    NVM_RC_VERSION=""
