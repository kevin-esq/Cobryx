#!/bin/bash
# CHAOS TEST: Retry Storm
# Simulates 50 concurrent requests with same idempotency key
# Expected: 1 success (200/201), 49 conflicts (409)

set -e

BASE_URL="${BASE_URL:-http://localhost:5000}"
ENDPOINT="${ENDPOINT:-/api/payments}"
IDEMPOTENCY_KEY="${IDEMPOTENCY_KEY:-chaos-test-$(date +%s)}"
CONCURRENCY="${CONCURRENCY:-50}"

echo "🔥 CHAOS TEST: Retry Storm"
echo "=========================="
echo "URL: $BASE_URL$ENDPOINT"
echo "Idempotency Key: $IDEMPOTENCY_KEY"
echo "Concurrency: $CONCURRENCY"
echo ""

# Create payload
PAYLOAD='{"customerId":"00000000-0000-0000-0000-000000000001","paymentMethodId":"00000000-0000-0000-0000-000000000002","amount":100,"currency":"MXN","paymentDate":"2024-01-01T00:00:00Z"}'

# Function to make request
make_request() {
    local result=$(curl -s -w "\n%{http_code}" \
        -X POST "$BASE_URL$ENDPOINT" \
        -H "Content-Type: application/json" \
        -H "X-Idempotency-Key: $IDEMPOTENCY_KEY" \
        -H "Authorization: Bearer $AUTH_TOKEN" \
        -d "$PAYLOAD")
    
    local http_code=$(echo "$result" | tail -n1)
    echo "$http_code"
}

# Run concurrent requests
echo "Launching $CONCURRENCY concurrent requests..."
echo ""

declare -a pids
declare -a results

for i in $(seq 1 $CONCURRENCY); do
    make_request &
    pids+=($!)
done

# Collect results
for pid in "${pids[@]}"; do
    wait $pid
    results+=($?)
done

# Count results
success_count=0
conflict_count=0
error_count=0

for code in "${results[@]}"; do
    case $code in
        200|201) ((success_count++)) ;;
        409) ((conflict_count++)) ;;
        *) ((error_count++)) ;;
    esac
done

echo ""
echo "📊 RESULTS"
echo "=========="
echo "✅ Success (200/201): $success_count"
echo "⚠️  Conflict (409):   $conflict_count"
echo "❌ Errors:            $error_count"
echo ""

# Validate
if [ $success_count -eq 1 ] && [ $conflict_count -eq $((CONCURRENCY - 1)) ]; then
    echo "🎉 TEST PASSED: Exactly 1 execution, rest got conflicts"
    exit 0
else
    echo "💀 TEST FAILED: Expected 1 success and $((CONCURRENCY - 1)) conflicts"
    exit 1
fi
