using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Library_Management_System.Helper;

namespace Library_Management_System.Service
{
    /// <summary>
    /// Test class for verifying ReportsService and SearchService functionality
    /// Run these tests to verify services work correctly after implementation
    /// </summary>
    public static class ServiceTests
    {
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        private static List<string> _testResults = new List<string>();

        /// <summary>
        /// Run all service tests
        /// </summary>
        public static void RunAllTests()
        {
            _testsPassed = 0;
            _testsFailed = 0;
            _testResults.Clear();

            try
            {
                TestReportsService();
                TestSearchService();
                PrintTestSummary();
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "ServiceTests");
                MessageBox.Show($"Error running tests: {ex.Message}", "Test Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Test ReportsService functionality
        /// </summary>
        private static void TestReportsService()
        {
            _testResults.Add("=== REPORTS SERVICE TESTS ===");
            var service = new ReportsService();

            // Test 1: GetDailyCirculationData
            try
            {
                var data = service.GetDailyCirculationData(DateTime.Now.AddDays(-7), DateTime.Now);
                LogTestResult("GetDailyCirculationData", 
                    data != null, 
                    $"Returned {data?.Count ?? 0} data points");
            }
            catch (Exception ex)
            {
                LogTestResult("GetDailyCirculationData", false, $"Error: {ex.Message}");
            }

            // Test 2: GetPopularBooks
            try
            {
                var books = service.GetPopularBooks(10, 30);
                LogTestResult("GetPopularBooks", 
                    books != null, 
                    $"Returned {books?.Count ?? 0} books");
            }
            catch (Exception ex)
            {
                LogTestResult("GetPopularBooks", false, $"Error: {ex.Message}");
            }

            // Test 3: GetOverdueBooks
            try
            {
                var overdue = service.GetOverdueBooks();
                LogTestResult("GetOverdueBooks", 
                    overdue != null, 
                    $"Returned {overdue?.Count ?? 0} overdue books");
            }
            catch (Exception ex)
            {
                LogTestResult("GetOverdueBooks", false, $"Error: {ex.Message}");
            }

            // Test 4: GetMemberTypeDistribution
            try
            {
                var distribution = service.GetMemberTypeDistribution();
                LogTestResult("GetMemberTypeDistribution", 
                    distribution != null, 
                    $"Returned {distribution?.Count ?? 0} member types");
            }
            catch (Exception ex)
            {
                LogTestResult("GetMemberTypeDistribution", false, $"Error: {ex.Message}");
            }

            // Test 5: GetMemberActivitySummary
            try
            {
                var summary = service.GetMemberActivitySummary();
                LogTestResult("GetMemberActivitySummary", 
                    summary != null, 
                    $"Summary retrieved - Total Members: {summary?.TotalMembers ?? 0}");
            }
            catch (Exception ex)
            {
                LogTestResult("GetMemberActivitySummary", false, $"Error: {ex.Message}");
            }

            // Test 6: GetCollectionStatistics
            try
            {
                var stats = service.GetCollectionStatistics();
                LogTestResult("GetCollectionStatistics", 
                    stats != null, 
                    $"Stats retrieved - Total Books: {stats?.TotalBooks ?? 0}");
            }
            catch (Exception ex)
            {
                LogTestResult("GetCollectionStatistics", false, $"Error: {ex.Message}");
            }

            // Test 7: GetCollectionByCategory
            try
            {
                var byCategory = service.GetCollectionByCategory();
                LogTestResult("GetCollectionByCategory", 
                    byCategory != null, 
                    $"Returned {byCategory?.Count ?? 0} categories");
            }
            catch (Exception ex)
            {
                LogTestResult("GetCollectionByCategory", false, $"Error: {ex.Message}");
            }

            // Test 8: GetFineReportData
            try
            {
                var fineReport = service.GetFineReportData();
                LogTestResult("GetFineReportData", 
                    fineReport != null, 
                    $"Fine report retrieved - Total Unpaid: ${fineReport?.TotalUnpaidFines ?? 0:F2}");
            }
            catch (Exception ex)
            {
                LogTestResult("GetFineReportData", false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Test SearchService functionality
        /// </summary>
        private static void TestSearchService()
        {
            _testResults.Add("=== SEARCH SERVICE TESTS ===");
            var service = new SearchService();

            // Test 1: SearchBooks (basic search)
            try
            {
                var results = service.SearchBooks("test");
                LogTestResult("SearchBooks (Basic)", 
                    results != null, 
                    $"Returned {results?.Count ?? 0} results");
            }
            catch (Exception ex)
            {
                LogTestResult("SearchBooks (Basic)", false, $"Error: {ex.Message}");
            }

            // Test 2: SearchBooks (with filters)
            try
            {
                var filters = new SearchService.SearchFilters
                {
                    AvailableOnly = true,
                    Category = "All Categories"
                };
                var results = service.SearchBooks("", filters);
                LogTestResult("SearchBooks (With Filters)", 
                    results != null, 
                    $"Returned {results?.Count ?? 0} available books");
            }
            catch (Exception ex)
            {
                LogTestResult("SearchBooks (With Filters)", false, $"Error: {ex.Message}");
            }

            // Test 3: SearchByTitle
            try
            {
                var results = service.SearchByTitle("test");
                LogTestResult("SearchByTitle", 
                    results != null, 
                    $"Returned {results?.Count ?? 0} results");
            }
            catch (Exception ex)
            {
                LogTestResult("SearchByTitle", false, $"Error: {ex.Message}");
            }

            // Test 4: SearchByAuthor
            try
            {
                var results = service.SearchByAuthor("test");
                LogTestResult("SearchByAuthor", 
                    results != null, 
                    $"Returned {results?.Count ?? 0} results");
            }
            catch (Exception ex)
            {
                LogTestResult("SearchByAuthor", false, $"Error: {ex.Message}");
            }

            // Test 5: SearchByISBN
            try
            {
                var results = service.SearchByISBN("1234567890");
                LogTestResult("SearchByISBN", 
                    results != null, 
                    $"Returned {results?.Count ?? 0} results");
            }
            catch (Exception ex)
            {
                LogTestResult("SearchByISBN", false, $"Error: {ex.Message}");
            }

            // Test 6: GetAvailableCategories
            try
            {
                var categories = service.GetAvailableCategories();
                LogTestResult("GetAvailableCategories", 
                    categories != null, 
                    $"Returned {categories?.Count ?? 0} categories");
            }
            catch (Exception ex)
            {
                LogTestResult("GetAvailableCategories", false, $"Error: {ex.Message}");
            }

            // Test 7: GetAvailablePublishers
            try
            {
                var publishers = service.GetAvailablePublishers();
                LogTestResult("GetAvailablePublishers", 
                    publishers != null, 
                    $"Returned {publishers?.Count ?? 0} publishers");
            }
            catch (Exception ex)
            {
                LogTestResult("GetAvailablePublishers", false, $"Error: {ex.Message}");
            }

            // Test 8: GetSearchResultCount
            try
            {
                var count = service.GetSearchResultCount("test");
                LogTestResult("GetSearchResultCount", 
                    count >= 0, 
                    $"Returned count: {count}");
            }
            catch (Exception ex)
            {
                LogTestResult("GetSearchResultCount", false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Log test result
        /// </summary>
        private static void LogTestResult(string testName, bool passed, string details)
        {
            if (passed)
            {
                _testsPassed++;
                _testResults.Add($"✅ PASS: {testName} - {details}");
            }
            else
            {
                _testsFailed++;
                _testResults.Add($"❌ FAIL: {testName} - {details}");
            }
        }

        /// <summary>
        /// Print test summary
        /// </summary>
        private static void PrintTestSummary()
        {
            _testResults.Add("");
            _testResults.Add("=== TEST SUMMARY ===");
            _testResults.Add($"Total Tests: {_testsPassed + _testsFailed}");
            _testResults.Add($"Passed: {_testsPassed}");
            _testResults.Add($"Failed: {_testsFailed}");
            _testResults.Add("");
            
            if (_testsFailed == 0)
            {
                _testResults.Add("✅ ALL TESTS PASSED!");
            }
            else
            {
                _testResults.Add($"⚠️ {_testsFailed} TEST(S) FAILED");
            }

            // Display results
            string resultText = string.Join(Environment.NewLine, _testResults);
            
            // Write to console/debug output
            System.Diagnostics.Debug.WriteLine(resultText);
            
            // Show message box
            MessageBox.Show(resultText, 
                "Service Tests Results", 
                MessageBoxButtons.OK, 
                _testsFailed == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Run tests and return results as string
        /// </summary>
        public static string RunTestsAndGetResults()
        {
            _testsPassed = 0;
            _testsFailed = 0;
            _testResults.Clear();

            try
            {
                TestReportsService();
                TestSearchService();
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "ServiceTests");
                _testResults.Add($"ERROR: {ex.Message}");
            }

            _testResults.Add("");
            _testResults.Add("=== TEST SUMMARY ===");
            _testResults.Add($"Total: {_testsPassed + _testsFailed} | Passed: {_testsPassed} | Failed: {_testsFailed}");

            return string.Join(Environment.NewLine, _testResults);
        }
    }
}

