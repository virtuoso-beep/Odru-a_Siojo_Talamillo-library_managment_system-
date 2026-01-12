using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using LMS_Library_Management_System.Helper;
using LMS_Library_Management_System.Interfaces;
using LMS_Library_Management_System.Models;
using System.Linq;

namespace LMS_Library_Management_System.Service
{
    /// <summary>
    /// Service class for managing library books
    /// Handles CRUD operations for Book catalog
    /// </summary>
    public class BookService : IBookService
    {
        /// <summary>
        /// Ensures book stored procedures exist, creates them if they don't
        /// </summary>
        private void EnsureStoredProceduresExist(MySqlConnection connection)
        {
            try
            {
                // Always explicitly set the database to ensure we're working with the correct database
                using (var useDbCmd = new MySqlCommand("USE LMS_DB", connection))
                {
                    useDbCmd.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine("EnsureStoredProceduresExist: Set to LMS_DB database");
                }

                // Check if critical stored procedures exist (sp_AddBook, sp_UpdateBook, sp_DeleteBook, sp_AddCopies)
                bool needsCreation = false;
                string[] criticalProcedures = { "sp_AddBook", "sp_UpdateBook", "sp_DeleteBook", "sp_AddCopies" };
                
                foreach (string procName in criticalProcedures)
                {
                    using (var checkCommand = new MySqlCommand(
                        "SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = 'LMS_DB' AND routine_name = @ProcName",
                        connection))
                    {
                        checkCommand.Parameters.AddWithValue("@ProcName", procName);
                        int exists = Convert.ToInt32(checkCommand.ExecuteScalar());
                        if (exists == 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"EnsureStoredProceduresExist: {procName} not found");
                            needsCreation = true;
                            break;
                        }
                    }
                }

                if (needsCreation)
                {
                    System.Diagnostics.Debug.WriteLine("EnsureStoredProceduresExist: Critical stored procedures not found, creating them...");
                    // Create stored procedures
                    CreateBookStoredProcedures(connection);
                    System.Diagnostics.Debug.WriteLine("EnsureStoredProceduresExist: Stored procedures created successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("EnsureStoredProceduresExist: All critical stored procedures already exist");
                }
            }
            catch (MySqlException mysqlEx)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureStoredProceduresExist MySQL Error: {mysqlEx.Number} - {mysqlEx.Message}");
                // Try to create stored procedures anyway
                try
                {
                    CreateBookStoredProcedures(connection);
                }
                catch (Exception createEx)
                {
                    System.Diagnostics.Debug.WriteLine($"EnsureStoredProceduresExist: Failed to create stored procedures: {createEx.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureStoredProceduresExist Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"EnsureStoredProceduresExist StackTrace: {ex.StackTrace}");
                // Try to create stored procedures anyway
                try
                {
                    CreateBookStoredProcedures(connection);
                }
                catch (Exception createEx)
                {
                    System.Diagnostics.Debug.WriteLine($"EnsureStoredProceduresExist: Failed to create stored procedures: {createEx.Message}");
                }
            }
        }

        /// <summary>
        /// Creates all book-related stored procedures
        /// </summary>
        private void CreateBookStoredProcedures(MySqlConnection connection)
        {
            try
            {
                // Always ensure we're using the correct database
                string currentDb = connection.Database;
                System.Diagnostics.Debug.WriteLine($"CreateBookStoredProcedures: Current database: {currentDb}");
                
                // Always explicitly set the database to ensure stored procedures are created in the right place
                using (var useDbCmd = new MySqlCommand("USE LMS_DB", connection))
                {
                    useDbCmd.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine("CreateBookStoredProcedures: Set to LMS_DB database");
                }
                // sp_AddBook
                string spAddBook = @"
                    DROP PROCEDURE IF EXISTS sp_AddBook;
                    CREATE PROCEDURE sp_AddBook(
                        IN p_ISBN VARCHAR(20),
                        IN p_Title VARCHAR(255),
                        IN p_Author VARCHAR(255),
                        IN p_Publisher VARCHAR(255),
                        IN p_PublicationYear INT,
                        IN p_Category VARCHAR(100),
                        IN p_TotalCopies INT,
                        IN p_Description TEXT
                    )
                    BEGIN
                        INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Category, TotalCopies, AvailableCopies, Description, CreatedDate)
                        VALUES (p_ISBN, p_Title, p_Author, NULLIF(p_Publisher, ''), p_PublicationYear, p_Category, p_TotalCopies, p_TotalCopies, NULLIF(p_Description, ''), NOW());
                        SELECT LAST_INSERT_ID() AS BookId;
                    END";

                // sp_GetAllBooks
                string spGetAllBooks = @"
                    DROP PROCEDURE IF EXISTS sp_GetAllBooks;
                    CREATE PROCEDURE sp_GetAllBooks()
                    BEGIN
                        SELECT BookId, ISBN, Title, Author, Publisher, PublicationYear, Category, TotalCopies, AvailableCopies, Description, CreatedDate
                        FROM Books ORDER BY CreatedDate DESC;
                    END";

                // sp_GetBookById
                string spGetBookById = @"
                    DROP PROCEDURE IF EXISTS sp_GetBookById;
                    CREATE PROCEDURE sp_GetBookById(IN p_BookId INT)
                    BEGIN
                        SELECT BookId, ISBN, Title, Author, Publisher, PublicationYear, Category, TotalCopies, AvailableCopies, Description, CreatedDate
                        FROM Books WHERE BookId = p_BookId;
                    END";

                // sp_SearchBooks
                string spSearchBooks = @"
                    DROP PROCEDURE IF EXISTS sp_SearchBooks;
                    CREATE PROCEDURE sp_SearchBooks(IN p_SearchTerm VARCHAR(255), IN p_Category VARCHAR(100))
                    BEGIN
                        SELECT BookId, ISBN, Title, Author, Publisher, PublicationYear, Category, TotalCopies, AvailableCopies, Description, CreatedDate
                        FROM Books
                        WHERE (p_SearchTerm IS NULL OR p_SearchTerm = '' OR Title LIKE CONCAT('%', p_SearchTerm, '%') OR Author LIKE CONCAT('%', p_SearchTerm, '%') OR ISBN LIKE CONCAT('%', p_SearchTerm, '%'))
                        AND (p_Category IS NULL OR p_Category = '' OR Category = p_Category)
                        ORDER BY CreatedDate DESC;
                    END";

                // sp_UpdateBook
                string spUpdateBook = @"
                    DROP PROCEDURE IF EXISTS sp_UpdateBook;
                    CREATE PROCEDURE sp_UpdateBook(IN p_BookId INT, IN p_ISBN VARCHAR(20), IN p_Title VARCHAR(255), IN p_Author VARCHAR(255), IN p_Publisher VARCHAR(255), IN p_PublicationYear INT, IN p_Category VARCHAR(100), IN p_TotalCopies INT, IN p_AvailableCopies INT, IN p_Description TEXT)
                    BEGIN
                        UPDATE Books SET ISBN = p_ISBN, Title = p_Title, Author = p_Author, Publisher = NULLIF(p_Publisher, ''), PublicationYear = p_PublicationYear, Category = p_Category, TotalCopies = p_TotalCopies, AvailableCopies = p_AvailableCopies, Description = NULLIF(p_Description, '')
                        WHERE BookId = p_BookId;
                        SELECT ROW_COUNT() AS RowsAffected;
                    END";

                // sp_DeleteBook - with SQL SECURITY DEFINER to ensure proper permissions
                // Note: ExecuteNonQuery handles multi-line SQL correctly, but we need to ensure proper formatting
                string spDeleteBook = @"DROP PROCEDURE IF EXISTS sp_DeleteBook;
CREATE PROCEDURE sp_DeleteBook(IN p_BookId INT)
SQL SECURITY DEFINER
BEGIN
    DECLARE v_ActiveBorrowings INT;
    DECLARE v_TotalBorrowings INT;
    DECLARE v_ErrorMessage VARCHAR(500);
    
    -- Check for active borrowings
    SELECT COUNT(*) INTO v_ActiveBorrowings
    FROM Borrowings 
    WHERE BookId = p_BookId AND ReturnDate IS NULL;
    
    IF v_ActiveBorrowings > 0 THEN
        SET v_ErrorMessage = CONCAT('Cannot delete book: There are ', v_ActiveBorrowings, ' active borrowing(s) for this book.');
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_ErrorMessage;
    END IF;
    
    -- Check for any borrowings (to maintain history)
    SELECT COUNT(*) INTO v_TotalBorrowings
    FROM Borrowings 
    WHERE BookId = p_BookId;
    
    IF v_TotalBorrowings > 0 THEN
        SET v_ErrorMessage = CONCAT('Cannot delete book: This book has borrowing history (', v_TotalBorrowings, ' record(s)).');
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_ErrorMessage;
    END IF;
    
    DELETE FROM Books WHERE BookId = p_BookId;
    
    SELECT ROW_COUNT() AS RowsAffected;
END";

                // sp_AddCopies - with SQL SECURITY DEFINER to ensure proper permissions
                // Note: ExecuteNonQuery handles multi-line SQL correctly, but we need to ensure proper formatting
                string spAddCopies = @"DROP PROCEDURE IF EXISTS sp_AddCopies;
CREATE PROCEDURE sp_AddCopies(IN p_BookId INT, IN p_CopiesToAdd INT)
SQL SECURITY DEFINER
BEGIN
    UPDATE Books 
    SET TotalCopies = TotalCopies + p_CopiesToAdd, 
        AvailableCopies = AvailableCopies + p_CopiesToAdd 
    WHERE BookId = p_BookId;
    SELECT ROW_COUNT() AS RowsAffected;
END";

                // sp_GetAllCategories
                string spGetAllCategories = @"
                    DROP PROCEDURE IF EXISTS sp_GetAllCategories;
                    CREATE PROCEDURE sp_GetAllCategories()
                    BEGIN
                        SELECT DISTINCT Category FROM Books WHERE Category IS NOT NULL AND Category != '' ORDER BY Category;
                    END";

                // sp_GetBookStatistics
                string spGetBookStatistics = @"
                    DROP PROCEDURE IF EXISTS sp_GetBookStatistics;
                    CREATE PROCEDURE sp_GetBookStatistics()
                    BEGIN
                        SELECT 
                            COUNT(*) AS TotalTitles,
                            COALESCE(SUM(TotalCopies), 0) AS TotalCopies,
                            COALESCE(SUM(AvailableCopies), 0) AS AvailableCopies,
                            COUNT(DISTINCT Category) AS Categories
                        FROM Books;
                    END";

                // Execute all procedures with error handling
                try
                {
                    using (var cmd = new MySqlCommand(spAddBook, connection)) { cmd.ExecuteNonQuery(); System.Diagnostics.Debug.WriteLine("Created sp_AddBook"); }
                    using (var cmd = new MySqlCommand(spGetAllBooks, connection)) { cmd.ExecuteNonQuery(); System.Diagnostics.Debug.WriteLine("Created sp_GetAllBooks"); }
                    using (var cmd = new MySqlCommand(spGetBookById, connection)) { cmd.ExecuteNonQuery(); System.Diagnostics.Debug.WriteLine("Created sp_GetBookById"); }
                    using (var cmd = new MySqlCommand(spSearchBooks, connection)) { cmd.ExecuteNonQuery(); System.Diagnostics.Debug.WriteLine("Created sp_SearchBooks"); }
                    using (var cmd = new MySqlCommand(spUpdateBook, connection)) { cmd.ExecuteNonQuery(); System.Diagnostics.Debug.WriteLine("Created sp_UpdateBook"); }
                    // Most important - sp_DeleteBook - create with detailed error handling
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("CreateBookStoredProcedures: Attempting to create sp_DeleteBook...");
                        using (var cmd = new MySqlCommand(spDeleteBook, connection)) 
                        { 
                            cmd.ExecuteNonQuery(); 
                            System.Diagnostics.Debug.WriteLine("CreateBookStoredProcedures: Created sp_DeleteBook successfully"); 
                        }
                    }
                    catch (MySqlException mysqlEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"CreateBookStoredProcedures: MySQL Error creating sp_DeleteBook: {mysqlEx.Number} - {mysqlEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"CreateBookStoredProcedures: SQL State: {mysqlEx.SqlState}");
                        // If SQL SECURITY DEFINER fails, try without it
                        if (mysqlEx.Number == 1044 || mysqlEx.Number == 1227 || mysqlEx.Number == 1370) // Access denied or insufficient privileges
                        {
                            System.Diagnostics.Debug.WriteLine("CreateBookStoredProcedures: Permission error with SQL SECURITY DEFINER, trying without it...");
                            try
                            {
                                string spDeleteBookNoDefiner = @"DROP PROCEDURE IF EXISTS sp_DeleteBook;
CREATE PROCEDURE sp_DeleteBook(IN p_BookId INT)
BEGIN
    DECLARE v_ActiveBorrowings INT;
    DECLARE v_TotalBorrowings INT;
    DECLARE v_ErrorMessage VARCHAR(500);
    
    -- Check for active borrowings
    SELECT COUNT(*) INTO v_ActiveBorrowings
    FROM Borrowings 
    WHERE BookId = p_BookId AND ReturnDate IS NULL;
    
    IF v_ActiveBorrowings > 0 THEN
        SET v_ErrorMessage = CONCAT('Cannot delete book: There are ', v_ActiveBorrowings, ' active borrowing(s) for this book.');
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_ErrorMessage;
    END IF;
    
    -- Check for any borrowings (to maintain history)
    SELECT COUNT(*) INTO v_TotalBorrowings
    FROM Borrowings 
    WHERE BookId = p_BookId;
    
    IF v_TotalBorrowings > 0 THEN
        SET v_ErrorMessage = CONCAT('Cannot delete book: This book has borrowing history (', v_TotalBorrowings, ' record(s)).');
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_ErrorMessage;
    END IF;
    
    DELETE FROM Books WHERE BookId = p_BookId;
    
    SELECT ROW_COUNT() AS RowsAffected;
END";
                                using (var cmd = new MySqlCommand(spDeleteBookNoDefiner, connection)) 
                                { 
                                    cmd.ExecuteNonQuery(); 
                                    System.Diagnostics.Debug.WriteLine("CreateBookStoredProcedures: Created sp_DeleteBook without SQL SECURITY DEFINER successfully"); 
                                }
                            }
                            catch (Exception fallbackEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"CreateBookStoredProcedures: Failed to create sp_DeleteBook even without DEFINER: {fallbackEx.Message}");
                                throw new InvalidOperationException($"Failed to create sp_DeleteBook stored procedure. MySQL Error {mysqlEx.Number}: {mysqlEx.Message}. Please check database permissions.", mysqlEx);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Failed to create sp_DeleteBook stored procedure. MySQL Error {mysqlEx.Number}: {mysqlEx.Message}", mysqlEx);
                        }
                    }
                    catch (Exception ex) 
                    { 
                        System.Diagnostics.Debug.WriteLine($"CreateBookStoredProcedures: Error creating sp_DeleteBook: {ex.Message}");
                        throw new InvalidOperationException($"Failed to create sp_DeleteBook stored procedure: {ex.Message}", ex);
                    }
                    // sp_AddCopies - create with detailed error handling
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("CreateBookStoredProcedures: Attempting to create sp_AddCopies...");
                        using (var cmd = new MySqlCommand(spAddCopies, connection)) 
                        { 
                            cmd.ExecuteNonQuery(); 
                            System.Diagnostics.Debug.WriteLine("CreateBookStoredProcedures: Created sp_AddCopies successfully"); 
                        }
                    }
                    catch (MySqlException mysqlEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"CreateBookStoredProcedures: MySQL Error creating sp_AddCopies: {mysqlEx.Number} - {mysqlEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"CreateBookStoredProcedures: SQL State: {mysqlEx.SqlState}");
                        // If SQL SECURITY DEFINER fails, try without it
                        if (mysqlEx.Number == 1044 || mysqlEx.Number == 1227 || mysqlEx.Number == 1370) // Access denied or insufficient privileges
                        {
                            System.Diagnostics.Debug.WriteLine("CreateBookStoredProcedures: Permission error with SQL SECURITY DEFINER, trying without it...");
                            try
                            {
                                string spAddCopiesNoDefiner = @"DROP PROCEDURE IF EXISTS sp_AddCopies;
CREATE PROCEDURE sp_AddCopies(IN p_BookId INT, IN p_CopiesToAdd INT)
BEGIN
    UPDATE Books 
    SET TotalCopies = TotalCopies + p_CopiesToAdd, 
        AvailableCopies = AvailableCopies + p_CopiesToAdd 
    WHERE BookId = p_BookId;
    SELECT ROW_COUNT() AS RowsAffected;
END";
                                using (var cmd = new MySqlCommand(spAddCopiesNoDefiner, connection)) 
                                { 
                                    cmd.ExecuteNonQuery(); 
                                    System.Diagnostics.Debug.WriteLine("CreateBookStoredProcedures: Created sp_AddCopies without SQL SECURITY DEFINER successfully"); 
                                }
                            }
                            catch (Exception fallbackEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"CreateBookStoredProcedures: Failed to create sp_AddCopies even without DEFINER: {fallbackEx.Message}");
                                throw new InvalidOperationException($"Failed to create sp_AddCopies stored procedure. MySQL Error {mysqlEx.Number}: {mysqlEx.Message}. Please check database permissions.", mysqlEx);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Failed to create sp_AddCopies stored procedure. MySQL Error {mysqlEx.Number}: {mysqlEx.Message}", mysqlEx);
                        }
                    }
                    catch (Exception ex) 
                    { 
                        System.Diagnostics.Debug.WriteLine($"CreateBookStoredProcedures: Error creating sp_AddCopies: {ex.Message}");
                        throw new InvalidOperationException($"Failed to create sp_AddCopies stored procedure: {ex.Message}", ex);
                    }
                    using (var cmd = new MySqlCommand(spGetAllCategories, connection)) { cmd.ExecuteNonQuery(); System.Diagnostics.Debug.WriteLine("Created sp_GetAllCategories"); }
                    using (var cmd = new MySqlCommand(spGetBookStatistics, connection)) { cmd.ExecuteNonQuery(); System.Diagnostics.Debug.WriteLine("Created sp_GetBookStatistics"); }

                    System.Diagnostics.Debug.WriteLine("CreateBookStoredProcedures: Successfully created all book-related stored procedures");
                    
                    // Verify critical stored procedures were created - wait a moment for MySQL to commit
                    System.Threading.Thread.Sleep(200);
                    
                    // Verify sp_DeleteBook
                    using (var verifyCmd = new MySqlCommand(
                        "SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = DATABASE() AND routine_name = 'sp_DeleteBook'",
                        connection))
                    {
                        int exists = Convert.ToInt32(verifyCmd.ExecuteScalar());
                        if (exists > 0)
                        {
                            System.Diagnostics.Debug.WriteLine("CreateBookStoredProcedures: Verified sp_DeleteBook exists");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("CreateBookStoredProcedures: WARNING - sp_DeleteBook verification failed!");
                            throw new InvalidOperationException("Failed to verify sp_DeleteBook was created. The procedure may not have been created successfully. Check database permissions.");
                        }
                    }
                    
                    // Verify sp_AddCopies
                    using (var verifyCmd = new MySqlCommand(
                        "SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = DATABASE() AND routine_name = 'sp_AddCopies'",
                        connection))
                    {
                        int exists = Convert.ToInt32(verifyCmd.ExecuteScalar());
                        if (exists > 0)
                        {
                            System.Diagnostics.Debug.WriteLine("CreateBookStoredProcedures: Verified sp_AddCopies exists");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("CreateBookStoredProcedures: WARNING - sp_AddCopies verification failed!");
                            throw new InvalidOperationException("Failed to verify sp_AddCopies was created. The procedure may not have been created successfully. Check database permissions.");
                        }
                    }
                }
                catch (MySqlException mysqlEx)
                {
                    System.Diagnostics.Debug.WriteLine($"CreateBookStoredProcedures MySQL Error: {mysqlEx.Number} - {mysqlEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"CreateBookStoredProcedures SQL State: {mysqlEx.SqlState}");
                    System.Diagnostics.Debug.WriteLine($"CreateBookStoredProcedures StackTrace: {mysqlEx.StackTrace}");
                    throw;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"CreateBookStoredProcedures General Error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"CreateBookStoredProcedures StackTrace: {ex.StackTrace}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating book stored procedures: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error creating book stored procedures StackTrace: {ex.StackTrace}");
                // Re-throw to allow calling code to handle the error properly
                throw;
            }
        }

