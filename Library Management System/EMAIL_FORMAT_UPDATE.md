# ✅ Email Format Updated

## Educational Email Format Enforced

The system now **only accepts** educational emails from **University of Mindanao** with the following format:

### Format
```
firstname.lastname.IDnumber.tc@umindanao.edu.ph
```

### Example
```
t.odruna.142275.tc@umindanao.edu.ph
```

---

## ✅ What Was Updated

### 1. **User Model** (`Models/User.cs`)
- ✅ Email validation now enforces educational email format
- ✅ Validates: `firstname.lastname.IDnumber.tc@umindanao.edu.ph`
- ✅ Uses regex pattern matching

### 2. **SignIn Form** (`Forms/SiginForm.cs`)
- ✅ Added educational email validation
- ✅ Shows helpful error message with format example
- ✅ Validates before authentication

### 3. **Database Script** (`Database Scripts/CreateUsersTable_MySQL.sql`)
- ✅ Updated test accounts to use educational email format:
  - **Administrator**: `a.dmin.123456.tc@umindanao.edu.ph` / `admin123`
  - **Staff**: `s.taff.234567.tc@umindanao.edu.ph` / `staff123`
  - **Member**: `t.odruna.142275.tc@umindanao.edu.ph` / `member123`

---

## 📋 Email Format Rules

1. **Must end with**: `@umindanao.edu.ph`
2. **Format before @**: `firstname.lastname.IDnumber.tc`
3. **Pattern**: 
   - `firstname` - lowercase letters
   - `lastname` - lowercase letters  
   - `IDnumber` - digits only
   - `.tc` - literal ".tc"
4. **Example**: `t.odruna.142275.tc@umindanao.edu.ph`

---

## 🔄 Next Steps

1. **Update Database** (if already created):
   - Run the updated `CreateUsersTable_MySQL.sql` script
   - Or manually update existing test accounts with new email format

2. **Test the Validation**:
   - Try entering invalid email → Should show error
   - Try entering valid format → Should accept

3. **Use New Test Accounts**:
   - Administrator: `a.dmin.123456.tc@umindanao.edu.ph` / `admin123`
   - Staff: `s.taff.234567.tc@umindanao.edu.ph` / `staff123`
   - Member: `t.odruna.142275.tc@umindanao.edu.ph` / `member123`

---

## ✅ Validation Features

- ✅ **Real-time validation** in SignIn form
- ✅ **Model-level validation** in User class
- ✅ **Clear error messages** with format example
- ✅ **Case-insensitive** (converts to lowercase)

---

**All email validation now enforces the educational email format!** 🎓

