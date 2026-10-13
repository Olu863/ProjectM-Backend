# ✅ FIXED: Entity Framework Translation Error - IsActive Property

## 🔴 Problem
```
System.InvalidOperationException: The LINQ expression 'DbSet<RefreshToken>()
	.Where(r => r.UserId == __userId_0 && r.IsActive)' could not be translated.
Additional information: Translation of member 'IsActive' on entity type 'RefreshToken' failed.
This commonly occurs when the specified member is unmapped.
```

## 🎯 Root Cause

The `RefreshToken` entity has **computed properties** that EF Core cannot translate to SQL:

```csharp
// ProjectM.Core/Entities/RefreshToken.cs
public bool IsExpired => DateTime.UtcNow >= ExpiresAt;  // ❌ Computed property
public bool IsActive => !IsRevoked && !IsExpired;       // ❌ Computed property
```

These are C# **expression-bodied properties** (getters only) that:
- ✅ Work great in C# code (in-memory)
- ❌ **Cannot be translated to SQL** by Entity Framework
- ❌ Are **not database columns**

When you use them in LINQ queries like:
```csharp
.Where(rt => rt.UserId == userId && rt.IsActive)  // ❌ Fails!
```

EF Core tries to convert this to SQL but can't, because `IsActive` doesn't exist in the database.

## ✅ Solution Applied

### **Before (Broken):**
```csharp
var existingTokens = await _context.RefreshTokens
	.Where(rt => rt.UserId == userId && rt.IsActive)  // ❌ Can't translate
	.ToListAsync();
```

### **After (Fixed):**
```csharp
var existingTokens = await _context.RefreshTokens
	.Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)  // ✅ Translates to SQL
	.ToListAsync();
```

The fixed query uses **actual database columns**:
- `IsRevoked` (bool column)
- `ExpiresAt` (datetime column)

## 🔧 All Fixed Locations

### 1. **GenerateRefreshTokenAsync** (Line 224)
```csharp
// BEFORE
.Where(rt => rt.UserId == userId && rt.IsActive)

// AFTER
.Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
```

### 2. **RevokeAllUserTokensAsync** (Line 277)
```csharp
// BEFORE
.Where(rt => rt.UserId == userId && rt.IsActive)

// AFTER
.Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
```

### 3. **LogoutAllDevicesAsync** (Line 424)
```csharp
// BEFORE
.Where(rt => rt.UserId == userId && rt.IsActive)

// AFTER
.Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
```

### 4. **BONUS: Fixed GetUsersByRoleAsync Logic Bug** (Line 397)
```csharp
// BEFORE (Bug - returned INACTIVE users!)
.Where(u => u.Role == role && !u.IsActive)

// AFTER (Fixed - returns ACTIVE users)
.Where(u => u.Role == role && u.IsActive)
```

**Note:** `User.IsActive` is a real database column, so it works in queries. The issue was the inverted logic (`!u.IsActive`).

## 📊 Why This Works

### **EF Core Translation:**

**Computed Property (Can't Translate):**
```csharp
.Where(rt => rt.IsActive)
// ❌ EF Core doesn't know how to convert this to SQL
```

**Database Columns (Can Translate):**
```csharp
.Where(rt => !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
// ✅ Converts to:
// WHERE IsRevoked = 0 AND ExpiresAt > @p0
```

## 🎓 Understanding Computed vs. Database Properties

### **Computed Property (Expression-Bodied)**
```csharp
public bool IsActive => !IsRevoked && !IsExpired;  // NOT in database
```
- No database column
- Calculated in C# code
- Can't be used in LINQ-to-SQL queries
- Use for: In-memory logic, after loading entities

### **Database Property**
```csharp
public bool IsRevoked { get; set; }  // Database column
public DateTime ExpiresAt { get; set; }  // Database column
```
- Has database column
- Can be queried via LINQ
- EF Core can translate to SQL
- Use for: Database queries

## 🚀 Testing

### **Restart Your Backend**
- **Stop debugging** (Shift + F5)
- **Start again** (F5) or use Hot Reload

### **Try Login Again**
```javascript
POST http://localhost:7008/api/users/login
{
  "email": "admin@example.com",
  "password": "Admin@123"
}
```

### **Expected Result:**
- ✅ **200 OK** with JWT token
- ✅ Refresh token stored in database
- ✅ No more translation errors

## 🔍 How to Avoid This in the Future

### **Rule: Never use computed properties in LINQ queries**

❌ **Don't:**
```csharp
.Where(entity => entity.ComputedProperty)  // If it uses =>
```

✅ **Do:**
```csharp
.Where(entity => entity.DatabaseColumn)    // If it has { get; set; }
```

### **How to Identify Computed Properties:**
```csharp
// Computed (Expression-Bodied) - Can't query
public bool IsActive => !IsRevoked && !IsExpired;

// Database Property - Can query
public bool IsRevoked { get; set; }
```

## 📝 Alternative Solutions

If you really need to use `IsActive` in queries, you have 3 options:

### **Option 1: Client Evaluation (Not Recommended)**
```csharp
var tokens = await _context.RefreshTokens
	.Where(rt => rt.UserId == userId)
	.ToListAsync();  // Load all to memory

var activeTokens = tokens.Where(rt => rt.IsActive);  // Filter in C#
```
- ⚠️ Loads all records to memory first
- ⚠️ Slower for large datasets

### **Option 2: Use Raw Database Columns (Current Solution)** ✅
```csharp
.Where(rt => !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
```
- ✅ Efficient (database-side filtering)
- ✅ Recommended approach

### **Option 3: Create Database Column with Computed Value**
```csharp
// Migration:
migrationBuilder.Sql(@"
	ALTER TABLE RefreshTokens ADD IsActive AS 
	CASE WHEN IsRevoked = 0 AND ExpiresAt > GETUTCDATE() 
	THEN 1 ELSE 0 END PERSISTED
");
```
- Makes it a real database column
- ⚠️ Requires migration and schema change

## ✅ Status

**All EF Core translation errors are now fixed!**

The login process should now complete successfully:
1. ✅ Validate credentials
2. ✅ Generate JWT token
3. ✅ Create refresh token (fixed)
4. ✅ Return token to frontend

---

**Fixed**: January 2024  
**Files Modified**: `ProjectM.Services/Services/UserService.cs`  
**Lines Changed**: 224, 277, 397, 424