        /// <summary>
        /// Adds a new book to the database
        /// </summary>
        public int AddBook(string isbn, string title, string author, string publisher, int? publicationYear, 
            string category, int totalCopies, string description)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Ensure stored procedures exist
                    EnsureStoredProceduresExist(connection);
                    
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Validate required fields
                            if (string.IsNullOrWhiteSpace(title))
                                throw new ArgumentException("Title is required.");
                            if (string.IsNullOrWhiteSpace(author))
                                throw new ArgumentException("Author is required.");
                            if (string.IsNullOrWhiteSpace(isbn))
                                throw new ArgumentException("ISBN is required.");
                            if (string.IsNullOrWhiteSpace(category))
                                throw new ArgumentException("Category is required.");
                            if (totalCopies < 1)
                                throw new ArgumentException("Total copies must be at least 1.");

                            // Check for duplicate ISBN
                            using (var checkCommand = new MySqlCommand(
                                "SELECT COUNT(*) FROM Books WHERE REPLACE(REPLACE(UPPER(ISBN), '-', ''), ' ', '') = @CleanISBN", 
                                connection, transaction))
                            {
                                string cleanISBN = System.Text.RegularExpressions.Regex.Replace(isbn.Trim().ToUpper(), @"[-\s]", "");
                                checkCommand.Parameters.AddWithValue("@CleanISBN", cleanISBN);
                                int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                                
                                if (count > 0)
                                {
                                    transaction.Rollback();
                                    throw new InvalidOperationException("A book with this ISBN already exists.");
                                }
                            }

