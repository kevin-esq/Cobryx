#!/bin/bash
# Run all chaos tests for the Cobryx payment system
# Tests: idempotency, webhooks, reconciliation, total system failure
# CHAOS TEST SUITE
# Runs all chaos tests in sequence

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "🔥🔥🔥 CHAOS TEST SUITE 🔥🔥🔥"
echo "================================"
echo ""

# Export common variables
export BASE_URL="${BASE_URL:-http://localhost:5000}"
export AUTH_TOKEN="${AUTH_TOKEN:-}"

PASSED=0
FAILED=0
SKIPPED=0

run_test() {
    local name=$1
    local script=$2

    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "Running: $name"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""

    if bash "$SCRIPT_DIR/$script"; then
        ((PASSED++))
    else
        ((FAILED++))
    fi
}

# Run tests
run_test "Retry Storm" "retry-storm.sh"
run_test "Payload Mutation Attack" "payload-mutation.sh"

# Optional tests (require specific setup)
if [ "${RUN_REDIS_TEST:-false}" = "true" ]; then
    run_test "Redis Down" "redis-down.sh"
else
    echo ""
    echo "⏭️  Skipping Redis Down test (set RUN_REDIS_TEST=true to enable)"
    ((SKIPPED++))
fi

if [ "${RUN_CRASH_TEST:-false}" = "true" ]; then
    run_test "Crash Recovery" "crash-recovery.sh"
else
    echo ""
    echo "⏭️  Skipping Crash Recovery test (set RUN_CRASH_TEST=true to enable)"
    ((SKIPPED++))
fi

# Summary
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📊 CHAOS TEST SUMMARY"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "✅ Passed:  $PASSED"
echo "❌ Failed:  $FAILED"
echo "⏭️  Skipped: $SKIPPED"
echo ""

if [ $FAILED -eq 0 ]; then
    echo "🎉 ALL TESTS PASSED!"
    exit 0
else
    echo "💀 SOME TESTS FAILED"
    exit 1
fi
