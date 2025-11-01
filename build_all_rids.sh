#!/bin/bash

# Exit immediately if a command exits with a non-zero status.
set -e

# Check if LIBVMA_BRANCH_VERSION is provided
if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <libvma_branch_version>"
    echo "Example: $0 9.8.72"
    exit 1
fi

LIBVMA_BRANCH_VERSION=$1

# Define RIDs and their corresponding Docker base images
declare -A RID_MAP
RID_MAP["ubuntu.25-x64"]="ubuntu:25.10"
RID_MAP["debian.13-x64"]="debian:13"
RID_MAP["debian.12-x64"]="debian:12"
RID_MAP["linux-x64"]="debian:11" # As per VmaBundle.Native.nuspec description

# Path to the build script
BUILD_SCRIPT="./build_vma_bundle.sh"

# Check if the build script exists
if [ ! -f "${BUILD_SCRIPT}" ]; then
    echo "Error: ${BUILD_SCRIPT} not found. Please ensure it's in the same directory."
    exit 1
fi

echo "--- Starting VMA bundle builds for all RIDs (VMA Branch: ${LIBVMA_BRANCH_VERSION}) ---"

for RID in "${!RID_MAP[@]}"; do
    DISTRO="${RID_MAP[$RID]}"
    OUTPUT_DIR="./NuGet/runtimes/${RID}/native"

    echo ""
    echo "===================================================================="
    echo "--- Building for RID: ${RID} (Distro: ${DISTRO}) ---"
    echo "--- Output will be placed in: ${OUTPUT_DIR} ---"
    echo "===================================================================="
    echo ""

    # Call the build_vma_bundle.sh script
    # The fourth argument '1' ensures the tar is unpacked and deleted
    bash "${BUILD_SCRIPT}" "${DISTRO}" "${OUTPUT_DIR}" "${LIBVMA_BRANCH_VERSION}" 1
    if [ $? -ne 0 ]; then
        echo "Error: Build for ${RID} failed. Aborting."
        exit 1
    fi

    DUMMY_FILE_PATH="${OUTPUT_DIR}/vma/${RID}"
    # Create dummy file for debugging, ignore if exists or fails
    touch "${DUMMY_FILE_PATH}" || true
done

echo ""
echo "--- All VMA bundles successfully built and extracted. ---"
