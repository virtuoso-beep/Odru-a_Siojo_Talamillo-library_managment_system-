# Testing Guide - Database Schema & Services

This guide explains how to test database schema fixes and verify service implementations.

---

## Part 1: Database Schema Tests

### Prerequisites
1. MySQL Server running
2. Database `LibraryManagementDB` created
3. All schema scripts executed:
   - `001_Create_Database_Schema.sql`
   - `002_Fix_Schema_Issues.sql`
   - `003_Add_Performance_Indexes.sql` (optional)
4. All stored procedures deployed

### Running Database Tests

#### Method 1: MySQL Workbench
1. Open MySQL Workbench
2. Connect to your MySQL server
3. Open `Database/005_Test_Schema_Fixes.sql`
4. Execute the entire script (Execute → Execute All or F5)
5. Review the test results in the output

#### Method 2: Command Line
```bash
mysql -u your_username -p LibraryManagementDB < "Library Management System/Database/005_Test_Schema_Fixes.sql"
```

### What the Tests Verify

#### ✅ Test 1: Borrowings Table
- Verifies `Borrowings` table exists
- Checks table structure (columns)
- Verifies required columns: BorrowingId, MemberId, BookId

#### ✅ Test 2: Reservations Table Columns
- Verifies `ReservedDate` column exists
- Verifies `ExpiryDate` column exists

#### ✅ Test 3: Members Table Columns
- Verifies `MembershipExpiryDate` column exists
- Verifies `EmergencyContactName` column exists
- Verifies `EmergencyContactPhone` column exists

#### ✅ Test 4: Fines Table Columns
- Verifies `BorrowingId` column exists
- Verifies `Reason` column exists
- Verifies `CreatedDate` column exists

#### ✅ Test 5: Books Table Columns
- Verifies `UpdatedDate` column exists
- Verifies `Category` column exists

#### ✅ Test 6: Foreign Key Relationships
- Verifies Borrowings → Members foreign key
- Verifies Borrowings → Books foreign key
- Verifies Fines → Borrowings foreign key

#### ✅ Test 7: Performance Indexes
- Verifies indexes exist on Borrowings table
- Verifies indexes exist on Fines table

#### ✅ Test 8: Stored Procedures
- Verifies circulation-related procedures exist
- Verifies fines-related procedures exist
- Verifies dashboard-related procedures exist

#### ✅ Test 9: Data Integrity
- Checks for orphaned records in Borrowings table
- Verifies foreign key relationships are maintained

### Expected Results

**All tests should pass** if:
- Schema scripts were run successfully
- Stored procedures were deployed
- No manual database modifications broke relationships

**If tests fail:**
- Review the error messages
- Check which specific test failed
- Re-run the corresponding schema fix script
- Verify stored procedures are deployed

---

## Part 2: Service Tests

### Prerequisites
1. Database connection configured correctly
2. Application can connect to database
3. Some test data in database (optional, but recommended)

### Running Service Tests

#### Method 1: From Application Code
Add this to your application startup or create a test form:

```csharp
using Library_Management_System.Service;

// In your form or startup code
private void RunServiceTests()
{
    ServiceTests.RunAllTests();
}
```

#### Method 2: Create Test Form
Create a simple test form with a button:

```csharp
private void btnRunTests_Click(object sender, EventArgs e)
{
    ServiceTests.RunAllTests();
}
```

#### Method 3: Programmatic Access
```csharp
string results = ServiceTests.RunTestsAndGetResults();
MessageBox.Show(results, "Test Results");
```

### What the Tests Verify

#### ReportsService Tests

1. **GetDailyCirculationData**
   - Tests retrieval of daily circulation statistics
   - Verifies method doesn't throw exceptions
   - Checks return value is not null

2. **GetPopularBooks**
   - Tests retrieval of popular books
   - Verifies method handles parameters correctly

3. **GetOverdueBooks**
   - Tests retrieval of overdue books
   - Verifies method returns list

4. **GetMemberTypeDistribution**
   - Tests member type statistics
   - Verifies data structure

5. **GetMemberActivitySummary**
   - Tests member activity statistics
   - Verifies summary data

6. **GetCollectionStatistics**
   - Tests collection statistics
   - Verifies totals are calculated

7. **GetCollectionByCategory**
   - Tests category breakdown
   - Verifies grouping works

8. **GetFineReportData**
   - Tests fine report generation
   - Verifies financial calculations

#### SearchService Tests

1. **SearchBooks (Basic)**
   - Tests basic search functionality
   - Verifies search term handling

2. **SearchBooks (With Filters)**
   - Tests filtered search
   - Verifies filter application

3. **SearchByTitle**
   - Tests title-specific search
   - Verifies exact matching

4. **SearchByAuthor**
   - Tests author search
   - Verifies author matching

5. **SearchByISBN**
   - Tests ISBN search
   - Verifies ISBN lookup

6. **GetAvailableCategories**
   - Tests category retrieval
   - Verifies category list

7. **GetAvailablePublishers**
   - Tests publisher retrieval
   - Verifies publisher list

8. **GetSearchResultCount**
   - Tests result counting
   - Verifies count accuracy

### Expected Results

**All tests should pass** if:
- Database connection is working
- Services are properly implemented
- Database has required tables

**If tests fail:**
- Check database connection string
- Verify database tables exist
- Check error logs in `error_log.txt`
- Review service implementation

---

## Part 3: Integration Testing

### Manual Testing Checklist

#### Database Integration
- [ ] Can connect to database
- [ ] Can query Borrowings table
- [ ] Can query all modified tables
- [ ] Foreign keys work correctly
- [ ] Stored procedures execute without errors

#### ReportsService Integration
- [ ] Reports view loads without errors
- [ ] Charts display data
- [ ] Export functionality works
- [ ] All report types generate correctly

#### SearchService Integration
- [ ] Search view loads
- [ ] Search returns results
- [ ] Filters work correctly
- [ ] Search results display properly

---

## Troubleshooting

### Database Tests Fail

**Issue:** "Table doesn't exist"
- **Solution:** Run `001_Create_Database_Schema.sql` first

**Issue:** "Column doesn't exist"
- **Solution:** Run `002_Fix_Schema_Issues.sql`

**Issue:** "Foreign key constraint fails"
- **Solution:** Check data integrity, verify foreign keys exist

### Service Tests Fail

**Issue:** "Connection string error"
- **Solution:** Check `App.config` or `MYSqlHelper.cs` connection string

**Issue:** "Method returns null"
- **Solution:** Check if database has data, verify SQL queries

**Issue:** "Exception thrown"
- **Solution:** Check `error_log.txt` for details, verify database structure

---

## Test Results Interpretation

### ✅ All Tests Pass
- Database schema is correct
- Services are working properly
- System is ready for use

### ⚠️ Some Tests Fail
- Review specific failures
- Check error messages
- Fix issues and re-run tests

### ❌ Many Tests Fail
- Verify database setup
- Check connection string
- Review schema scripts execution
- Verify stored procedures deployment

---

## Next Steps After Testing

1. **If all tests pass:**
   - System is ready for deployment
   - Proceed with user acceptance testing

2. **If tests fail:**
   - Fix identified issues
   - Re-run tests
   - Document any known limitations

3. **Document results:**
   - Keep test results for reference
   - Update project documentation
   - Note any configuration requirements

---

**Last Updated:** Current Date  
**Test Scripts:** 
- Database: `Database/005_Test_Schema_Fixes.sql`
- Services: `Service/ServiceTests.cs`

