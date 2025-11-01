ARG BASE_IMAGE=debian:13

FROM ${BASE_IMAGE} AS build

RUN apt update && apt install -y \
    build-essential autoconf automake libtool pkg-config git \
    rdma-core libibverbs-dev librdmacm-dev libnl-3-dev libnl-route-3-dev && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src

# clone layer — cached unless ARG changes
ARG LIBVMA_BRANCH_VERSION=9.8.72 # Default branch version for libvma
RUN git clone --depth=1 --branch ${LIBVMA_BRANCH_VERSION} https://github.com/Mellanox/libvma.git

# build and install — runs only if clone layer changes
RUN cd libvma && \
    cp LICENSE /src/libvma.LICENSE && \
    ./autogen.sh && \
    ./configure --prefix=/usr --libdir=/usr/lib/x86_64-linux-gnu && \
    make -j"$(nproc)" && \
    make install && \
    cd /src && \
    rm -rf libvma

# collect license texts for all bundled libraries
RUN mkdir -p /opt/vma/licenses && \
    for P in libibverbs1 librdmacm1 libnl-3-200 libnl-route-3-200; do \
        if [ -f "/usr/share/doc/$P/copyright" ]; then \
            cp -a "/usr/share/doc/$P/copyright" "/opt/vma/licenses/${P}.LICENSE"; \
        else \
            echo "Warning: Copyright file for $P not found." >&2; \
        fi; \
    done && \
    # libmlx5-1/libmlx4-1 license is inside rdma-core
    if [ -f "/usr/share/doc/rdma-core/copyright" ]; then \
        cp -a "/usr/share/doc/rdma-core/copyright" "/opt/vma/licenses/libmlx5-1.LICENSE"; \
        cp -a "/usr/share/doc/rdma-core/copyright" "/opt/vma/licenses/libmlx4-1.LICENSE"; \
    else \
        echo "Warning: Copyright file for rdma-core not found." >&2; \
    fi && \
    # add libvma license from source
    cp -a /src/libvma.LICENSE /opt/vma/licenses/libvma.LICENSE && \
    rm /src/libvma.LICENSE

# collect only single real .so files (no symlinks)
RUN mkdir -p /opt/vma/lib && \
    for L in libvma libibverbs librdmacm libmlx5 libmlx4 libnl-3 libnl-route-3; do \
        SRC=$(ls -1 /usr/lib/x86_64-linux-gnu/${L}.so* 2>/dev/null | head -n1); \
        [ -n "$SRC" ] && cp -L "$SRC" "/opt/vma/lib/${L}.so"; \
    done && \
    tar czf /opt/vma-bundle.tar.gz -C /opt vma

FROM scratch AS export
COPY --from=build /opt/vma-bundle.tar.gz /export/vma-bundle.tar.gz

# For LD_PRELOAD prefer system libraries if present
# export LD_LIBRARY_PATH=${LD_LIBRARY_PATH:+$LD_LIBRARY_PATH:}/opt/vma/lib

# or prefer extracted ones
# export LD_LIBRARY_PATH=/opt/vma/lib:$LD_LIBRARY_PATH

# To run, extract & clean (example for Debian 13)
# docker build -t vma-builder-debian13 .
# docker create --name tmp-debian13 vma-builder-debian13
# docker cp tmp-debian13:/export/vma-bundle.tar.gz .
# docker rm tmp-debian13

# To run, extract & clean (example for Ubuntu 25.10)
# docker build --build-arg BASE_IMAGE=ubuntu:25.10 -t vma-builder-ubuntu2510 .
# docker create --name tmp-ubuntu2510 vma-builder-ubuntu2510
# docker cp tmp-ubuntu2510:/export/vma-bundle.tar.gz .
# docker rm tmp-ubuntu2510

# Cleanup images
# podman image prune -a -f
