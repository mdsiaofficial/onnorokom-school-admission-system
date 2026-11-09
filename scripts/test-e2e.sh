#!/usr/bin/env bash
# ==============================================================================
# scripts/test-e2e.sh — School Admission System Integration & Concurrency Runner
# ==============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"

cd "${ROOT_DIR}"

echo "=========================================================="
echo "  School Admission System — Automated Test Suite Runner   "
echo "=========================================================="

MODE="${1:-docker}"

if [[ "${MODE}" == "--local" || "${MODE}" == "local" ]]; then
    echo "[INFO] Running tests locally with dotnet test against local/container DB..."
    dotnet test backend/SchoolAdmission.sln --logger:"console;verbosity=normal"
    exit $?
fi

echo "[INFO] Ensuring Docker Compose dependencies are up..."
docker compose up -d db

echo "[INFO] Waiting for PostgreSQL to be healthy..."
until docker compose exec db pg_isready -U postgres > /dev/null 2>&1; do
    echo "  Waiting for PostgreSQL..."
    sleep 1
done

echo "[INFO] Running test suite in Docker container (backend-tests profile)..."
docker compose --profile test run --rm backend-tests
TEST_EXIT_CODE=$?

if [[ ${TEST_EXIT_CODE} -eq 0 ]]; then
    echo "=========================================================="
    echo "  [SUCCESS] All automated tests passed successfully!     "
    echo "=========================================================="
else
    echo "=========================================================="
    echo "  [FAILURE] Tests failed with exit code ${TEST_EXIT_CODE}."
    echo "=========================================================="
fi

exit ${TEST_EXIT_CODE}
