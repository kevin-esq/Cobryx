#!/bin/bash
# CHAOS TEST: Payload Mutation Attack
# Attempts to reuse idempotency key with different payload
# Expected: 409 Conflict with IDEMPOTENCY_KEY_REUSED

set -e

BASE_URL="${BASE_URL:-http://localhost:5000}"
ENDPOINT="${ENDPOINT:-/api/payments}"
IDEMPOTENCY_KEY="${IDEMPOTENCY_KEY:-mutation-test-$(date +%s)}"

echo "🎭 CHAOS TEST: Payload Mutation Attack"
echo "======================================="
echo "URL: $BASE_URL$ENDPOINT"
echo "Idempotency Key: $IDEMPOTENCY_KEY"
echo ""

# Original payload
PAYLOAD_ORIGINAL='{"customerId":"00000000-0000-0000-0000-000000000001","paymentMethodId":"00000000-0000-0000-0000-000000000002","amount":100,"currency":"MXN","paymentDate":"2024-01-01T00:00:00Z"}'

# Mutated payload (different amount - attack attempt)
PAYLOAD_MUTATED='{"customerId":"00000000-0000-0000-0000-000000000001","paymentMethodId":"00000000-0000-0000-0000-000000000002","amount":10000,"currency":"MXN","paymentDate":"2024-01-01T00:00:00Z"}'

# Step 1: Send original request
echo "Step 1: Sending original request (amount: 100)..."

RESULT_ORIGINAL=$(curl -s -w "\n%{http_code}" \
    -X POST "$BASE_URL$ENDPOINT" \
    -H "Content-Type: application/json" \
    -H "X-Idempotency-Key: $IDEMPOTENCY_KEY" \
    -H "Authorization: Bearer $AUTH_TOKEN" \
    -d "$PAYLOAD_ORIGINAL")

HTTP_CODE_ORIGINAL=$(echo "$RESULT_ORIGINAL" | tail -n1)
BODY_ORIGINAL=$(echo "$RESULT_ORIGINAL" | head -n -1)

echo "Response: $HTTP_CODE_ORIGINAL"
echo ""

# Step 2: Attempt mutation attack
echo "Step 2: Attempting mutation attack (amount: 10000)..."

RESULT_MUTATED=$(curl -s -w "\n%{http_code}" \
    -X POST "$BASE_URL$ENDPOINT" \
    -H "Content-Type: application/json" \
    -H "X-Idempotency-Key: $IDEMPOTENCY_KEY" \
    -H "Authorization: Bearer $AUTH_TOKEN" \
    -d "$PAYLOAD_MUTATED")

HTTP_CODE_MUTATED=$(echo "$RESULT_MUTATED" | tail -n1)
BODY_MUTATED=$(echo "$RESULT_MUTATED" | head -n -1)

echo "Response: $HTTP_CODE_MUTATED"
echo "Body: $BODY_MUTATED"
echo ""

# Step 3: Verify legitimate replay works
echo "Step 3: Verify legitimate replay (same payload)..."

RESULT_REPLAY=$(curl -s -w "\n%{http_code}" \
    -X POST "$BASE_URL$ENDPOINT" \
    -H "Content-Type: application/json" \
    -H "X-Idempotency-Key: $IDEMPOTENCY_KEY" \
    -H "Authorization: Bearer $AUTH_TOKEN" \
    -d "$PAYLOAD_ORIGINAL")

HTTP_CODE_REPLAY=$(echo "$RESULT_REPLAY" | tail -n1)

echo "Response: $HTTP_CODE_REPLAY"
echo ""

echo "📊 RESULTS"
echo "=========="
echo "Original request:  $HTTP_CODE_ORIGINAL"
echo "Mutation attack:   $HTTP_CODE_MUTATED"
echo "Legitimate replay: $HTTP_CODE_REPLAY"
echo ""

# Validate
if [ "$HTTP_CODE_MUTATED" = "409" ]; then
    if echo "$BODY_MUTATED" | grep -q "IDEMPOTENCY_KEY_REUSED"; then
        echo "🎉 TEST PASSED: Mutation attack blocked with correct error code"
        exit 0
    else
        echo "⚠️  TEST PARTIAL: Got 409 but wrong error code"
        exit 1
    fi
else
    echo "💀 TEST FAILED: Mutation attack was not blocked (got $HTTP_CODE_MUTATED)"
    exit 1
fi
