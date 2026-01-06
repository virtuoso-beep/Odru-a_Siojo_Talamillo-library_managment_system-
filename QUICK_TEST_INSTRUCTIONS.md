# Quick Test Instructions

## Database Schema Tests

### Run Database Tests (5 minutes)

1. **Open MySQL Workbench**
2. **Connect to your MySQL server**
3. **Open the test script:**
   - Navigate to: `Library Management System/Database/005_Test_Schema_Fixes.sql`
4. **Execute the script:**
   - Click "Execute" button or press F5
5. **Review results:**
   - Look for ✅ PASS or ❌ FAIL messages
   - Check the test summary at the end

**Expected:** All tests should show ✅ PASS

---

## Service Tests

### Option 1: Run from Application (Recommended)

1. **Open the solution in Visual Studio**
2. **Add this code to DashboardForm.cs** (in InitializeDashboard method or create a test button):

```csharp
// Add a test button (optional - for easy access)
private void AddTestButton()
{
    Button btnTest = new Button
    {
        Text = "🧪 Run Tests",
        Size = new Size(100, 30),
        Location = new Point(10, 10)
    };
    btnTest.Click += (s, e) => 
    {
        TestForm testForm = new TestForm();
        testForm.ShowDialog();
    };
    // Add to your form
}
```

3. **Or call directly:**
```csharp
ServiceTests.RunAllTests();
```

### Option 2: Use Test Form

1. **In your application startup** (Program.cs or DashboardForm), add:
```csharp
// For testing only - remove in production
TestForm testForm = new TestForm();
testForm.ShowDialog();
```

2. **Or add a menu item** in DashboardForm to open TestForm

### Option 3: Command Line / Debug

Add this to any button click or form load:
```csharp
using Library_Management_System.Service;

// Run tests
string results = ServiceTests.RunTestsAndGetResults();
MessageBox.Show(results, "Test Results");
```

---

## What Gets Tested

### Database Tests (9 test categories):
✅ Borrowings table exists and has correct structure  
✅ Reservations table has new columns  
✅ Members table has new columns  
✅ Fines table has new columns  
✅ Books table has new columns  
✅ Foreign keys are properly set up  
✅ Performance indexes exist  
✅ Stored procedures are deployed  
✅ Data integrity (no orphaned records)  

### Service Tests (16 tests total):

**ReportsService (8 tests):**
- GetDailyCirculationData
- GetPopularBooks
- GetOverdueBooks
- GetMemberTypeDistribution
- GetMemberActivitySummary
- GetCollectionStatistics
- GetCollectionByCategory
- GetFineReportData

**SearchService (8 tests):**
- SearchBooks (basic)
- SearchBooks (with filters)
- SearchByTitle
- SearchByAuthor
- SearchByISBN
- GetAvailableCategories
- GetAvailablePublishers
- GetSearchResultCount

---

## Expected Results

### ✅ Success Criteria:
- **Database Tests:** All 9+ tests pass
- **Service Tests:** All 16 tests pass
- No exceptions thrown
- All methods return valid data (or empty lists if no data)

### ⚠️ If Tests Fail:

1. **Database connection issues:**
   - Check connection string
   - Verify MySQL server is running
   - Check user permissions

2. **Missing tables/columns:**
   - Re-run schema scripts
   - Check `002_Fix_Schema_Issues.sql` executed successfully

3. **Service errors:**
   - Check `error_log.txt` for details
   - Verify database has required tables
   - Check SQL queries in service files

---

## Quick Test Checklist

- [ ] Database connection works
- [ ] Run `005_Test_Schema_Fixes.sql` - all tests pass
- [ ] Run ServiceTests.RunAllTests() - all tests pass
- [ ] No exceptions in error logs
- [ ] All services return data (or empty lists)

---

**Time Required:** 10-15 minutes for complete testing

**Files Created:**
- `Database/005_Test_Schema_Fixes.sql` - Database tests
- `Service/ServiceTests.cs` - Service tests
- `Forms/TestForm.cs` - Test UI (optional)
- `TESTING_GUIDE.md` - Detailed guide

