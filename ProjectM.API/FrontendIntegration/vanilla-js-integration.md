# Vanilla JavaScript Integration

This guide shows how to integrate with the ProjectM API using plain JavaScript (no frameworks).

## 1. Configuration

Create `js/config.js`:

```javascript
const API_CONFIG = {
  baseUrl: 'https://your-api-url.com/api',
  endpoints: {
	login: '/users/login-with-cookie',
	refresh: '/users/refresh-token-cookie',
	logout: '/users/logout',
	logoutAll: '/users/logout-all-devices'
  }
};
```

## 2. Authentication Service

Create `js/auth-service.js`:

```javascript
class AuthService {
  constructor() {
	this.accessToken = null;
	this.tokenExpiry = null;
	this.user = null;
	this.isRefreshing = false;
	this.refreshSubscribers = [];

	// Load token from storage on init
	this.loadFromStorage();
  }

  // Load token from sessionStorage
  loadFromStorage() {
	this.accessToken = sessionStorage.getItem('accessToken');
	this.tokenExpiry = sessionStorage.getItem('tokenExpiry');
	const userData = sessionStorage.getItem('user');
	this.user = userData ? JSON.parse(userData) : null;
  }

  // Save token to storage
  saveToStorage(token, expiry, user) {
	sessionStorage.setItem('accessToken', token);
	sessionStorage.setItem('tokenExpiry', expiry);
	sessionStorage.setItem('user', JSON.stringify(user));
	this.accessToken = token;
	this.tokenExpiry = expiry;
	this.user = user;
  }

  // Clear storage
  clearStorage() {
	sessionStorage.removeItem('accessToken');
	sessionStorage.removeItem('tokenExpiry');
	sessionStorage.removeItem('user');
	this.accessToken = null;
	this.tokenExpiry = null;
	this.user = null;
  }

  // Login
  async login(email, password) {
	try {
	  const response = await fetch(`${API_CONFIG.baseUrl}${API_CONFIG.endpoints.login}`, {
		method: 'POST',
		headers: {
		  'Content-Type': 'application/json',
		},
		credentials: 'include', // IMPORTANT: Include cookies
		body: JSON.stringify({ email, password })
	  });

	  if (!response.ok) {
		throw new Error('Login failed');
	  }

	  const data = await response.json();
	  this.saveToStorage(data.token, data.expiresAt, data.user);

	  return data;
	} catch (error) {
	  console.error('Login error:', error);
	  throw error;
	}
  }

  // Refresh token
  async refreshToken() {
	if (this.isRefreshing) {
	  return new Promise((resolve, reject) => {
		this.refreshSubscribers.push({ resolve, reject });
	  });
	}

	this.isRefreshing = true;

	try {
	  const response = await fetch(`${API_CONFIG.baseUrl}${API_CONFIG.endpoints.refresh}`, {
		method: 'POST',
		credentials: 'include',
	  });

	  if (!response.ok) {
		throw new Error('Token refresh failed');
	  }

	  const data = await response.json();
	  this.saveToStorage(data.token, data.expiresAt, data.user);

	  // Resolve all pending requests
	  this.refreshSubscribers.forEach(subscriber => subscriber.resolve(data.token));
	  this.refreshSubscribers = [];

	  return data.token;
	} catch (error) {
	  // Reject all pending requests
	  this.refreshSubscribers.forEach(subscriber => subscriber.reject(error));
	  this.refreshSubscribers = [];

	  this.clearStorage();
	  window.location.href = '/login.html';
	  throw error;
	} finally {
	  this.isRefreshing = false;
	}
  }

  // Logout
  async logout() {
	try {
	  await fetch(`${API_CONFIG.baseUrl}${API_CONFIG.endpoints.logout}`, {
		method: 'POST',
		headers: {
		  'Authorization': `Bearer ${this.accessToken}`
		},
		credentials: 'include',
	  });
	} catch (error) {
	  console.error('Logout error:', error);
	} finally {
	  this.clearStorage();
	  window.location.href = '/login.html';
	}
  }

  // Logout all devices
  async logoutAllDevices() {
	try {
	  await fetch(`${API_CONFIG.baseUrl}${API_CONFIG.endpoints.logoutAll}`, {
		method: 'POST',
		headers: {
		  'Authorization': `Bearer ${this.accessToken}`
		},
		credentials: 'include',
	  });
	} catch (error) {
	  console.error('Logout all devices error:', error);
	} finally {
	  this.clearStorage();
	  window.location.href = '/login.html';
	}
  }

  // Check if user is authenticated
  isAuthenticated() {
	return !!this.accessToken && !!this.user;
  }

  // Get current user
  getCurrentUser() {
	return this.user;
  }

  // Get access token
  getAccessToken() {
	return this.accessToken;
  }
}

// Create singleton instance
const authService = new AuthService();
```

