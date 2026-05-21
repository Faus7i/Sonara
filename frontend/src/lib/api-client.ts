import axios from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
});

// 请求拦截器：自动附加 JWT
apiClient.interceptors.request.use((config) => {
  if (typeof window !== 'undefined') {
    const token = localStorage.getItem('auth-token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
  }
  return config;
});

// 响应拦截器：解包 ApiResponse 信封
apiClient.interceptors.response.use(
  (response) => {
    const body = response.data;
    // 后端返回 ApiResponse<T> 格式 { success, data, message, errors }
    if (body && typeof body === 'object' && 'success' in body) {
      if (body.success) {
        return body.data;
      }
      const error = new Error(body.message || '请求失败');
      (error as unknown as Record<string, unknown>).errors = body.errors;
      return Promise.reject(error);
    }
    return body;
  },
  (error) => {
    if (error.response?.status === 401) {
      if (typeof window !== 'undefined') {
        localStorage.removeItem('auth-token');
        localStorage.removeItem('auth-user');
        window.location.href = '/login';
      }
    }
    return Promise.reject(error.response?.data || error);
  }
);

export default apiClient;
