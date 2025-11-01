#!/bin/bash

# Exit immediately if a command exits with a non-zero status.
set -e

ARTIFACTS_DIR="./artifacts"
BUILD_ALL_RIDS_SCRIPT="./build_all_rids.sh"
NUSPEC_DIR="./NuGet"

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
bash "${BUILD_ALL_RIDS_SCRIPT}"
if [ $? -ne 0 ]; then
    echo "Error: ${BUILD_ALL_RIDS_SCRIPT} failed. Aborting NuGet package build."
    exit 1
fi

echo "--- Creating artifacts directory: ${ARTIFACTS_DIR} ---"
mkdir -p "${ARTIFACTS_DIR}"

echo "--- Building NuGet packages ---"
for NUSPEC_FILE in "${NUSPEC_DIR}"/*.nuspec; do
    if [ -f "${NUSPEC_FILE}" ]; then
        echo "Packing ${NUSPEC_FILE}..."
        dotnet nuget pack "${NUSPEC_FILE}" -o "${ARTIFACTS_DIR}" -p:Version=0.1.0 # Using a fixed version for consistency
        if [ $? -ne 0 ]; then
            echo "Error: dotnet nuget pack failed for ${NUSPEC_FILE}. Aborting."
            exit 1
        fi
    fi
done

echo ""
echo "--- All NuGet packages successfully built and placed in ${ARTIFACTS_DIR} ---"