## 3. API Client with Auto-Refresh

Create `js/api-client.js`:

```javascript
class ApiClient {
  constructor(authService) {
	this.authService = authService;
  }

  // Make authenticated request
  async request(url, options = {}) {
	// Add authorization header
	const headers = {
	  'Content-Type': 'application/json',
	  ...options.headers,
	};

	if (this.authService.getAccessToken()) {
	  headers['Authorization'] = `Bearer ${this.authService.getAccessToken()}`;
	}

	const config = {
	  ...options,
	  headers,
	  credentials: 'include', // Include cookies
	};

	try {
	  let response = await fetch(`${API_CONFIG.baseUrl}${url}`, config);

	  // If 401, try to refresh token and retry
	  if (response.status === 401 && !options._retry) {
		try {
		  await this.authService.refreshToken();

		  // Retry with new token
		  headers['Authorization'] = `Bearer ${this.authService.getAccessToken()}`;
		  response = await fetch(`${API_CONFIG.baseUrl}${url}`, {
			...config,
			headers,
			_retry: true
		  });
		} catch (refreshError) {
		  // Refresh failed, redirect to login
		  window.location.href = '/login.html';
		  throw refreshError;
		}
	  }

	  if (!response.ok) {
		throw new Error(`HTTP error! status: ${response.status}`);
	  }

	  return await response.json();
	} catch (error) {
	  console.error('API request error:', error);
	  throw error;
	}
  }

  // Convenience methods
  get(url) {
	return this.request(url, { method: 'GET' });
  }

  post(url, data) {
	return this.request(url, {
	  method: 'POST',
	  body: JSON.stringify(data),
	});
  }

  put(url, data) {
	return this.request(url, {
	  method: 'PUT',
	  body: JSON.stringify(data),
	});
  }

  delete(url) {
	return this.request(url, { method: 'DELETE' });
  }
}

// Create singleton instance
const apiClient = new ApiClient(authService);
```

## 4. Login Page

Create `login.html`:

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Login - ProjectM</title>
  <link rel="stylesheet" href="css/styles.css">
</head>
<body>
  <div class="login-container">
	<h1>Login</h1>
	<form id="loginForm">
	  <div class="form-group">
		<label for="email">Email:</label>
		<input type="email" id="email" required>
	  </div>
	  <div class="form-group">
		<label for="password">Password:</label>
		<input type="password" id="password" required>
	  </div>
	  <div id="error" class="error-message" style="display: none;"></div>
	  <button type="submit">Login</button>
	</form>
  </div>

  <script src="js/config.js"></script>
  <script src="js/auth-service.js"></script>
  <script>
	// Check if already logged in
	if (authService.isAuthenticated()) {
	  window.location.href = '/dashboard.html';
	}

	const loginForm = document.getElementById('loginForm');
	const errorDiv = document.getElementById('error');

	loginForm.addEventListener('submit', async (e) => {
	  e.preventDefault();

	  const email = document.getElementById('email').value;
	  const password = document.getElementById('password').value;

	  errorDiv.style.display = 'none';

	  try {
		await authService.login(email, password);
		window.location.href = '/dashboard.html';
	  } catch (error) {
		errorDiv.textContent = 'Invalid email or password';
		errorDiv.style.display = 'block';
	  }
	});
  </script>
