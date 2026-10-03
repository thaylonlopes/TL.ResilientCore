import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.API_URL || 'http://host.docker.internal:5000';

export const options = {
  stages: [
    { duration: '10s', target: 20 },
    { duration: '30s', target: 50 },
    { duration: '10s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<200'],
    http_req_failed: ['rate<0.01'],
  },
};

function testHealthCheckEndpoint(baseUrl) {
  const response = http.get(`${baseUrl}/`);
  check(response, {
    'HealthCheck status is 200': (res) => res.status === 200,
  });
}

function testPaginatedClientesEndpoint(baseUrl) {
  const response = http.get(`${baseUrl}/clientes?pageNumber=1&pageSize=10`);
  check(response, {
    'Clientes status is 200': (res) => res.status === 200,
  });
}

function testSecureEndpointWithoutToken(baseUrl) {
  const response = http.get(`${baseUrl}/secure-data`);
  check(response, {
    'SecureData status is 401': (res) => res.status === 401,
  });
}

export default function () {
  testHealthCheckEndpoint(BASE_URL);
  testPaginatedClientesEndpoint(BASE_URL);
  testSecureEndpointWithoutToken(BASE_URL);

  sleep(1);
}
