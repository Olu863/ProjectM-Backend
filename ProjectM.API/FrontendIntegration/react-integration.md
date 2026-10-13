# React/Next.js Integration

## Installation

```bash
npm install axios
# or
npm install @tanstack/react-query  # For advanced state management
```

## 1. Authentication Context (React Context API)

Create `src/contexts/AuthContext.tsx`:

```typescript
import React, { createContext, useContext, useState, useEffect } from 'react';
import axios from 'axios';

interface User {
  id: number;
  name: string;
  email: string;
  role: string;
}

interface AuthContextType {
  user: User | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  logoutAllDevices: () => Promise<void>;
  isAuthenticated: boolean;
  isLoading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
	// Try to refresh token on mount
	refreshToken().catch(() => {
	  setIsLoading(false);
	});
  }, []);

  const login = async (email: string, password: string) => {
	try {
	  const response = await axios.post(
		'https://your-api-url.com/api/users/login-with-cookie',
		{ email, password },
		{ withCredentials: true } // IMPORTANT: Include cookies
	  );

	  setUser(response.data.user);

	  // Store access token (in memory or sessionStorage)
	  sessionStorage.setItem('accessToken', response.data.token);
	  sessionStorage.setItem('tokenExpiry', response.data.expiresAt);

	  setIsLoading(false);
	} catch (error) {
	  throw new Error('Login failed');
	}
  };

  const refreshToken = async () => {
	try {
	  const response = await axios.post(
		'https://your-api-url.com/api/users/refresh-token-cookie',
		{},
		{ withCredentials: true }
	  );

	  setUser(response.data.user);
	  sessionStorage.setItem('accessToken', response.data.token);
	  sessionStorage.setItem('tokenExpiry', response.data.expiresAt);

	  setIsLoading(false);
	  return response.data.token;
	} catch (error) {
	  setUser(null);
	  sessionStorage.removeItem('accessToken');
	  sessionStorage.removeItem('tokenExpiry');
	  setIsLoading(false);
	  throw error;
	}
  };

  const logout = async () => {
	try {
	  const token = sessionStorage.getItem('accessToken');
	  await axios.post(
		'https://your-api-url.com/api/users/logout',
		{},
		{
		  withCredentials: true,
		  headers: { Authorization: `Bearer ${token}` }
		}
	  );
	} catch (error) {
	  console.error('Logout error:', error);
	} finally {
	  setUser(null);
	  sessionStorage.removeItem('accessToken');
	  sessionStorage.removeItem('tokenExpiry');
	}
  };

  const logoutAllDevices = async () => {
	try {
	  const token = sessionStorage.getItem('accessToken');
	  await axios.post(
		'https://your-api-url.com/api/users/logout-all-devices',
		{},
		{
		  withCredentials: true,
		  headers: { Authorization: `Bearer ${token}` }
		}
	  );
	} catch (error) {
	  console.error('Logout all devices error:', error);
	} finally {
	  setUser(null);
	  sessionStorage.removeItem('accessToken');
	  sessionStorage.removeItem('tokenExpiry');
	}
  };

  return (
	<AuthContext.Provider
	  value={{
		user,
		login,
		logout,
		logoutAllDevices,
		isAuthenticated: !!user,
		isLoading
	  }}
	>
	  {children}
	</AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
	throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};
```

## 2. Axios Interceptor for Auto Token Refresh

Create `src/lib/axios.ts`:

```typescript
import axios from 'axios';

const API_BASE_URL = 'https://your-api-url.com/api';

// Create axios instance
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true, // IMPORTANT: Always include credentials for cookies
  headers: {
	'Content-Type': 'application/json',
  },
});

// Request interceptor - Add access token to all requests
apiClient.interceptors.request.use(
  (config) => {
	const token = sessionStorage.getItem('accessToken');
	if (token) {
	  config.headers.Authorization = `Bearer ${token}`;
	}
	return config;
  },
  (error) => {
	return Promise.reject(error);
  }
);

// Response interceptor - Auto refresh on 401
let isRefreshing = false;
let failedQueue: any[] = [];

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((prom) => {
	if (error) {
	  prom.reject(error);
	} else {
	  prom.resolve(token);
	}
  });
  failedQueue = [];
};

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
	const originalRequest = error.config;

	// If error is 401 and we haven't tried to refresh yet
	if (error.response?.status === 401 && !originalRequest._retry) {
	  if (isRefreshing) {
		// If already refreshing, queue this request
		return new Promise((resolve, reject) => {
		  failedQueue.push({ resolve, reject });
		})
		  .then((token) => {
			originalRequest.headers.Authorization = `Bearer ${token}`;
			return apiClient(originalRequest);
		  })
		  .catch((err) => {
			return Promise.reject(err);
		  });
	  }

	  originalRequest._retry = true;
	  isRefreshing = true;

	  try {
		// Try to refresh the token
		const response = await axios.post(
		  `${API_BASE_URL}/users/refresh-token-cookie`,
		  {},
		  { withCredentials: true }
		);

		const newToken = response.data.token;
		sessionStorage.setItem('accessToken', newToken);
		sessionStorage.setItem('tokenExpiry', response.data.expiresAt);

		// Update authorization header
		apiClient.defaults.headers.common.Authorization = `Bearer ${newToken}`;
		originalRequest.headers.Authorization = `Bearer ${newToken}`;

		processQueue(null, newToken);

		// Retry original request
		return apiClient(originalRequest);
	  } catch (refreshError) {
		processQueue(refreshError, null);

		// Refresh failed - redirect to login
		sessionStorage.removeItem('accessToken');
		sessionStorage.removeItem('tokenExpiry');
		window.location.href = '/login';

		return Promise.reject(refreshError);
	  } finally {
		isRefreshing = false;
	  }
	}

	return Promise.reject(error);
  }
);

export default apiClient;
```