                            // Insert book using stored procedure
                            int bookId = -1;
                            using (var command = new MySqlCommand("sp_AddBook", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("p_ISBN", isbn.Trim());
                                command.Parameters.AddWithValue("p_Title", title.Trim());
                                command.Parameters.AddWithValue("p_Author", author.Trim());
                                command.Parameters.AddWithValue("p_Publisher", string.IsNullOrWhiteSpace(publisher) ? (object)DBNull.Value : publisher.Trim());
                                command.Parameters.AddWithValue("p_PublicationYear", publicationYear.HasValue ? (object)publicationYear.Value : DBNull.Value);
                                command.Parameters.AddWithValue("p_Category", category.Trim());
                                command.Parameters.AddWithValue("p_TotalCopies", totalCopies);
                                command.Parameters.AddWithValue("p_Description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim());

                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        bookId = reader.GetInt32("BookId");
                                    }
                                }
                            }

                            if (bookId <= 0)
                            {
                                transaction.Rollback();
                                System.Diagnostics.Debug.WriteLine("Failed to create book record");
                                return -1;
                            }

                            // Create individual copies in BookCopies table
                            EnsureBookCopiesTableExists(connection, transaction);
                            
                            // Get the book's created date
                            DateTime createdDate = DateTime.Now;
                            using (var getDateCmd = new MySqlCommand("SELECT CreatedDate FROM Books WHERE BookId = @BookId", connection, transaction))
                            {
                                getDateCmd.Parameters.AddWithValue("@BookId", bookId);
                                object dateResult = getDateCmd.ExecuteScalar();
                                if (dateResult != null && dateResult != DBNull.Value)
                                {
                                    createdDate = Convert.ToDateTime(dateResult);
                                }
                            }
                            
                            for (int i = 1; i <= totalCopies; i++)
                            {
                                string accessionNumber = $"ACC-{createdDate.Year}-{bookId:D5}-{i:D3}";
                                
                                string insertCopyQuery = @"
                                    INSERT INTO BookCopies (BookId, AccessionNumber, Location, `Condition`, Status, CreatedDate)
                                    VALUES (@BookId, @AccessionNumber, 'Main Library', 'Good', 'Available', NOW())";
                                
                                using (var insertCopyCmd = new MySqlCommand(insertCopyQuery, connection, transaction))
                                {
                                    insertCopyCmd.Parameters.AddWithValue("@BookId", bookId);
                                    insertCopyCmd.Parameters.AddWithValue("@AccessionNumber", accessionNumber);
                                    insertCopyCmd.ExecuteNonQuery();
                                }
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"Created {totalCopies} individual copies for BookId = {bookId}");

