import { apiClient } from '../src/api/client';

describe('apiClient', () => {
  test('apiClient is initialized with default baseURL /api and credentials', () => {
    expect(apiClient.defaults.baseURL).toBe('/api');
    expect(apiClient.defaults.withCredentials).toBe(true);
  });
});