## 3. Login Component

Create `src/components/Login.tsx`:

```typescript
import React, { useState } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { useNavigate } from 'react-router-dom';

export const Login: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
	e.preventDefault();
	setError('');
	setLoading(true);

	try {
	  await login(email, password);
	  navigate('/dashboard'); // Redirect after successful login
	} catch (err) {
	  setError('Invalid email or password');
	} finally {
	  setLoading(false);
	}
  };

  return (
	<div className="login-container">
	  <h2>Login</h2>
	  <form onSubmit={handleSubmit}>
		<div>
		  <label>Email:</label>
		  <input
			type="email"
			value={email}
			onChange={(e) => setEmail(e.target.value)}
			required
		  />
		</div>
		<div>
		  <label>Password:</label>
		  <input
			type="password"
			value={password}
			onChange={(e) => setPassword(e.target.value)}
			required
		  />
		</div>
		{error && <div className="error">{error}</div>}
		<button type="submit" disabled={loading}>
		  {loading ? 'Logging in...' : 'Login'}
		</button>
	  </form>
	</div>
  );
};
```

## 4. Protected Route Component

Create `src/components/ProtectedRoute.tsx`:

```typescript
import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRole?: string;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ 
  children, 
  requiredRole 
}) => {
  const { isAuthenticated, user, isLoading } = useAuth();

  if (isLoading) {
	return <div>Loading...</div>;
  }

  if (!isAuthenticated) {
	return <Navigate to="/login" replace />;
  }

  if (requiredRole && user?.role !== requiredRole) {
	return <Navigate to="/unauthorized" replace />;
  }

  return <>{children}</>;
};
```

## 5. Using the API Client

Example of making authenticated API calls:

```typescript
import apiClient from '../lib/axios';

// Example: Fetch user data
export const getUserById = async (id: number) => {
  const response = await apiClient.get(`/users/${id}`);
  return response.data;
};

// Example: Update user
export const updateUser = async (id: number, data: any) => {
  const response = await apiClient.put(`/users/${id}`, data);
  return response.data;
};

// Example: Custom hook
import { useQuery } from '@tanstack/react-query';

export const useUser = (id: number) => {
  return useQuery({
	queryKey: ['user', id],
	queryFn: () => getUserById(id),
  });
};
```

## 6. App Setup

Update `src/App.tsx`:

```typescript
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Login } from './components/Login';
import Dashboard from './pages/Dashboard';

function App() {
  return (
	<BrowserRouter>
	  <AuthProvider>
		<Routes>
		  <Route path="/login" element={<Login />} />

		  <Route
			path="/dashboard"
			element={
			  <ProtectedRoute>
				<Dashboard />
			  </ProtectedRoute>
			}
		  />

		  <Route
			path="/admin"
			element={
			  <ProtectedRoute requiredRole="Admin">
				<AdminPanel />
			  </ProtectedRoute>
			}
		  />
		</Routes>
	  </AuthProvider>
	</BrowserRouter>
  );
}

export default App;
```

## 7. Environment Variables

Create `.env`:

```env
VITE_API_BASE_URL=https://your-api-url.com/api
# or for Create React App
REACT_APP_API_BASE_URL=https://your-api-url.com/api
```

## 8. Logout Component

```typescript
import { useAuth } from '../contexts/AuthContext';

export const LogoutButton: React.FC = () => {
  const { logout, logoutAllDevices } = useAuth();

  return (
	<div>
	  <button onClick={logout}>Logout</button>
	  <button onClick={logoutAllDevices}>Logout All Devices</button>
	</div>
  );
};
```

## Testing

Test the integration:

1. Login with valid credentials
2. Verify access token is stored
3. Make authenticated API calls
4. Wait for token to expire and verify auto-refresh
5. Logout and verify token is cleared
6. Try accessing protected routes without auth

## Notes

- Always use HTTPS in production
- Consider using `httpOnly` cookies for access tokens too (requires API changes)
- Implement proper error boundaries
- Add loading states for better UX
- Consider implementing a token refresh timer instead of waiting for 401