</body>
</html>
```

## 5. Dashboard Page

Create `dashboard.html`:

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Dashboard - ProjectM</title>
  <link rel="stylesheet" href="css/styles.css">
</head>
<body>
  <div class="dashboard-container">
	<header>
	  <h1>Dashboard</h1>
	  <div class="user-info">
		<span id="userName"></span>
		<button onclick="handleLogout()">Logout</button>
		<button onclick="handleLogoutAll()">Logout All Devices</button>
	  </div>
	</header>

	<main>
	  <h2>Welcome!</h2>
	  <div id="content">
		<p>Loading...</p>
	  </div>
	</main>
  </div>

  <script src="js/config.js"></script>
  <script src="js/auth-service.js"></script>
  <script src="js/api-client.js"></script>
  <script>
	// Check authentication
	if (!authService.isAuthenticated()) {
	  window.location.href = '/login.html';
	}

	// Display user info
	const user = authService.getCurrentUser();
	document.getElementById('userName').textContent = user.name;

	// Load dashboard data
	async function loadDashboard() {
	  try {
		// Example: Fetch user data
		const userData = await apiClient.get(`/users/${user.id}`);

		document.getElementById('content').innerHTML = `
		  <h3>User Details</h3>
		  <p><strong>Name:</strong> ${userData.name}</p>
		  <p><strong>Email:</strong> ${userData.email}</p>
		  <p><strong>Role:</strong> ${userData.role}</p>
		  <p><strong>Company:</strong> ${userData.company}</p>
		`;
	  } catch (error) {
		console.error('Error loading dashboard:', error);
		document.getElementById('content').innerHTML = 
		  '<p class="error">Error loading dashboard data</p>';
	  }
	}

	async function handleLogout() {
	  if (confirm('Are you sure you want to logout?')) {
		await authService.logout();
	  }
	}

	async function handleLogoutAll() {
	  if (confirm('This will logout all devices. Continue?')) {
		await authService.logoutAllDevices();
	  }
	}

	// Load dashboard on page load
	loadDashboard();
  </script>
</body>
</html>
```

## 6. Example API Usage

Create `js/examples.js`:

```javascript
// Example: Fetch all users
async function getAllUsers() {
  try {
	const users = await apiClient.get('/users');
	console.log('Users:', users);
	return users;
  } catch (error) {
	console.error('Error fetching users:', error);
  }
}

// Example: Update user
async function updateUser(userId, data) {
  try {
	const updatedUser = await apiClient.put(`/users/${userId}`, data);
	console.log('User updated:', updatedUser);
	return updatedUser;
  } catch (error) {
	console.error('Error updating user:', error);
  }
}

// Example: Change password
async function changePassword(userId, oldPassword, newPassword) {
  try {
	const result = await apiClient.post(`/users/${userId}/change-password`, {
	  oldPassword,
	  newPassword
	});
	console.log('Password changed:', result);
	return result;
  } catch (error) {
	console.error('Error changing password:', error);
  }
}
```

## 7. Route Protection

Create `js/route-guard.js`:

```javascript
// Add this to every protected page
(function() {
  if (!authService.isAuthenticated()) {
	window.location.href = '/login.html';
  }
})();
```

Then include it in protected pages:

```html
<script src="js/config.js"></script>
<script src="js/auth-service.js"></script>
<script src="js/route-guard.js"></script>
```

## 8. Basic Styles

Create `css/styles.css`:

```css
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
  background: #f5f5f5;
}

.login-container {
  max-width: 400px;
  margin: 100px auto;
  padding: 40px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.1);
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: 500;
}

.form-group input {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
}

button {
  width: 100%;
  padding: 12px;
  background: #007bff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 16px;
}

button:hover {
  background: #0056b3;
}

.error-message {
  color: #dc3545;
  padding: 10px;
  margin: 10px 0;
  background: #f8d7da;
  border-radius: 4px;
}

.dashboard-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
}

header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px 0;
  border-bottom: 1px solid #ddd;
  margin-bottom: 30px;
}

.user-info {
  display: flex;
  gap: 10px;
  align-items: center;
}

.user-info button {
  width: auto;
  padding: 8px 16px;
}
```

## Testing

1. Open `login.html` in browser
2. Login with valid credentials
3. Verify redirect to dashboard
4. Check browser DevTools → Application → Session Storage for tokens
5. Make API calls and verify auto-refresh on 401
6. Test logout functionality

## Production Considerations

1. **Use HTTPS**: Always use HTTPS in production
2. **Environment Variables**: Use different API URLs for dev/prod
3. **Error Handling**: Add proper error boundaries and user feedback
4. **Security**: Never log sensitive data
5. **Token Expiry**: Consider implementing proactive token refresh before expiry
