import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    scenarios: {
        // Scenario 1: concurrent invoice creation (plan limit enforcement)
        invoice_stress: {
            executor: 'constant-vus',
            vus: 50,
            duration: '30s',
            gracefulStop: '5s',
            exec: 'invoice_creation',
        },
        // Scenario 2: concurrent checkout session creation (upgrade/purchase)
        checkout_stress: {
            executor: 'per-vu-iterations',
            vus: 20,
            iterations: 10,
            maxDuration: '1m',
            exec: 'subscription_checkout',
        },
        // Scenario 3: mixed read/write traffic
        mixed_traffic: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: '30s', target: 50 },
                { duration: '1m', target: 50 },
                { duration: '20s', target: 0 },
            ],
            exec: 'mixed_load',
        },
    },
    thresholds: {
        http_req_failed: ['rate<0.01'],
        http_req_duration: ['p(95)<500'],
    },
};

const API_BASE_URL = __ENV.API_URL || 'http://localhost:5142/api/v1';
const TENANT_ID = __ENV.TENANT_ID || '3fa85f64-5717-4562-b3fc-2c963f66afa6';
const AUTH_TOKEN = __ENV.AUTH_TOKEN;
const PLAN_ID = __ENV.PLAN_ID || '9fa85f64-5717-4562-b3fc-2c963f66afa6';
const CUSTOMER_ID = __ENV.CUSTOMER_ID || '4fa85f64-5717-4562-b3fc-2c963f66afa6';

const params = {
    headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${AUTH_TOKEN}`,
        'X-Tenant-Id': TENANT_ID,
    },
};

function idempotencyKey() {
    return `${__VU}-${__ITER}-${Date.now()}`;
}

export function invoice_creation() {
    const payload = JSON.stringify({
        customerId: CUSTOMER_ID,
        dueDate: '2026-12-31T23:59:59Z',
        items: [{ description: 'Test Item', quantity: 1, unitPrice: 100.0 }],
    });

    const res = http.post(`${API_BASE_URL}/invoices`, payload, {
        ...params,
        headers: {
            ...params.headers,
            'X-Idempotency-Key': idempotencyKey(),
        },
    });

    check(res, {
        'invoice created (201)': (r) => r.status === 201,
        'plan limit reached (403)': (r) => r.status === 403,
    });
}

export function subscription_checkout() {
    const payload = JSON.stringify({
        planId: PLAN_ID,
        successUrl: 'https://app.cobryx.mx/billing/success',
        cancelUrl: 'https://app.cobryx.mx/billing/cancel',
    });

    const res = http.post(`${API_BASE_URL}/subscription/checkout`, payload, params);

    check(res, {
        'checkout session created (200)': (r) => r.status === 200,
    });
}

export function mixed_load() {
    if (Math.random() < 0.7) {
        const res = http.get(`${API_BASE_URL}/subscription/status`, params);
        check(res, { 'subscription status (200)': (r) => r.status === 200 });
    } else {
        invoice_creation();
    }
    sleep(1);
}
