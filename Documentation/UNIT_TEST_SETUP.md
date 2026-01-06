# Unit Test Project Setup Guide

## Overview

This guide explains how to set up a unit test project for the Library Management System.

---

## Option 1: Using Visual Studio Test Project Template

### Step 1: Create Test Project

1. Right-click on solution in Visual Studio
2. Select **Add** → **New Project**
3. Choose **MSTest Test Project (.NET Framework)** or **NUnit Test Project**
4. Name it: `Library Management System.Tests`
5. Click **Create**

### Step 2: Add References

1. Right-click on test project → **Add** → **Reference**
2. Add reference to main project: `Library Management System`
3. Add NuGet packages:
   - For MSTest: Already included
   - For NUnit: Install `NUnit` and `NUnit3TestAdapter`
   - For Moq (mocking): Install `Moq`

### Step 3: Create Test Classes

Create test classes for each service:
- `AuthenticationServiceTests.cs`
- `BookServiceTests.cs`
- `MembersServiceTests.cs`
- `CirculationServiceTests.cs`
- `ReportsServiceTests.cs`
- `SearchServiceTests.cs`

---

## Option 2: Manual Setup (MSTest)

### Create Project File

Create `Library Management System.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.0.0" />
    <PackageReference Include="MSTest.TestAdapter" Version="2.2.8" />
    <PackageReference Include="MSTest.TestFramework" Version="2.2.8" />
    <PackageReference Include="coverlet.collector" Version="3.1.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Library Management System\Library Management System.csproj" />
  </ItemGroup>
</Project>
```

### Example Test Class

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Library_Management_System.Service;

namespace Library_Management_System.Tests
{
    [TestClass]
    public class AuthenticationServiceTests
    {
        [TestMethod]
        public void Authenticate_ValidCredentials_ReturnsUser()
        {
            // Arrange
            var service = new AuthenticationService();
            
            // Act
            var user = service.Authenticate("admin@umindanao.edu.ph", "password", UserRole.Administrator);
            
            // Assert
            Assert.IsNotNull(user);
            Assert.AreEqual("admin@umindanao.edu.ph", user.Email);
        }
        
        [TestMethod]
        public void Authenticate_InvalidCredentials_ReturnsNull()
        {
            // Arrange
            var service = new AuthenticationService();
            
            // Act
            var user = service.Authenticate("invalid@email.com", "wrongpassword", UserRole.Administrator);
            
            // Assert
            Assert.IsNull(user);
        }
    }
}
```

---

## Option 3: Using NUnit

### Install NUnit

```bash
Install-Package NUnit
Install-Package NUnit3TestAdapter
```

### Example Test Class

```csharp
using NUnit.Framework;
using Library_Management_System.Service;

namespace Library_Management_System.Tests
{
    [TestFixture]
    public class SearchServiceTests
    {
        [Test]
        public void SearchBooks_ValidSearch_ReturnsResults()
        {
            // Arrange
            var service = new SearchService();
            
            // Act
            var results = service.SearchBooks("test");
            
            // Assert
            Assert.IsNotNull(results);
            Assert.IsInstanceOf<List<SearchService.SearchResult>>(results);
        }
    }
}
```

---

## Running Tests

### Visual Studio
- **Test Explorer**: View → Test Explorer
- **Run All**: Test → Run All Tests
- **Debug Tests**: Right-click test → Debug

### Command Line (MSTest)
```bash
dotnet test
```

### Command Line (NUnit)
```bash
nunit3-console.exe LibraryManagementSystem.Tests.dll
```

---

## Test Data Setup

### Database Setup for Tests

1. Create test database: `LibraryManagementDB_Test`
2. Run schema scripts on test database
3. Seed test data
4. Use test connection string in `App.config` for test project

### Test Isolation

- Use transactions that rollback after each test
- Or use a separate test database
- Clean up test data after tests

---

## Best Practices

1. **Arrange-Act-Assert Pattern**: Structure tests clearly
2. **Test Naming**: `MethodName_Scenario_ExpectedResult`
3. **One Assert Per Test**: Keep tests focused
4. **Mock Dependencies**: Use Moq for external dependencies
5. **Test Coverage**: Aim for 80%+ coverage

---

## Current Testing

The project includes `ServiceTests.cs` for manual testing:
- Run via `ServiceTests.RunAllTests()`
- Provides basic functionality verification
- Can be integrated into test project

---

**Note**: Unit test project setup is optional but recommended for production use.

