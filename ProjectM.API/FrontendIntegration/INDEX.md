# 📚 Complete Frontend Integration Package

## 🎉 Welcome!

Your ProjectM API is now equipped with enterprise-grade JWT authentication. This folder contains everything you need to integrate your frontend application.

## 📁 What's Included

### 📖 Documentation Files

| File | Description | Use When |
|------|-------------|----------|
| **SUMMARY.md** | 🌟 **START HERE** - Complete overview & quick examples | You want a high-level overview |
| **QUICKSTART.md** | Fast implementation guide with code snippets | You want to get started quickly |
| **README.md** | Detailed integration guide & best practices | You need comprehensive documentation |
| **ENVIRONMENT-SETUP.md** | Configuration for dev/staging/production | You're setting up environments |
| **react-integration.md** | Full React/Next.js implementation with Context API | You're building with React |
| **vanilla-js-integration.md** | Plain JavaScript implementation | You're using vanilla JS or jQuery |
| **test-page.html** | Interactive test page | You want to test the API visually |
| **ProjectM-Postman-Collection.json** | Postman collection | You want to test with Postman |

### 🎯 Quick Navigation

**New to this?** → Start with `SUMMARY.md`  
**Want code now?** → Go to `QUICKSTART.md`  
**Using React?** → Read `react-integration.md`  
**Using vanilla JS?** → Read `vanilla-js-integration.md`  
**Setting up production?** → Check `ENVIRONMENT-SETUP.md`  
**Want to test?** → Open `test-page.html` or import Postman collection

## 🚀 Get Started in 5 Minutes

### 1. Configure Your API (1 min)

Edit `ProjectM.API/appsettings.json`:

```json
{
  "Jwt": {
	"Key": "YourSuperSecretKeyAtLeast32CharactersLong!",
	"ExpiryInMinutes": 15
  }
}
```

Update CORS in `Program.cs` line 24:
```csharp
policy.WithOrigins("http://localhost:3000")  // Your frontend URL
```

### 2. Run Your API (1 min)

```bash
cd ProjectM.API
dotnet run
```

### 3. Test with HTML Page (2 mins)

1. Open `test-page.html` in your browser
2. Update API URL if needed
3. Enter credentials and click Login
4. See it work! ✨

### 4. Integrate Your Frontend (1 min)

**React:**
```javascript
import axios from 'axios';

const api = axios.create({
  baseURL: 'https://localhost:7xxx/api',
  withCredentials: true
});

const login = async (email, password) => {
  const { data } = await api.post('/users/login-with-cookie', { email, password });
  sessionStorage.setItem('accessToken', data.token);
  return data;
};
```

**Vanilla JS:**
```javascript
async function login(email, password) {
  const res = await fetch('https://localhost:7xxx/api/users/login-with-cookie', {
	method: 'POST',
	headers: { 'Content-Type': 'application/json' },
	credentials: 'include',
	body: JSON.stringify({ email, password })
  });
  const data = await res.json();
  sessionStorage.setItem('accessToken', data.token);
  return data;
}
```

## 🔐 API Endpoints Reference

### Authentication
- `POST /api/users/login-with-cookie` - Login with cookie
- `POST /api/users/refresh-token-cookie` - Refresh access token
- `POST /api/users/logout` - Logout current device
- `POST /api/users/logout-all-devices` - Logout all devices

### Users
- `GET /api/users/{id}` - Get user by ID
- `PUT /api/users/{id}` - Update user
- `POST /api/users/{id}/change-password` - Change password

See full API documentation in `README.md`

## 🧪 Testing Options

### Option 1: HTML Test Page (Easiest)
1. Open `test-page.html`
2. Enter API URL
3. Login and test

### Option 2: Postman (Most Powerful)
1. Import `ProjectM-Postman-Collection.json`
2. Set `baseUrl` environment variable
3. Run requests

### Option 3: cURL (CLI)
```bash
curl -X POST https://localhost:7xxx/api/users/login-with-cookie \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"password"}' \
  -c cookies.txt
```

## 📝 Example Workflow

### Complete Authentication Flow

