import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:6001';
const TOKEN = __ENV.JWT || 'fluxo-caixa-secret-para-o-teste-987654321';

export const options = {
  scenarios: {
    steady_50_rps: {
      executor: 'constant-arrival-rate',
      rate: 50,
      timeUnit: '1s',
      duration: '60s',
      preAllocatedVUs: 50,
      maxVUs: 200
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
  }
};

export default function () {
  const url = `${BASE_URL}/api/lancamentos`;

  const payload = JSON.stringify({
        comercianteId: "11111111-1111-1111-1111-111111111111",
        valor: 10.50,
        tipo: 1,
        data: new Date().toISOString()
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
      ...(TOKEN ? { Authorization: `Bearer ${TOKEN}` } : {})
    }
  };

  const res = http.post(url, payload, params);

  check(res, {
    'status is 200 or 201': (r) => r.status === 200 || r.status === 201
  });

  sleep(0.01);
}
