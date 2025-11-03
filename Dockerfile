ARG BASE_IMAGE=debian:13

FROM ${BASE_IMAGE} AS build

RUN apt-get update -qq > /dev/null && \
    apt-get install -y -qq \
        build-essential autoconf automake libtool pkg-config git \
        rdma-core libibverbs-dev librdmacm-dev libnl-3-dev libnl-route-3-dev \
        > /dev/null && \
    apt-get clean -qq > /dev/null && \
    rm -rf /var/lib/apt/lists/* > /dev/null

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

RUN set -e; mkdir -p /opt/vma/lib && \
    for libname in libibverbs librdmacm libmlx5 libmlx4 libnl-3 libnl-route-3; do \
      for search_path in /usr/lib /lib; do \
        for src_file in "$search_path/x86_64-linux-gnu/${libname}.so."*; do \
          [ -e "$src_file" ] || continue; \
          case "$src_file" in \
            *.so.[0-9]*.*) continue ;; \
            *.so.[0-9]*) ;; \
            *) continue ;; \
          esac; \
          dst_file="/opt/vma/lib/$(basename "$src_file")"; \
          [ -e "$dst_file" ] && continue; \
          cp -L "$src_file" "$dst_file"; \
        done; \
      done; \
    done && \
    cp -L /usr/lib/x86_64-linux-gnu/libvma.so /opt/vma/lib/libvma.so && \
    tar czf /opt/vma-bundle.tar.gz -C /opt vma

FROM scratch AS export
COPY --from=build /opt/vma-bundle.tar.gz /export/vma-bundle.tar.gz
