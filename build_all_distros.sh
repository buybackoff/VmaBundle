#!/bin/bash

# Run this on Podman WSL instance from the repo root folder from Windows (mnt/c/...).

# Exit immediately if a command exits with a non-zero status.
set -e

# Check if required arguments are provided
if [ "$#" -lt 1 ] || [ "$#" -gt 2 ]; then
    echo "Usage: $0 <libvma_branch_version> [extract and delete tar (0/1, default: 1)]"
    echo "Example: $0 9.8.80"
    echo "Example: $0 9.8.80 0"
    exit 1
fi

LIBVMA_BRANCH_VERSION=$1
UNPACK_AND_DELETE=${2:-1} # Default to 1 (true) if not provided

echo "--- Building VMA bundles for all distros (VMA Branch: ${LIBVMA_BRANCH_VERSION}) ---"

./build_vma_bundle.sh debian:13 ./output/debian13 "${LIBVMA_BRANCH_VERSION}" "${UNPACK_AND_DELETE}"
./build_vma_bundle.sh debian:12 ./output/debian12 "${LIBVMA_BRANCH_VERSION}" "${UNPACK_AND_DELETE}"
./build_vma_bundle.sh ubuntu:24.04 ./output/ubuntu2404 "${LIBVMA_BRANCH_VERSION}" "${UNPACK_AND_DELETE}"
./build_vma_bundle.sh ubuntu:25.10 ./output/ubuntu2510 "${LIBVMA_BRANCH_VERSION}" "${UNPACK_AND_DELETE}"

echo "--- Successfully built VMA bundles for all distros ---"