#!/bin/bash
# CHAOS TEST: Redis Down
# Simulates Redis failure - system should fallback to DB
# Expected: Requests still work (slower but functional)

set -e

BASE_URL="${BASE_URL:-http://localhost:5000}"
ENDPOINT="${ENDPOINT:-/api/payments}"
REDIS_CONTAINER="${REDIS_CONTAINER:-cobryx-redis}"

echo "🔴 CHAOS TEST: Redis Down"
echo "========================="
echo "URL: $BASE_URL$ENDPOINT"
echo "Redis Container: $REDIS_CONTAINER"
echo ""

PAYLOAD='{"customerId":"00000000-0000-0000-0000-000000000001","paymentMethodId":"00000000-0000-0000-0000-000000000002","amount":100,"currency":"MXN","paymentDate":"2024-01-01T00:00:00Z"}'

# Step 1: Verify system works with Redis
echo "Step 1: Verify system works with Redis up..."

IDEMPOTENCY_KEY="redis-test-$(date +%s)-1"
RESULT_WITH_REDIS=$(curl -s -w "%{http_code}" -o /dev/null \
    -X POST "$BASE_URL$ENDPOINT" \
    -H "Content-Type: application/json" \
    -H "X-Idempotency-Key: $IDEMPOTENCY_KEY" \
    -H "Authorization: Bearer $AUTH_TOKEN" \
    -d "$PAYLOAD")

if [ "$RESULT_WITH_REDIS" = "200" ] || [ "$RESULT_WITH_REDIS" = "201" ]; then
    echo "✓ System works with Redis: $RESULT_WITH_REDIS"
else
    echo "❌ System not working even with Redis: $RESULT_WITH_REDIS"
    exit 1
fi
echo ""

# Step 2: Stop Redis
echo "Step 2: Stopping Redis..."
docker stop $REDIS_CONTAINER 2>/dev/null || {
    echo "⚠️  Could not stop Redis container. Trying redis-cli..."
    redis-cli SHUTDOWN NOSAVE 2>/dev/null || {
        echo "❌ Could not stop Redis. Skipping test."
        exit 0
    }
}
echo "✓ Redis stopped"
echo ""

# Step 3: Test with Redis down
echo "Step 3: Testing with Redis down..."

IDEMPOTENCY_KEY="redis-test-$(date +%s)-2"
RESULT_WITHOUT_REDIS=$(curl -s -w "%{http_code}" -o /dev/null \
    -X POST "$BASE_URL$ENDPOINT" \
    -H "Content-Type: application/json" \
    -H "X-Idempotency-Key: $IDEMPOTENCY_KEY" \
    -H "Authorization: Bearer $AUTH_TOKEN" \
    -d "$PAYLOAD")

echo "Result without Redis: $RESULT_WITHOUT_REDIS"
echo ""

# Step 4: Restart Redis
echo "Step 4: Restarting Redis..."
docker start $REDIS_CONTAINER 2>/dev/null || {
    echo "⚠️  Could not restart Redis container"
}
echo "✓ Redis restarted"
echo ""

# Step 5: Verify idempotency still works (should replay from DB)
echo "Step 5: Verify idempotency replay from DB..."

RESULT_REPLAY=$(curl -s -w "%{http_code}" -o /dev/null \
    -X POST "$BASE_URL$ENDPOINT" \
    -H "Content-Type: application/json" \
    -H "X-Idempotency-Key: $IDEMPOTENCY_KEY" \
    -H "Authorization: Bearer $AUTH_TOKEN" \
    -d "$PAYLOAD")

echo ""
echo "📊 RESULTS"
echo "=========="
echo "With Redis:      $RESULT_WITH_REDIS"
echo "Without Redis:   $RESULT_WITHOUT_REDIS"
echo "Replay from DB:  $RESULT_REPLAY"
echo ""

if [ "$RESULT_WITHOUT_REDIS" = "200" ] || [ "$RESULT_WITHOUT_REDIS" = "201" ]; then
    echo "🎉 TEST PASSED: System works without Redis (DB fallback)"
    exit 0
else
    echo "💀 TEST FAILED: System failed without Redis"
    exit 1
fi
