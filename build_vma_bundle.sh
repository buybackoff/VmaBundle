#!/bin/bash

# Check if required arguments are provided
if [ "$#" -lt 2 ] || [ "$#" -gt 3 ]; then
    echo "Usage: $0 <distro docker image> <output_folder> [extract and delete tar (0/1, default: 1)]"
    echo "Example: $0 debian:12 ./output/debian12"
    echo "Example: $0 ubuntu:25.10 ./output/ubuntu2510 0"
    exit 1
fi

DISTRO=$1
OUTPUT_FOLDER=$2
UNPACK_AND_DELETE=${3:-1} # Default to 1 (true) if not provided

# Sanitize distro name for image tag and container name
SANITIZED_DISTRO=$(echo "$DISTRO" | sed 's/[^a-zA-Z0-9_.-]/_/g')
IMAGE_TAG="vma-builder-${SANITIZED_DISTRO}"
CONTAINER_NAME="tmp-vma-builder-${SANITIZED_DISTRO}"
BUNDLE_FILENAME="vma-bundle.tar.gz"
OUTPUT_BUNDLE_PATH="${OUTPUT_FOLDER}/${BUNDLE_FILENAME}" # Path for bundle in output folder

echo "--- Building Docker image for distro: ${DISTRO} ---"
docker build --build-arg BASE_IMAGE="${DISTRO}" -t "${IMAGE_TAG}" .
if [ $? -ne 0 ]; then
    echo "Docker build failed."
    exit 1
fi

echo "--- Creating temporary container: ${CONTAINER_NAME} ---"
docker create --name "${CONTAINER_NAME}" "${IMAGE_TAG}"
if [ $? -ne 0 ]; then
    echo "Docker create failed."
    exit 1
fi

echo "--- Creating output folder: ${OUTPUT_FOLDER} ---"
mkdir -p "${OUTPUT_FOLDER}"
if [ $? -ne 0 ]; then
    echo "Failed to create output folder."
    docker rm "${CONTAINER_NAME}"
    exit 1
fi

echo "--- Copying bundle from container to output folder ---"
docker cp "${CONTAINER_NAME}:/export/${BUNDLE_FILENAME}" "${OUTPUT_BUNDLE_PATH}"
if [ $? -ne 0 ]; then
    echo "Docker cp failed."
    docker rm "${CONTAINER_NAME}"
    exit 1
fi

echo "--- Removing temporary container: ${CONTAINER_NAME} ---"
docker rm "${CONTAINER_NAME}"
if [ $? -ne 0 ]; then
    echo "Docker rm failed."
    # Continue as the bundle is already copied
fi

if [ "${UNPACK_AND_DELETE}" -eq 1 ]; then
    echo "--- Decompressing bundle into output folder ---"
    # Extract the tarball, ensuring the 'vma' directory is placed directly in OUTPUT_FOLDER
    tar -xzf "${OUTPUT_BUNDLE_PATH}" -C "${OUTPUT_FOLDER}"
    if [ $? -ne 0 ]; then
        echo "Failed to decompress bundle."
        rm -f "${OUTPUT_BUNDLE_PATH}"
        exit 1
    fi

    echo "--- Cleaning up bundle tar file ---"
    rm -f "${OUTPUT_BUNDLE_PATH}" # Delete bundle from output folder
    echo "--- Successfully built and extracted VMA bundle for ${DISTRO} to ${OUTPUT_FOLDER}/vma ---"
else
    echo "--- Keeping bundle tar file in output folder ---"
    echo "--- Successfully built VMA bundle for ${DISTRO} and saved to ${OUTPUT_BUNDLE_PATH} ---"
fi
