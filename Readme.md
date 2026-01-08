# VmaBundle

For VMA to work, only kernel mods are required. Normally they are loaded automatically when Mellanox cards are present.

The rest is userspace libraries.

The goal of this project is to extract all required userspace libraries using a Dickerfile build, and then pack them into NuGet format.

## Dockerfile to get packaged libraries

Dockerfile gets actual libraries from apt, builds libvma and extracts all required libraries to a tar archive.

To run, extract & clean

* Debian 13

```sh
docker build -t vma-builder-debian13 .
docker create --name tmp-debian13 vma-builder-debian13
docker cp tmp-debian13:/export/vma-bundle.tar.gz .
docker rm tmp-debian13
```

* *Ubuntu 25.10

```sh
docker build --build-arg BASE_IMAGE=ubuntu:25.10 -t vma-builder-ubuntu2510 .
docker create --name tmp-ubuntu2510 vma-builder-ubuntu2510
docker cp tmp-ubuntu2510:/export/vma-bundle.tar.gz .
docker rm tmp-ubuntu2510
```

Cleanup images & layers:

```
podman image prune -a -f
```

## Build scripts

* build_vma_bundle.sh - builds the bundle for a given distro and libvma version

```sh
# This works fine from WSL, directly from Podman Desktop WSL distro
# If it's the default WSL distro, just run wsl in Explorer's address bar, or go to mnt/c/path/to/this/repo
./build_vma_bundle.sh debian:13 ./output/debian13 9.8.80
```

TODO Need a separate script build_nuget.sh/build_nuget.cmd
On Windows, it's nuget pack NuGet\VmaBundle.template.nuspec -OutputDirectory output -Version 0.4.9880 -Properties distro=debian13

* build_all_distros.sh - builds the bundle for all supported distros via `build_vma_bundle.sh`.
* build_nugets.cmd - builds NuGets packages.

## Run with LD_PRELOAD

For LD_PRELOAD prefer system libraries if present
```sh
export LD_LIBRARY_PATH=${LD_LIBRARY_PATH:+$LD_LIBRARY_PATH:}/opt/vma/lib
```

or prefer extracted ones

```sh
export LD_LIBRARY_PATH=/opt/vma/lib:$LD_LIBRARY_PATH
```

## Proxy

To clone libvma, a TLS certificate for a proxy may be required. Place it in the root of this repo as `proxy.pem` or `proxy.crt`.