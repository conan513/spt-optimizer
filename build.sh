#!/usr/bin/env bash
set -euo pipefail

# ANSI color codes
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
RED='\033[0;31m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CSPROJ_FILE="${PROJECT_DIR}/SPTOptimizer.csproj"
DIST_DIR="${PROJECT_DIR}/dist"
DEFAULT_SPT_PATH="${SPT_INSTALL_PATH:-/home/conan/Games/Heroic/Prefixes/Escape From Tarkov/drive_c/Games/SPP-Tarkov}"

CONFIGURATION="Release"
CLEAN=false
NO_DEPLOY=false
SPT_PATH="${DEFAULT_SPT_PATH}"

usage() {
    cat <<EOF
SPT Optimizer Build Script (Linux)

Usage:
  ./build.sh [options]

Options:
  -c, --configuration <Config>  Build configuration: 'Release' (default) or 'Debug'
  --clean                       Performs a clean build (removes bin and obj folders)
  --no-deploy                   Skips copying to the SPT game plugins directory
  --spt-path <Path>             Target SPT installation path (default: '${DEFAULT_SPT_PATH}')
  -h, --help                    Show this help message
EOF
}

# Parse command-line arguments
while [[ $# -gt 0 ]]; do
    case "$1" in
        -c|--configuration)
            if [[ "$2" =~ ^(Release|Debug)$ ]]; then
                CONFIGURATION="$2"
                shift 2
            else
                echo -e "${RED}[ERROR] Invalid configuration '$2'. Allowed values: Release, Debug.${NC}" >&2
                exit 1
            fi
            ;;
        --clean)
            CLEAN=true
            shift
            ;;
        --no-deploy)
            NO_DEPLOY=true
            shift
            ;;
        --spt-path)
            SPT_PATH="$2"
            shift 2
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo -e "${RED}[ERROR] Unknown option: $1${NC}" >&2
            usage
            exit 1
            ;;
    esac
done

echo -e "${CYAN}==========================================${NC}"
echo -e "${CYAN}       SPT Optimizer Build Script         ${NC}"
echo -e "${CYAN}==========================================${NC}"
echo -e "${GRAY}Configuration : ${CONFIGURATION}${NC}"
echo -e "${GRAY}Project       : ${CSPROJ_FILE}${NC}"

# 1. Clean if requested
if [ "$CLEAN" = true ]; then
    echo -e "\n${YELLOW}[1/4] Cleaning project artifacts...${NC}"
    dotnet clean "${CSPROJ_FILE}" -c "${CONFIGURATION}" --verbosity quiet
    rm -rf "${PROJECT_DIR}/bin" "${PROJECT_DIR}/obj" "${DIST_DIR}/staging"
    echo -e "${GREEN}Clean completed.${NC}"
else
    echo -e "\n${GRAY}[1/4] Skipping clean (use --clean to force).${NC}"
fi

# 2. Build
echo -e "\n${YELLOW}[2/4] Building SPT Optimizer (${CONFIGURATION})...${NC}"
BUILD_FLAGS=("-c" "${CONFIGURATION}")
if [ "$CLEAN" = true ]; then
    BUILD_FLAGS+=("--no-incremental")
fi

dotnet build "${CSPROJ_FILE}" "${BUILD_FLAGS[@]}"

DLL_PATH="${PROJECT_DIR}/bin/${CONFIGURATION}/netstandard2.1/SPTOptimizer.dll"
if [ ! -f "${DLL_PATH}" ]; then
    echo -e "${RED}[ERROR] Compiled DLL not found at: ${DLL_PATH}${NC}" >&2
    exit 1
fi

# Extract version from csproj or fallback
VERSION=$(grep -oP '(?<=<Version>)[^<]+' "${CSPROJ_FILE}" 2>/dev/null || echo "1.0.0")
echo -e "${GRAY}Compiled binary: SPTOptimizer.dll (v${VERSION})${NC}"

# 3. Direct Deploy to SPT folder
if [ "$NO_DEPLOY" = false ]; then
    echo -e "\n${YELLOW}[3/4] Deploying to SPT game folder...${NC}"
    PLUGIN_TARGET_DIR="${SPT_PATH}/BepInEx/plugins"

    if [ -d "${SPT_PATH}/BepInEx" ] || [ -d "${SPT_PATH}/BepInEx/plugins" ]; then
        mkdir -p "${PLUGIN_TARGET_DIR}"
        cp -f "${DLL_PATH}" "${PLUGIN_TARGET_DIR}/SPTOptimizer.dll"
        echo -e "${GREEN}Successfully deployed to: ${PLUGIN_TARGET_DIR}/SPTOptimizer.dll${NC}"
    else
        echo -e "${YELLOW}[WARN] SPT install path not found at '${SPT_PATH}'. Skipped copy.${NC}"
    fi
else
    echo -e "\n${GRAY}[3/4] Skipping deployment (--no-deploy specified).${NC}"
fi

# 4. Packaging / Distribution Zip
echo -e "\n${YELLOW}[4/4] Creating distribution package in /dist...${NC}"
mkdir -p "${DIST_DIR}"

STAGING_ROOT="${DIST_DIR}/staging"
PACKAGE_STAGING="${STAGING_ROOT}/BepInEx/plugins"

rm -rf "${STAGING_ROOT}"
mkdir -p "${PACKAGE_STAGING}"

cp -f "${DLL_PATH}" "${PACKAGE_STAGING}/SPTOptimizer.dll"

ZIP_FILE_NAME="SPTOptimizer-v${VERSION}.zip"
ZIP_FILE_PATH="${DIST_DIR}/${ZIP_FILE_NAME}"

rm -f "${ZIP_FILE_PATH}"

# Create zip archive from staging
if command -v zip >/dev/null 2>&1; then
    (cd "${STAGING_ROOT}" && zip -r -q "${ZIP_FILE_PATH}" .)
else
    python3 -c "import shutil; shutil.make_archive('${ZIP_FILE_PATH%.zip}', 'zip', '${STAGING_ROOT}')"
fi

rm -rf "${STAGING_ROOT}"

echo -e "${GREEN}Package created: dist/${ZIP_FILE_NAME}${NC}"

echo -e "\n${CYAN}==========================================${NC}"
echo -e "${CYAN}              BUILD COMPLETE!             ${NC}"
echo -e "${CYAN}==========================================${NC}"