                            // Commit transaction
                            transaction.Commit();
                            System.Diagnostics.Debug.WriteLine($"Book created successfully: BookId = {bookId}");
                            return bookId;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"Error creating book: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (MySqlException mysqlEx)
            {
                // Check for duplicate entry error (MySQL error code 1062)
                if (mysqlEx.Number == 1062)
                {
                    System.Diagnostics.Debug.WriteLine($"Duplicate book entry: {isbn}");
                    return -1; // Book already exists
                }
                System.Diagnostics.Debug.WriteLine($"Error creating book: {mysqlEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating book: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all books from the database
        /// </summary>
        public List<Book> GetAllBooks()
        {
            List<Book> books = new List<Book>();
            
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Ensure stored procedures exist
                    EnsureStoredProceduresExist(connection);
                    
                    using (var command = new MySqlCommand("sp_GetAllBooks", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                books.Add(new Book
                                {
                                    BookId = reader.GetInt32("BookId"),
                                    ISBN = reader.IsDBNull(reader.GetOrdinal("ISBN")) ? "" : reader.GetString("ISBN"),
                                    Title = reader.GetString("Title"),
                                    Author = reader.GetString("Author"),
                                    Publisher = reader.IsDBNull(reader.GetOrdinal("Publisher")) ? "" : reader.GetString("Publisher"),
                                    PublicationYear = reader.IsDBNull(reader.GetOrdinal("PublicationYear")) ? (int?)null : reader.GetInt32("PublicationYear"),
                                    Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "" : reader.GetString("Category"),
                                    TotalCopies = reader.GetInt32("TotalCopies"),
                                    AvailableCopies = reader.GetInt32("AvailableCopies"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString("Description"),
                                    CreatedDate = reader.GetDateTime("CreatedDate")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting books: {ex.Message}");
            }
            
            return books;
        }

        /// <summary>
        /// Gets a single book by ID
        /// </summary>
        public Book GetBookById(int bookId)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Ensure stored procedures exist
                    EnsureStoredProceduresExist(connection);
                    
                    using (var command = new MySqlCommand("sp_GetBookById", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_BookId", bookId);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Book
                                {
                                    BookId = reader.GetInt32("BookId"),
                                    ISBN = reader.IsDBNull(reader.GetOrdinal("ISBN")) ? "" : reader.GetString("ISBN"),
                                    Title = reader.GetString("Title"),
                                    Author = reader.GetString("Author"),
                                    Publisher = reader.IsDBNull(reader.GetOrdinal("Publisher")) ? "" : reader.GetString("Publisher"),
                                    PublicationYear = reader.IsDBNull(reader.GetOrdinal("PublicationYear")) ? (int?)null : reader.GetInt32("PublicationYear"),
                                    Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "" : reader.GetString("Category"),
                                    TotalCopies = reader.GetInt32("TotalCopies"),
                                    AvailableCopies = reader.GetInt32("AvailableCopies"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString("Description"),
                                    CreatedDate = reader.GetDateTime("CreatedDate")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting book: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Updates an existing book in the database
        /// </summary>
        public bool UpdateBook(int bookId, string isbn, string title, string author, string publisher, 
            int? publicationYear, string category, int totalCopies, int availableCopies, string description)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Ensure stored procedures exist
                    EnsureStoredProceduresExist(connection);
                    
                    // Ensure we're using the correct database
                    if (string.IsNullOrEmpty(connection.Database) || !connection.Database.Equals("LMS_DB", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var useDbCmd = new MySqlCommand("USE LMS_DB", connection))
                        {
                            useDbCmd.ExecuteNonQuery();
                            System.Diagnostics.Debug.WriteLine("UpdateBook: Switched to LMS_DB database");
                        }
                    }
                    
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Validate required fields
                            if (string.IsNullOrWhiteSpace(title))
                                throw new ArgumentException("Title is required.");
                            if (string.IsNullOrWhiteSpace(author))
                                throw new ArgumentException("Author is required.");
                            if (string.IsNullOrWhiteSpace(isbn))
                                throw new ArgumentException("ISBN is required.");
                            if (string.IsNullOrWhiteSpace(category))
                                throw new ArgumentException("Category is required.");
                            if (totalCopies < 1)
                                throw new ArgumentException("Total copies must be at least 1.");
                            if (availableCopies < 0 || availableCopies > totalCopies)
                                throw new ArgumentException("Available copies must be between 0 and total copies.");

                            // Check for duplicate ISBN (excluding current book)
                            using (var checkCommand = new MySqlCommand(
                                "SELECT COUNT(*) FROM Books WHERE BookId != @BookId AND REPLACE(REPLACE(UPPER(ISBN), '-', ''), ' ', '') = @CleanISBN", 
                                connection, transaction))
                            {
                                string cleanISBN = System.Text.RegularExpressions.Regex.Replace(isbn.Trim().ToUpper(), @"[-\s]", "");
                                checkCommand.Parameters.AddWithValue("@BookId", bookId);
                                checkCommand.Parameters.AddWithValue("@CleanISBN", cleanISBN);
                                int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                                
                                if (count > 0)
                                {
                                    transaction.Rollback();
                                    throw new InvalidOperationException("A book with this ISBN already exists.");
                                }
                            }

                            // Update book using stored procedure
                            using (var command = new MySqlCommand("sp_UpdateBook", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("p_BookId", bookId);
                                command.Parameters.AddWithValue("p_ISBN", isbn.Trim());
                                command.Parameters.AddWithValue("p_Title", title.Trim());
                                command.Parameters.AddWithValue("p_Author", author.Trim());
                                command.Parameters.AddWithValue("p_Publisher", string.IsNullOrWhiteSpace(publisher) ? (object)DBNull.Value : publisher.Trim());
                                command.Parameters.AddWithValue("p_PublicationYear", publicationYear.HasValue ? (object)publicationYear.Value : DBNull.Value);
                                command.Parameters.AddWithValue("p_Category", category.Trim());
                                command.Parameters.AddWithValue("p_TotalCopies", totalCopies);
                                command.Parameters.AddWithValue("p_AvailableCopies", availableCopies);
                                command.Parameters.AddWithValue("p_Description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim());

                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        int rowsAffected = reader.GetInt32("RowsAffected");
                                        if (rowsAffected == 0)
                                        {
                                            transaction.Rollback();
                                            return false;
                                        }
                                    }
                                }
                            }

                            // Commit transaction
                            transaction.Commit();
                            System.Diagnostics.Debug.WriteLine($"Book updated successfully: BookId = {bookId}");
                            return true;
                        }
                        catch (MySqlException mysqlEx)
                        {
                            transaction.Rollback();
                            // Handle stored procedure not found (1305)
                            if (mysqlEx.Number == 1305)
                            {
                                System.Diagnostics.Debug.WriteLine($"UpdateBook: Stored procedure not found (Error 1305), attempting to create and retry...");
                                // Create procedures and retry once
                                CreateBookStoredProcedures(connection);
                                System.Threading.Thread.Sleep(200); // Wait for MySQL to commit
                                
                                // Retry the update operation
                                using (var retryTransaction = connection.BeginTransaction())
                                {
                                    try
                                    {
                                        using (var retryCommand = new MySqlCommand("sp_UpdateBook", connection, retryTransaction))
                                        {
                                            retryCommand.CommandType = CommandType.StoredProcedure;
                                            retryCommand.Parameters.AddWithValue("p_BookId", bookId);
                                            retryCommand.Parameters.AddWithValue("p_ISBN", isbn.Trim());
                                            retryCommand.Parameters.AddWithValue("p_Title", title.Trim());
                                            retryCommand.Parameters.AddWithValue("p_Author", author.Trim());
                                            retryCommand.Parameters.AddWithValue("p_Publisher", string.IsNullOrWhiteSpace(publisher) ? (object)DBNull.Value : publisher.Trim());
                                            retryCommand.Parameters.AddWithValue("p_PublicationYear", publicationYear.HasValue ? (object)publicationYear.Value : DBNull.Value);
                                            retryCommand.Parameters.AddWithValue("p_Category", category.Trim());
                                            retryCommand.Parameters.AddWithValue("p_TotalCopies", totalCopies);
                                            retryCommand.Parameters.AddWithValue("p_AvailableCopies", availableCopies);
                                            retryCommand.Parameters.AddWithValue("p_Description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim());
                                            
                                            using (var reader = retryCommand.ExecuteReader())
                                            {
                                                if (reader.Read())
                                                {
                                                    int rowsAffected = reader.GetInt32("RowsAffected");
                                                    if (rowsAffected > 0)
                                                    {
                                                        retryTransaction.Commit();
                                                        System.Diagnostics.Debug.WriteLine($"Book updated successfully after retry: BookId = {bookId}");
                                                        return true;
                                                    }
                                                }
                                            }
                                        }
                                        retryTransaction.Rollback();
                                        return false;
                                    }
                                    catch (Exception retryEx)
                                    {
                                        retryTransaction.Rollback();
                                        System.Diagnostics.Debug.WriteLine($"UpdateBook: Error in retry transaction: {retryEx.Message}");
                                        throw;
                                    }
                                }
                            }
                            System.Diagnostics.Debug.WriteLine($"UpdateBook MySQL Error in transaction: {mysqlEx.Number} - {mysqlEx.Message}");
                            throw;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"Error updating book: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (MySqlException mysqlEx)
            {
                // Handle stored procedure not found error (1305) - outer catch
                if (mysqlEx.Number == 1305)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateBook: Stored procedure sp_UpdateBook not found (Error 1305) in outer catch, attempting to create it...");
                    try
                    {
                        using (var retryConnection = MYSqlHelper.CreateConnection())
                        {
                            EnsureStoredProceduresExist(retryConnection);
                            System.Diagnostics.Debug.WriteLine("UpdateBook: Stored procedures ensured, retrying update operation...");
                            // Retry the update operation recursively (only once)
                            return UpdateBook(bookId, isbn, title, author, publisher, publicationYear, category, totalCopies, availableCopies, description);
                        }
                    }
                    catch (Exception retryEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"UpdateBook: Failed to create stored procedure and retry: {retryEx.Message}");
                        throw new InvalidOperationException($"Failed to create sp_UpdateBook stored procedure: {retryEx.Message}", retryEx);
                    }
                }
                System.Diagnostics.Debug.WriteLine($"UpdateBook MySQL Error: {mysqlEx.Number} - {mysqlEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating book: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a book from the database
        /// Checks for active borrowings and maintains data integrity
        /// </summary>
        public bool DeleteBook(int bookId)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Explicitly set database first
                    using (var useDbCmd = new MySqlCommand("USE LMS_DB", connection))
                    {
                        useDbCmd.ExecuteNonQuery();
                        System.Diagnostics.Debug.WriteLine("DeleteBook: Set to LMS_DB database");
                    }
                    
                    // Ensure stored procedures exist BEFORE starting transaction
                    EnsureStoredProceduresExist(connection);
                    
                    // Verify sp_DeleteBook exists before proceeding
                    bool useStoredProcedure = true;
                    using (var verifyCmd = new MySqlCommand(
                        "SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = 'LMS_DB' AND routine_name = 'sp_DeleteBook'",
                        connection))
                    {
                        int exists = Convert.ToInt32(verifyCmd.ExecuteScalar());
                        if (exists == 0)
                        {
                            System.Diagnostics.Debug.WriteLine("DeleteBook: sp_DeleteBook not found, attempting to create it...");
                            try
                            {
                                CreateBookStoredProcedures(connection);
                                // Verify again
                                exists = Convert.ToInt32(verifyCmd.ExecuteScalar());
                                if (exists == 0)
                                {
                                    System.Diagnostics.Debug.WriteLine("DeleteBook: Failed to create stored procedure, will use direct SQL instead");
                                    useStoredProcedure = false;
                                }
                            }
                            catch (Exception createEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"DeleteBook: Failed to create stored procedure: {createEx.Message}, will use direct SQL instead");
                                useStoredProcedure = false;
                            }
                        }
                    }
                    
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Ensure database is set in transaction context
                            using (var useDbInTrans = new MySqlCommand("USE LMS_DB", connection, transaction))
                            {
                                useDbInTrans.ExecuteNonQuery();
                            }
                            
                            int rowsAffected = 0;
                            
                            if (useStoredProcedure)
                            {
                                // Delete book using stored procedure (handles all checks internally)
                                using (var deleteCommand = new MySqlCommand("sp_DeleteBook", connection, transaction))
                                {
                                    deleteCommand.CommandType = CommandType.StoredProcedure;
                                    deleteCommand.Parameters.AddWithValue("p_BookId", bookId);
                                    
                                    using (var reader = deleteCommand.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            rowsAffected = reader.GetInt32("RowsAffected");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Fallback to direct SQL if stored procedure doesn't exist
                                // First check for active borrowings
                                using (var checkCommand = new MySqlCommand(
                                    "SELECT COUNT(*) FROM Borrowings WHERE BookId = @BookId AND ReturnDate IS NULL",
                                    connection, transaction))
                                {
                                    checkCommand.Parameters.AddWithValue("@BookId", bookId);
                                    int activeBorrowings = Convert.ToInt32(checkCommand.ExecuteScalar());
                                    
                                    if (activeBorrowings > 0)
                                    {
                                        transaction.Rollback();
                                        throw new InvalidOperationException($"Cannot delete book: There are {activeBorrowings} active borrowing(s) for this book.");
                                    }
                                    
                                    // Check for any borrowings (to maintain history)
                                    using (var checkHistoryCommand = new MySqlCommand(
                                        "SELECT COUNT(*) FROM Borrowings WHERE BookId = @BookId",
                                        connection, transaction))
                                    {
                                        checkHistoryCommand.Parameters.AddWithValue("@BookId", bookId);
                                        int totalBorrowings = Convert.ToInt32(checkHistoryCommand.ExecuteScalar());
                                        
                                        if (totalBorrowings > 0)
                                        {
                                            transaction.Rollback();
                                            throw new InvalidOperationException($"Cannot delete book: This book has borrowing history ({totalBorrowings} record(s)).");
                                        }
                                    }
                                }
                                
                                // Delete the book
                                using (var deleteCommand = new MySqlCommand(
                                    "DELETE FROM Books WHERE BookId = @BookId",
                                    connection, transaction))
                                {
                                    deleteCommand.Parameters.AddWithValue("@BookId", bookId);
                                    rowsAffected = deleteCommand.ExecuteNonQuery();
                                }
                            }
                            
                            if (rowsAffected > 0)
                            {
                                transaction.Commit();
                                System.Diagnostics.Debug.WriteLine($"Book deleted successfully: BookId = {bookId}");
                                return true;
                            }
                            
                            transaction.Rollback();
                            return false;
                        }
                        catch (MySqlException mysqlEx)
                        {
                            transaction.Rollback();
                            // Handle stored procedure SIGNAL errors (error code 1644)
                            if (mysqlEx.Number == 1644) // SIGNAL error from stored procedure
                            {
                                System.Diagnostics.Debug.WriteLine($"DeleteBook: Stored procedure SIGNAL error: {mysqlEx.Message}");
                                throw new InvalidOperationException(mysqlEx.Message, mysqlEx);
                            }
                            // Handle stored procedure not found (1305) - fallback to direct SQL
                            else if (mysqlEx.Number == 1305)
                            {
                                System.Diagnostics.Debug.WriteLine($"DeleteBook: Stored procedure not found (Error 1305), using direct SQL fallback...");
                                try
                                {
                                    // Retry with direct SQL
                                    using (var retryTransaction = connection.BeginTransaction())
                                    {
                                        try
                                        {
                                            // Ensure database is set in retry transaction
                                            using (var useDbInTrans = new MySqlCommand("USE LMS_DB", connection, retryTransaction))
                                            {
                                                useDbInTrans.ExecuteNonQuery();
                                            }
                                            
                                            // Check for active borrowings
                                            using (var checkCommand = new MySqlCommand(
                                                "SELECT COUNT(*) FROM Borrowings WHERE BookId = @BookId AND ReturnDate IS NULL",
                                                connection, retryTransaction))
                                            {
                                                checkCommand.Parameters.AddWithValue("@BookId", bookId);
                                                int activeBorrowings = Convert.ToInt32(checkCommand.ExecuteScalar());
                                                
                                                if (activeBorrowings > 0)
                                                {
                                                    retryTransaction.Rollback();
                                                    throw new InvalidOperationException($"Cannot delete book: There are {activeBorrowings} active borrowing(s) for this book.");
                                                }
                                                
                                                // Check for any borrowings (to maintain history)
                                                using (var checkHistoryCommand = new MySqlCommand(
                                                    "SELECT COUNT(*) FROM Borrowings WHERE BookId = @BookId",
                                                    connection, retryTransaction))
                                                {
                                                    checkHistoryCommand.Parameters.AddWithValue("@BookId", bookId);
                                                    int totalBorrowings = Convert.ToInt32(checkHistoryCommand.ExecuteScalar());
                                                    
                                                    if (totalBorrowings > 0)
                                                    {
                                                        retryTransaction.Rollback();
                                                        throw new InvalidOperationException($"Cannot delete book: This book has borrowing history ({totalBorrowings} record(s)).");
                                                    }
                                                }
                                            }
                                            
                                            // Delete the book
                                            using (var deleteCommand = new MySqlCommand(
                                                "DELETE FROM Books WHERE BookId = @BookId",
                                                connection, retryTransaction))
                                            {
                                                deleteCommand.Parameters.AddWithValue("@BookId", bookId);
                                                int rowsAffected = deleteCommand.ExecuteNonQuery();
                                                
                                                if (rowsAffected > 0)
                                                {
                                                    retryTransaction.Commit();
                                                    System.Diagnostics.Debug.WriteLine($"Book deleted successfully using direct SQL fallback: BookId = {bookId}");
                                                    return true;
                                                }
                                            }
                                            retryTransaction.Rollback();
                                            return false;
                                        }
                                        catch (Exception retryEx)
                                        {
                                            retryTransaction.Rollback();
                                            System.Diagnostics.Debug.WriteLine($"DeleteBook: Error in direct SQL fallback: {retryEx.Message}");
                                            throw;
                                        }
                                    }
                                }
                                catch (Exception fallbackEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"DeleteBook: Direct SQL fallback also failed: {fallbackEx.Message}");
                                    throw;
                                }
                            }
                            System.Diagnostics.Debug.WriteLine($"DeleteBook MySQL Error in transaction: {mysqlEx.Number} - {mysqlEx.Message}");
                            throw;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"Error deleting book: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (MySqlException mysqlEx)
            {
                // Handle stored procedure not found error (1305) - use direct SQL fallback
                if (mysqlEx.Number == 1305) // Procedure or function doesn't exist
                {
                    System.Diagnostics.Debug.WriteLine($"DeleteBook: Stored procedure sp_DeleteBook not found (Error 1305), using direct SQL fallback...");
                    try
                    {
                        using (var retryConnection = MYSqlHelper.CreateConnection())
                        {
                            using (var useDbCmd = new MySqlCommand("USE LMS_DB", retryConnection))
                            {
                                useDbCmd.ExecuteNonQuery();
                            }
                            
                            using (var transaction = retryConnection.BeginTransaction())
                            {
                                try
                                {
                                    // Check for active borrowings
                                    using (var checkCommand = new MySqlCommand(
                                        "SELECT COUNT(*) FROM Borrowings WHERE BookId = @BookId AND ReturnDate IS NULL",
                                        retryConnection, transaction))
                                    {
                                        checkCommand.Parameters.AddWithValue("@BookId", bookId);
                                        int activeBorrowings = Convert.ToInt32(checkCommand.ExecuteScalar());
                                        
                                        if (activeBorrowings > 0)
                                        {
                                            transaction.Rollback();
                                            throw new InvalidOperationException($"Cannot delete book: There are {activeBorrowings} active borrowing(s) for this book.");
                                        }
                                        
                                        // Check for any borrowings (to maintain history)
                                        using (var checkHistoryCommand = new MySqlCommand(
                                            "SELECT COUNT(*) FROM Borrowings WHERE BookId = @BookId",
                                            retryConnection, transaction))
                                        {
                                            checkHistoryCommand.Parameters.AddWithValue("@BookId", bookId);
                                            int totalBorrowings = Convert.ToInt32(checkHistoryCommand.ExecuteScalar());
                                            
                                            if (totalBorrowings > 0)
                                            {
                                                transaction.Rollback();
                                                throw new InvalidOperationException($"Cannot delete book: This book has borrowing history ({totalBorrowings} record(s)).");
                                            }
                                        }
                                    }
                                    
                                    // Delete the book
                                    using (var deleteCommand = new MySqlCommand(
                                        "DELETE FROM Books WHERE BookId = @BookId",
                                        retryConnection, transaction))
                                    {
                                        deleteCommand.Parameters.AddWithValue("@BookId", bookId);
                                        int rowsAffected = deleteCommand.ExecuteNonQuery();
                                        
                                        if (rowsAffected > 0)
                                        {
                                            transaction.Commit();
                                            System.Diagnostics.Debug.WriteLine($"DeleteBook: Successfully deleted book using direct SQL fallback: BookId = {bookId}");
                                            return true;
                                        }
                                    }
                                    transaction.Rollback();
                                    return false;
                                }
                                catch (Exception)
                                {
                                    transaction.Rollback();
                                    throw;
                                }
                            }
                        }
                    }
                    catch (Exception retryEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"DeleteBook: Direct SQL fallback failed: {retryEx.Message}");
                        throw new InvalidOperationException($"Failed to delete book. Please check database permissions and connection.", retryEx);
                    }
                }
                // Handle stored procedure SIGNAL errors (error code 1644) and foreign key constraint violations
                else if (mysqlEx.Number == 1644) // SIGNAL error from stored procedure
                {
                    throw new InvalidOperationException(mysqlEx.Message, mysqlEx);
                }
                else if (mysqlEx.Number == 1451) // Cannot delete or update a parent row: a foreign key constraint fails
                {
                    throw new InvalidOperationException(
                        "Cannot delete book: There are related records in the system. " +
                        "Please ensure all borrowings and related data are removed first.",
                        mysqlEx
                    );
                }
                System.Diagnostics.Debug.WriteLine($"MySQL Error deleting book: {mysqlEx.Number} - {mysqlEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting book: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Adds copies to an existing book
        /// </summary>
        public bool AddCopies(int bookId, int copiesToAdd)
        {
            if (copiesToAdd <= 0)
            {
                throw new ArgumentException("Number of copies to add must be greater than zero.", nameof(copiesToAdd));
            }

            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Explicitly set database first
                    using (var useDbCmd = new MySqlCommand("USE LMS_DB", connection))
                    {
                        useDbCmd.ExecuteNonQuery();
                        System.Diagnostics.Debug.WriteLine("AddCopies: Set to LMS_DB database");
                    }
                    
                    // Ensure stored procedures exist BEFORE starting transaction
                    EnsureStoredProceduresExist(connection);
                    
                    // Verify sp_AddCopies exists before proceeding
                    bool useStoredProcedure = true;
                    using (var verifyCmd = new MySqlCommand(
                        "SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = 'LMS_DB' AND routine_name = 'sp_AddCopies'",
                        connection))
                    {
                        int exists = Convert.ToInt32(verifyCmd.ExecuteScalar());
                        if (exists == 0)
                        {
                            System.Diagnostics.Debug.WriteLine("AddCopies: sp_AddCopies not found, attempting to create it...");
                            try
                            {
                                CreateBookStoredProcedures(connection);
                                // Verify again
                                exists = Convert.ToInt32(verifyCmd.ExecuteScalar());
                                if (exists == 0)
                                {
                                    System.Diagnostics.Debug.WriteLine("AddCopies: Failed to create stored procedure, will use direct SQL instead");
                                    useStoredProcedure = false;
                                }
                            }
                            catch (Exception createEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"AddCopies: Failed to create stored procedure: {createEx.Message}, will use direct SQL instead");
                                useStoredProcedure = false;
                            }
                        }
                    }
                    
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Get current book data
                            Book currentBook = GetBookById(bookId);
                            if (currentBook == null)
                            {
                                transaction.Rollback();
                                return false; // Book not found
                            }

                            // Ensure database is set in transaction context
                            using (var useDbInTrans = new MySqlCommand("USE LMS_DB", connection, transaction))
                            {
                                useDbInTrans.ExecuteNonQuery();
                            }

                            int rowsAffected = 0;
                            
                            if (useStoredProcedure)
                            {
                                // Update total copies and available copies using stored procedure
                                using (var command = new MySqlCommand("sp_AddCopies", connection, transaction))
                                {
                                    command.CommandType = CommandType.StoredProcedure;
                                    command.Parameters.AddWithValue("p_BookId", bookId);
                                    command.Parameters.AddWithValue("p_CopiesToAdd", copiesToAdd);

                                    using (var reader = command.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            rowsAffected = reader.GetInt32("RowsAffected");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Fallback to direct SQL if stored procedure doesn't exist
                                using (var command = new MySqlCommand(
                                    "UPDATE Books SET TotalCopies = TotalCopies + @CopiesToAdd, AvailableCopies = AvailableCopies + @CopiesToAdd WHERE BookId = @BookId",
                                    connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@BookId", bookId);
                                    command.Parameters.AddWithValue("@CopiesToAdd", copiesToAdd);
                                    rowsAffected = command.ExecuteNonQuery();
                                }
                            }
                            
                            if (rowsAffected > 0)
                            {
                                // Create individual copies in BookCopies table
                                EnsureBookCopiesTableExists(connection, transaction);
                                
                                // Get the book's created date and current copy count
                                DateTime createdDate = DateTime.Now;
                                int existingCopies = 0;
                                using (var getInfoCmd = new MySqlCommand("SELECT CreatedDate, (SELECT COUNT(*) FROM BookCopies WHERE BookId = @BookId) AS ExistingCopies FROM Books WHERE BookId = @BookId", connection, transaction))
                                {
                                    getInfoCmd.Parameters.AddWithValue("@BookId", bookId);
                                    using (var reader = getInfoCmd.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            if (reader["CreatedDate"] != DBNull.Value)
                                                createdDate = reader.GetDateTime("CreatedDate");
                                            existingCopies = reader.GetInt32("ExistingCopies");
                                        }
                                    }
                                }
                                
                                // Create new individual copies
                                for (int i = 1; i <= copiesToAdd; i++)
                                {
                                    string accessionNumber = $"ACC-{createdDate.Year}-{bookId:D5}-{existingCopies + i:D3}";
                                    
                                    string insertCopyQuery = @"
                                        INSERT INTO BookCopies (BookId, AccessionNumber, Location, `Condition`, Status, CreatedDate)
                                        VALUES (@BookId, @AccessionNumber, 'Main Library', 'Good', 'Available', NOW())";
                                    
                                    using (var insertCopyCmd = new MySqlCommand(insertCopyQuery, connection, transaction))
                                    {
                                        insertCopyCmd.Parameters.AddWithValue("@BookId", bookId);
                                        insertCopyCmd.Parameters.AddWithValue("@AccessionNumber", accessionNumber);
                                        insertCopyCmd.ExecuteNonQuery();
                                    }
                                }
                                
                                System.Diagnostics.Debug.WriteLine($"Created {copiesToAdd} individual copies in BookCopies for BookId = {bookId}");
                                
                                transaction.Commit();
                                System.Diagnostics.Debug.WriteLine($"Copies added successfully: BookId = {bookId}, CopiesAdded = {copiesToAdd}, RowsAffected = {rowsAffected}");
                                return true;
                            }
                            
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"AddCopies: No rows affected for BookId = {bookId}");
                            return false;
                        }
                        catch (MySqlException mysqlEx)
                        {
                            transaction.Rollback();
                            // Handle stored procedure not found (1305) - fallback to direct SQL
                            if (mysqlEx.Number == 1305)
                            {
                                System.Diagnostics.Debug.WriteLine($"AddCopies: Stored procedure not found (Error 1305), using direct SQL fallback...");
                                try
                                {
                                    // Retry with direct SQL
                                    using (var retryTransaction = connection.BeginTransaction())
                                    {
                                        try
                                        {
                                            // Ensure database is set in retry transaction
                                            using (var useDbInTrans = new MySqlCommand("USE LMS_DB", connection, retryTransaction))
                                            {
                                                useDbInTrans.ExecuteNonQuery();
                                            }
                                            
                                            // Use direct SQL instead of stored procedure
                                            using (var retryCommand = new MySqlCommand(
                                                "UPDATE Books SET TotalCopies = TotalCopies + @CopiesToAdd, AvailableCopies = AvailableCopies + @CopiesToAdd WHERE BookId = @BookId",
                                                connection, retryTransaction))
                                            {
                                                retryCommand.Parameters.AddWithValue("@BookId", bookId);
                                                retryCommand.Parameters.AddWithValue("@CopiesToAdd", copiesToAdd);
                                                int rowsAffected = retryCommand.ExecuteNonQuery();
                                                
                                                if (rowsAffected > 0)
                                                {
                                                    // Create individual copies in BookCopies table
                                                    EnsureBookCopiesTableExists(connection, retryTransaction);
                                                    
                                                    // Get the book's created date and current copy count
                                                    DateTime createdDate = DateTime.Now;
                                                    int existingCopies = 0;
                                                    using (var getInfoCmd = new MySqlCommand("SELECT CreatedDate, (SELECT COUNT(*) FROM BookCopies WHERE BookId = @BookId) AS ExistingCopies FROM Books WHERE BookId = @BookId", connection, retryTransaction))
                                                    {
                                                        getInfoCmd.Parameters.AddWithValue("@BookId", bookId);
                                                        using (var reader = getInfoCmd.ExecuteReader())
                                                        {
                                                            if (reader.Read())
                                                            {
                                                                if (reader["CreatedDate"] != DBNull.Value)
                                                                    createdDate = reader.GetDateTime("CreatedDate");
                                                                existingCopies = reader.GetInt32("ExistingCopies");
                                                            }
                                                        }
                                                    }
                                                    
                                                    // Create new individual copies
                                                    for (int i = 1; i <= copiesToAdd; i++)
                                                    {
                                                        string accessionNumber = $"ACC-{createdDate.Year}-{bookId:D5}-{existingCopies + i:D3}";
                                                        
                                                        string insertCopyQuery = @"
                                                            INSERT INTO BookCopies (BookId, AccessionNumber, Location, `Condition`, Status, CreatedDate)
                                                            VALUES (@BookId, @AccessionNumber, 'Main Library', 'Good', 'Available', NOW())";
                                                        
                                                        using (var insertCopyCmd = new MySqlCommand(insertCopyQuery, connection, retryTransaction))
                                                        {
                                                            insertCopyCmd.Parameters.AddWithValue("@BookId", bookId);
                                                            insertCopyCmd.Parameters.AddWithValue("@AccessionNumber", accessionNumber);
                                                            insertCopyCmd.ExecuteNonQuery();
                                                        }
                                                    }
                                                    
                                                    System.Diagnostics.Debug.WriteLine($"Created {copiesToAdd} individual copies in BookCopies for BookId = {bookId}");
                                                    
                                                    retryTransaction.Commit();
                                                    System.Diagnostics.Debug.WriteLine($"Copies added successfully using direct SQL fallback: BookId = {bookId}");
                                                    return true;
                                                }
                                            }
                                            retryTransaction.Rollback();
                                            return false;
                                        }
                                        catch (Exception retryEx)
                                        {
                                            retryTransaction.Rollback();
                                            System.Diagnostics.Debug.WriteLine($"AddCopies: Error in direct SQL fallback: {retryEx.Message}");
                                            throw;
                                        }
                                    }
                                }
                                catch (Exception fallbackEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"AddCopies: Direct SQL fallback also failed: {fallbackEx.Message}");
                                    throw new InvalidOperationException($"Failed to add copies. Please check database permissions and connection.", fallbackEx);
                                }
                            }
                            System.Diagnostics.Debug.WriteLine($"AddCopies MySQL Error in transaction: {mysqlEx.Number} - {mysqlEx.Message}");
                            throw;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"Error adding copies: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (MySqlException mysqlEx)
            {
                // Handle stored procedure not found error (1305) - outer catch - retry with direct SQL
                if (mysqlEx.Number == 1305)
                {
                    System.Diagnostics.Debug.WriteLine($"AddCopies: Stored procedure sp_AddCopies not found (Error 1305) in outer catch, using direct SQL fallback...");
                    try
                    {
                        using (var retryConnection = MYSqlHelper.CreateConnection())
                        {
                            using (var useDbCmd = new MySqlCommand("USE LMS_DB", retryConnection))
                            {
                                useDbCmd.ExecuteNonQuery();
                            }
                            
                            using (var transaction = retryConnection.BeginTransaction())
                            {
                                try
                                {
                                    using (var command = new MySqlCommand(
                                        "UPDATE Books SET TotalCopies = TotalCopies + @CopiesToAdd, AvailableCopies = AvailableCopies + @CopiesToAdd WHERE BookId = @BookId",
                                        retryConnection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@BookId", bookId);
                                        command.Parameters.AddWithValue("@CopiesToAdd", copiesToAdd);
                                        int rowsAffected = command.ExecuteNonQuery();
                                        
                                        if (rowsAffected > 0)
                                        {
                                            transaction.Commit();
                                            System.Diagnostics.Debug.WriteLine($"AddCopies: Successfully added copies using direct SQL fallback: BookId = {bookId}");
                                            return true;
                                        }
                                    }
                                    transaction.Rollback();
                                    return false;
                                }
                                catch (Exception)
                                {
                                    transaction.Rollback();
                                    throw;
                                }
                            }
                        }
                    }
                    catch (Exception retryEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"AddCopies: Direct SQL fallback failed: {retryEx.Message}");
                        throw new InvalidOperationException($"Failed to add copies. Please check database permissions and connection.", retryEx);
                    }
                }
                System.Diagnostics.Debug.WriteLine($"AddCopies MySQL Error: {mysqlEx.Number} - {mysqlEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding copies: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all distinct categories from the database
        /// </summary>
        public List<string> GetAllCategories()
        {
            List<string> categories = new List<string>();
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Ensure stored procedures exist
                    EnsureStoredProceduresExist(connection);
                    
                    using (var command = new MySqlCommand("sp_GetAllCategories", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string category = reader.GetString("Category");
                                if (!string.IsNullOrWhiteSpace(category) && !categories.Contains(category))
                                {
                                    categories.Add(category);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting categories: {ex.Message}");
            }
            return categories;
        }

        /// <summary>
        /// Searches books by title, author, or ISBN with optional category filter
        /// </summary>
        public List<Book> SearchBooks(string searchTerm, string category = null)
        {
            List<Book> books = new List<Book>();
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Ensure stored procedures exist
                    EnsureStoredProceduresExist(connection);
                    
                    using (var command = new MySqlCommand("sp_SearchBooks", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm.Trim());
                        command.Parameters.AddWithValue("p_Category", string.IsNullOrWhiteSpace(category) || category == "All Categories" ? (object)DBNull.Value : category);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                books.Add(new Book
                                {
                                    BookId = reader.GetInt32("BookId"),
                                    ISBN = reader.IsDBNull(reader.GetOrdinal("ISBN")) ? "" : reader.GetString("ISBN"),
                                    Title = reader.GetString("Title"),
                                    Author = reader.GetString("Author"),
                                    Publisher = reader.IsDBNull(reader.GetOrdinal("Publisher")) ? "" : reader.GetString("Publisher"),
                                    PublicationYear = reader.IsDBNull(reader.GetOrdinal("PublicationYear")) ? (int?)null : reader.GetInt32("PublicationYear"),
                                    Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "" : reader.GetString("Category"),
                                    TotalCopies = reader.GetInt32("TotalCopies"),
                                    AvailableCopies = reader.GetInt32("AvailableCopies"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString("Description"),
                                    CreatedDate = reader.GetDateTime("CreatedDate")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching books: {ex.Message}");
            }
            return books;
        }

        /// <summary>
        /// Gets book statistics
        /// </summary>
        public Dictionary<string, int> GetBookStatistics()
        {
            Dictionary<string, int> stats = new Dictionary<string, int>();
            
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Ensure stored procedures exist
                    EnsureStoredProceduresExist(connection);
                    
                    // Ensure we're using the correct database
                    using (var useDbCmd = new MySqlCommand("USE LMS_DB", connection))
                    {
                        useDbCmd.ExecuteNonQuery();
                    }
                    
                    // Check if stored procedure exists
                    bool useStoredProcedure = true;
                    using (var verifyCmd = new MySqlCommand(
                        "SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = 'LMS_DB' AND routine_name = 'sp_GetBookStatistics'",
                        connection))
                    {
                        int exists = Convert.ToInt32(verifyCmd.ExecuteScalar());
                        if (exists == 0)
                        {
                            System.Diagnostics.Debug.WriteLine("GetBookStatistics: sp_GetBookStatistics not found, using direct SQL");
                            useStoredProcedure = false;
                        }
                    }
                    
                    if (useStoredProcedure)
                    {
                        System.Diagnostics.Debug.WriteLine("GetBookStatistics: Calling sp_GetBookStatistics stored procedure");
                        
                        using (var command = new MySqlCommand("sp_GetBookStatistics", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    stats["TotalTitles"] = reader.GetInt32("TotalTitles");
                                    stats["TotalCopies"] = reader.GetInt32("TotalCopies");
                                    stats["AvailableCopies"] = reader.GetInt32("AvailableCopies");
                                    stats["Categories"] = reader.GetInt32("Categories");
                                    
                                    System.Diagnostics.Debug.WriteLine($"GetBookStatistics: Retrieved - TotalTitles={stats["TotalTitles"]}, TotalCopies={stats["TotalCopies"]}, AvailableCopies={stats["AvailableCopies"]}, Categories={stats["Categories"]}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("GetBookStatistics: No data returned from stored procedure");
                                    stats["TotalTitles"] = 0;
                                    stats["TotalCopies"] = 0;
                                    stats["AvailableCopies"] = 0;
                                    stats["Categories"] = 0;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Fallback to direct SQL
                        System.Diagnostics.Debug.WriteLine("GetBookStatistics: Using direct SQL fallback");
                        string sql = @"
                            SELECT 
                                COUNT(*) AS TotalTitles,
                                COALESCE(SUM(TotalCopies), 0) AS TotalCopies,
                                COALESCE(SUM(AvailableCopies), 0) AS AvailableCopies,
                                COUNT(DISTINCT Category) AS Categories
                            FROM Books";
                        
                        using (var command = new MySqlCommand(sql, connection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    stats["TotalTitles"] = reader.GetInt32("TotalTitles");
                                    stats["TotalCopies"] = reader.GetInt32("TotalCopies");
                                    stats["AvailableCopies"] = reader.GetInt32("AvailableCopies");
                                    stats["Categories"] = reader.GetInt32("Categories");
                                    
                                    System.Diagnostics.Debug.WriteLine($"GetBookStatistics: Retrieved via direct SQL - TotalTitles={stats["TotalTitles"]}, TotalCopies={stats["TotalCopies"]}, AvailableCopies={stats["AvailableCopies"]}, Categories={stats["Categories"]}");
                                }
                                else
                                {
                                    stats["TotalTitles"] = 0;
                                    stats["TotalCopies"] = 0;
                                    stats["AvailableCopies"] = 0;
                                    stats["Categories"] = 0;
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException mysqlEx)
            {
                System.Diagnostics.Debug.WriteLine($"GetBookStatistics MySQL Error: {mysqlEx.Number} - {mysqlEx.Message}");
                System.Diagnostics.Debug.WriteLine($"GetBookStatistics StackTrace: {mysqlEx.StackTrace}");
                
                // Try direct SQL as fallback
                try
                {
                    using (var fallbackConnection = MYSqlHelper.CreateConnection())
                    {
                        using (var useDbCmd = new MySqlCommand("USE LMS_DB", fallbackConnection))
                        {
                            useDbCmd.ExecuteNonQuery();
                        }
                        
                        string sql = @"
                            SELECT 
                                COUNT(*) AS TotalTitles,
                                COALESCE(SUM(TotalCopies), 0) AS TotalCopies,
                                COALESCE(SUM(AvailableCopies), 0) AS AvailableCopies,
                                COUNT(DISTINCT Category) AS Categories
                            FROM Books";
                        
                        using (var command = new MySqlCommand(sql, fallbackConnection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    stats["TotalTitles"] = reader.GetInt32("TotalTitles");
                                    stats["TotalCopies"] = reader.GetInt32("TotalCopies");
                                    stats["AvailableCopies"] = reader.GetInt32("AvailableCopies");
                                    stats["Categories"] = reader.GetInt32("Categories");
                                    System.Diagnostics.Debug.WriteLine($"GetBookStatistics: Retrieved via direct SQL fallback after error");
                                    return stats;
                                }
                            }
                        }
                    }
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"GetBookStatistics: Direct SQL fallback also failed: {fallbackEx.Message}");
                }
                
                stats["TotalTitles"] = 0;
                stats["TotalCopies"] = 0;
                stats["AvailableCopies"] = 0;
                stats["Categories"] = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetBookStatistics Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"GetBookStatistics StackTrace: {ex.StackTrace}");
                stats["TotalTitles"] = 0;
                stats["TotalCopies"] = 0;
                stats["AvailableCopies"] = 0;
                stats["Categories"] = 0;
            }
            
            return stats;
        }
        
        /// <summary>
        /// Ensures the BookCopies table exists, creates it if it doesn't
        /// </summary>
        private void EnsureBookCopiesTableExists(MySqlConnection connection, MySqlTransaction transaction = null)
        {
            try
            {
                // Ensure we're using the correct database
                using (var useDbCmd = new MySqlCommand("USE LMS_DB", connection, transaction))
                {
                    useDbCmd.ExecuteNonQuery();
                }
                
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = 'LMS_DB' 
                    AND table_name = 'BookCopies'";
                
                using (var checkCmd = new MySqlCommand(checkTableQuery, connection, transaction))
                {
                    int tableExists = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (tableExists == 0)
                    {
                        // Create BookCopies table
                        string createTableQuery = @"
                            CREATE TABLE BookCopies (
                                CopyId INT PRIMARY KEY AUTO_INCREMENT,
                                BookId INT NOT NULL,
                                AccessionNumber VARCHAR(50) UNIQUE NOT NULL,
                                Location VARCHAR(255) DEFAULT 'Main Library',
                                `Condition` VARCHAR(50) DEFAULT 'Good',
                                Status VARCHAR(50) DEFAULT 'Available',
                                CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                                LastUpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                                FOREIGN KEY (BookId) REFERENCES Books(BookId) ON DELETE CASCADE,
                                INDEX idx_BookId (BookId),
                                INDEX idx_Status (Status),
                                INDEX idx_Condition (`Condition`),
                                INDEX idx_AccessionNumber (AccessionNumber)
                            )";
                        
                        using (var createCmd = new MySqlCommand(createTableQuery, connection, transaction))
                        {
                            createCmd.ExecuteNonQuery();
                            System.Diagnostics.Debug.WriteLine("BookCopies table created successfully");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ensuring BookCopies table exists: {ex.Message}");
                // Don't throw - allow the process to continue
            }
        }
    }
}

