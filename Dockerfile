FROM debian:13 AS build

RUN apt update && apt install -y \
    build-essential autoconf automake libtool pkg-config git \
    rdma-core libibverbs-dev librdmacm-dev libnl-3-dev libnl-route-3-dev

WORKDIR /src
RUN git clone --branch 9.8.72 --depth=1 https://github.com/Mellanox/libvma.git

WORKDIR /src/libvma
RUN ./autogen.sh && \
    ./configure --prefix=/usr --libdir=/usr/lib/x86_64-linux-gnu && \
    make -j"$(nproc)" && \
    make install

# collect license texts for all bundled libraries
RUN mkdir -p /opt/vma/licenses && \
    for P in libibverbs1 librdmacm1 libnl-3-200 libnl-route-3-200; do \
        cp -a /usr/share/doc/$P/copyright /opt/vma/licenses/${P}.LICENSE || true; \
    done && \
    # libmlx5-1/libmlx4-1 license is inside rdma-core
    cp -a /usr/share/doc/rdma-core/copyright /opt/vma/licenses/libmlx5-1.LICENSE || true && \
    cp -a /usr/share/doc/rdma-core/copyright /opt/vma/licenses/libmlx4-1.LICENSE || true && \
    # add libvma license from source
    cp -a /src/libvma/LICENSE /opt/vma/licenses/libvma.LICENSE

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

# To run, extract & clean
# docker build -t vma-builder .
# docker create --name tmp vma-builder
# docker cp tmp:/export/vma-bundle.tar.gz . 
# docker rm tmp
