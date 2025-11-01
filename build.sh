#!/bin/bash

# Exit immediately if a command exits with a non-zero status.
set -e

ARTIFACTS_DIR="./artifacts"
BUILD_ALL_RIDS_SCRIPT="./build_all_rids.sh"
NUSPEC_DIR="./NuGet"

# Define the base version and extract the libvma branch version
# For example, if LIBVMA_BRANCH_VERSION is "9.8.72", the patch version will be "9872"
LIBVMA_BRANCH_VERSION="9.8.72" # This can be set as an environment variable or passed as an argument if needed
NUGET_PATCH_VERSION=$(echo "${LIBVMA_BRANCH_VERSION}" | sed 's/\.//g')
FULL_NUGET_VERSION="0.1.${NUGET_PATCH_VERSION}"

echo "--- Using VMA Branch Version: ${LIBVMA_BRANCH_VERSION} ---"
echo "--- Calculated NuGet Version: ${FULL_NUGET_VERSION} ---"

# Check if build_all_rids.sh exists
if [ ! -f "${BUILD_ALL_RIDS_SCRIPT}" ]; then
    echo "Error: ${BUILD_ALL_RIDS_SCRIPT} not found. Please ensure it's in the same directory."
    exit 1
fi

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo "Error: 'dotnet' command not found. Please install .NET SDK."
    exit 1
fi

echo "--- Running ${BUILD_ALL_RIDS_SCRIPT} to build native bundles ---"
bash "${BUILD_ALL_RIDS_SCRIPT}" "${LIBVMA_BRANCH_VERSION}"
if [ $? -ne 0 ]; then
    echo "Error: ${BUILD_ALL_RIDS_SCRIPT} failed. Aborting NuGet package build."
    exit 1
fi

echo "--- Creating artifacts directory: ${ARTIFACTS_DIR} ---"
mkdir -p "${ARTIFACTS_DIR}"

echo "--- Building NuGet packages ---"
for NUSPEC_FILE in "${NUSPEC_DIR}"/*.nuspec; do
    if [ -f "${NUSPEC_FILE}" ]; then
        echo "Packing ${NUSPEC_FILE} with version ${FULL_NUGET_VERSION}..."
        dotnet nuget pack "${NUSPEC_FILE}" -o "${ARTIFACTS_DIR}" -p:Version="${FULL_NUGET_VERSION}"
        if [ $? -ne 0 ]; then
            echo "Error: dotnet nuget pack failed for ${NUSPEC_FILE}. Aborting."
            exit 1
        fi
    fi
done

echo ""
echo "--- All NuGet packages successfully built and placed in ${ARTIFACTS_DIR} ---"