```javascript
// 1. Login
const loginResponse = await api.post('/users/login-with-cookie', {
  email: 'user@example.com',
  password: 'password123'
});

// Save token
sessionStorage.setItem('accessToken', loginResponse.data.token);

// 2. Make authenticated requests
const userData = await api.get('/users/1', {
  headers: { 'Authorization': `Bearer ${sessionStorage.getItem('accessToken')}` }
});

// 3. Refresh when expired
const refreshResponse = await api.post('/users/refresh-token-cookie');
sessionStorage.setItem('accessToken', refreshResponse.data.token);

// 4. Logout
await api.post('/users/logout', {}, {
  headers: { 'Authorization': `Bearer ${sessionStorage.getItem('accessToken')}` }
});
sessionStorage.removeItem('accessToken');
```

## 🛡️ Security Checklist

Before deploying to production:

- [ ] **JWT Secret** - Use a strong random key (32+ characters)
- [ ] **HTTPS** - Always use HTTPS in production
- [ ] **CORS** - Restrict to your specific domain(s)
- [ ] **Secure Cookies** - Set `Secure: true` in production
- [ ] **Environment Variables** - Never commit secrets to Git
- [ ] **Token Expiry** - Keep access tokens short-lived (15 min)
- [ ] **Database** - Use connection string from environment

## 🐛 Troubleshooting

### "CORS error"
→ Add your frontend URL to CORS policy in `Program.cs`

### "Cookie not set"
→ Ensure `credentials: 'include'` in your fetch/axios config

### "401 Unauthorized"
→ Check `Authorization: Bearer {token}` header is present

### "Invalid JWT Key"
→ Use a key with at least 32 characters

See full troubleshooting guide in `ENVIRONMENT-SETUP.md`

## 📚 Learning Path

**For Beginners:**
1. Read `SUMMARY.md`
2. Open `test-page.html` and play with it
3. Read `QUICKSTART.md`
4. Implement based on your framework guide

**For Experienced Developers:**
1. Skim `SUMMARY.md`
2. Go straight to your framework guide
3. Set up environment from `ENVIRONMENT-SETUP.md`
4. Test with Postman collection

## 🎯 Framework-Specific Guides

### React / Next.js
→ `react-integration.md`
- Complete Context API implementation
- Auto-refresh interceptor
- Protected routes
- TypeScript examples

### Vanilla JavaScript
→ `vanilla-js-integration.md`  
- No framework required
- Class-based service
- Auto-refresh implementation
- ES6+ examples

### Your Framework Not Listed?
The principles in `vanilla-js-integration.md` apply to all frameworks. Key concepts:
1. Always include `credentials: 'include'`
2. Add `Authorization: Bearer {token}` header
3. Store token in sessionStorage
4. Implement auto-refresh on 401

## 💡 Pro Tips

**Development:**
- Use `test-page.html` for quick testing
- Import Postman collection for API testing
- Check browser DevTools → Application → Cookies

**Production:**
- Use environment variables for all configs
- Implement proper error boundaries
- Add request/response logging
- Monitor failed auth attempts
- Set up alerts for token reuse detection

## 📞 Support

**Issues with:**
- **API** → Check `README.md` troubleshooting section
- **Environment** → See `ENVIRONMENT-SETUP.md`
- **React** → Read `react-integration.md`
- **Vanilla JS** → Read `vanilla-js-integration.md`

## 🎓 Additional Resources

- [JWT.io](https://jwt.io) - Decode and verify JWTs
- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- [MDN Web Docs - Fetch API](https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API)

## ✅ Next Steps

1. ✅ Read `SUMMARY.md` for overview
2. ✅ Test with `test-page.html`
3. ✅ Read guide for your framework
4. ✅ Implement authentication in your app
5. ✅ Test thoroughly
6. ✅ Configure for production
7. ✅ Deploy! 🚀

---

**Need help?** All the answers are in these documentation files. Start with `SUMMARY.md` and follow the links!

**Ready to code?** Jump to `QUICKSTART.md` or your framework-specific guide!

Happy coding! 🎉
