#!/bin/bash
# CHAOS TEST: Crash Recovery
# Simulates app crash during request processing
# Expected: Retry succeeds after timeout (2 min)

set -e

BASE_URL="${BASE_URL:-http://localhost:5000}"
ENDPOINT="${ENDPOINT:-/api/payments}"
IDEMPOTENCY_KEY="${IDEMPOTENCY_KEY:-crash-test-$(date +%s)}"
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-130}" # 2 min + buffer

echo "💥 CHAOS TEST: Crash Recovery"
echo "=============================="
echo "URL: $BASE_URL$ENDPOINT"
echo "Idempotency Key: $IDEMPOTENCY_KEY"
echo "Timeout: ${TIMEOUT_SECONDS}s"
echo ""

PAYLOAD='{"customerId":"00000000-0000-0000-0000-000000000001","paymentMethodId":"00000000-0000-0000-0000-000000000002","amount":100,"currency":"MXN","paymentDate":"2024-01-01T00:00:00Z"}'

# Step 1: Start request and kill it mid-flight
echo "Step 1: Starting request and simulating crash..."

# Start request in background with short timeout (will fail)
timeout 1 curl -s -X POST "$BASE_URL$ENDPOINT" \
    -H "Content-Type: application/json" \
    -H "X-Idempotency-Key: $IDEMPOTENCY_KEY" \
    -H "Authorization: Bearer $AUTH_TOKEN" \
    -d "$PAYLOAD" &

REQUEST_PID=$!
sleep 0.5

# Kill the request (simulates crash)
kill $REQUEST_PID 2>/dev/null || true

echo "✓ Request killed (simulating crash)"
echo ""

# Step 2: Immediate retry should get conflict
echo "Step 2: Immediate retry (should get 409 conflict)..."

IMMEDIATE_RESULT=$(curl -s -w "%{http_code}" -o /dev/null \
    -X POST "$BASE_URL$ENDPOINT" \
    -H "Content-Type: application/json" \
    -H "X-Idempotency-Key: $IDEMPOTENCY_KEY" \
    -H "Authorization: Bearer $AUTH_TOKEN" \
    -d "$PAYLOAD")

if [ "$IMMEDIATE_RESULT" = "409" ]; then
    echo "✓ Got 409 Conflict (expected - request still processing)"
else
    echo "⚠️  Got $IMMEDIATE_RESULT (expected 409)"
fi
echo ""

# Step 3: Wait for timeout
echo "Step 3: Waiting for processing timeout (${TIMEOUT_SECONDS}s)..."
echo "        (In production, stuck records are recovered after 2 minutes)"

for i in $(seq 1 $TIMEOUT_SECONDS); do
    printf "\r        Waiting... %d/%d seconds" $i $TIMEOUT_SECONDS
    sleep 1
done
echo ""
echo ""

# Step 4: Retry after timeout should succeed
echo "Step 4: Retry after timeout (should succeed)..."

RETRY_RESULT=$(curl -s -w "%{http_code}" -o /dev/null \
    -X POST "$BASE_URL$ENDPOINT" \
    -H "Content-Type: application/json" \
    -H "X-Idempotency-Key: $IDEMPOTENCY_KEY" \
    -H "Authorization: Bearer $AUTH_TOKEN" \
    -d "$PAYLOAD")

echo ""
echo "📊 RESULTS"
echo "=========="
echo "Immediate retry: $IMMEDIATE_RESULT"
echo "After timeout:   $RETRY_RESULT"
echo ""

if [ "$RETRY_RESULT" = "200" ] || [ "$RETRY_RESULT" = "201" ]; then
    echo "🎉 TEST PASSED: Crash recovery worked!"
    exit 0
else
    echo "💀 TEST FAILED: Expected 200/201 after timeout, got $RETRY_RESULT"
    exit 1
fi
