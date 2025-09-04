# Bug Fix: Duplicate ActivityTemplate Creation Issue

## Problem Description

When clicking "Save changes" on the StudentProfile modal, the `UpdateActivityAsync` method was creating new `ActivityTemplate` records instead of properly identifying and updating existing ones. This occurred both in SQLite and SQL Server implementations.

## Root Cause Analysis

The issue was in the `UpdateActivityAsync` method in both `GradeDbLiteService.cs` and `IGradeService.cs` (SQL Server implementation). When a user clicked "Fix" on a missing activity:

1. `data-activity-id="0"` was passed to the modal
2. This triggered the `else` branch in the method (lines 310-348 in SQLite, 325-363 in SQL Server)
3. The method searched for existing `ActivityTemplate` records using **case-sensitive** string comparison
4. **Case sensitivity issue**: `at.Name == activityName` would fail if there were case differences
5. **Whitespace issue**: Leading/trailing spaces could prevent matches
6. When no match was found, a new `ActivityTemplate` was created instead of using the existing one

## Fixes Applied

### 1. Case-Insensitive and Trimmed String Comparison ✅
**Files Modified:** 
- `Services/GradeDbLiteService.cs` (line 322)
- `Services/IGradeService.cs` (line 337)

**Change:**
```csharp
// BEFORE (case-sensitive)
activityTemplate = await _context.ActivityTemplates
    .FirstOrDefaultAsync(at => at.Name == activityName &&
                             at.Period == period &&
                             at.SectionId == student.SectionId);

// AFTER (case-insensitive, trimmed)
var normalizedActivityName = activityName.Trim();
activityTemplate = await _context.ActivityTemplates
    .FirstOrDefaultAsync(at => at.Name.Trim().ToLower() == normalizedActivityName.ToLower() &&
                             at.Period == period &&
                             at.SectionId == student.SectionId);
```

### 2. Database Unique Constraint ✅
**Files Modified:**
- `Data/ApplicationDbLiteContext.cs` (line 58)
- `Data/ApplicationDbContext.cs` (line 38)

**Change:** Added unique composite index on `(SectionId, Period, Name)` to prevent duplicate ActivityTemplate records at the database level.

**SQLite:**
```csharp
// Unique composite index to prevent duplicate activity templates
entity.HasIndex(at => new { at.SectionId, at.Period, at.Name }).IsUnique();
```

**SQL Server:**
```csharp
// Add unique constraint on ActivityTemplate to prevent duplicates
modelBuilder.Entity<ActivityTemplate>()
    .HasIndex(at => new { at.SectionId, at.Period, at.Name })
    .IsUnique();
```

### 3. Debug Logging ✅
**Files Modified:**
- `Services/GradeDbLiteService.cs` (lines 323, 333, 343)

**Added console logging to track:**
- When ActivityTemplate searches are performed
- Whether existing templates are found or new ones are created
- Template IDs for debugging purposes

### 4. Entity Framework Migration ✅
**Files Created:**
- `Data/DesignTimeDbContextFactory.cs` - Design-time context factory for migrations
- `Migrations/20250904041758_AddUniqueConstraintToActivityTemplate.cs` - SQL Server migration

The migration adds the unique constraint: `IX_ActivityTemplates_SectionId_Period_Name`

## Testing Recommendations

1. **Test case sensitivity**: Create an activity with name "Lab1", then try to update it using "lab1" or "LAB1"
2. **Test whitespace handling**: Create an activity with name "Assignment 1", then try to update with " Assignment 1 " (with spaces)
3. **Test SQL Server migration**: Switch to SQL Server provider and run `dotnet ef database update` to apply the unique constraint
4. **Test SQLite**: Delete and recreate the SQLite database to apply the unique constraint automatically

## Database Impact

### For SQLite Users:
- The unique constraint will be applied when the database is recreated (using `EnsureCreatedAsync()`)
- No manual migration required

### For SQL Server Users:
- Run `dotnet ef database update` to apply the migration
- The migration will add the unique constraint without data loss

## Benefits

1. **No more duplicate ActivityTemplate records** - The unique constraint prevents database-level duplicates
2. **Better string matching** - Case-insensitive and trimmed comparisons handle user input variations
3. **Debugging capability** - Console logging helps identify issues in development
4. **Data integrity** - Database constraints ensure consistency

## Files Modified

1. `Services/GradeDbLiteService.cs` - Fixed SQLite implementation
2. `Services/IGradeService.cs` - Fixed SQL Server implementation  
3. `Data/ApplicationDbLiteContext.cs` - Added unique constraint for SQLite
4. `Data/ApplicationDbContext.cs` - Added unique constraint for SQL Server
5. `Data/DesignTimeDbContextFactory.cs` - Created design-time factory for EF migrations
6. `Migrations/20250904041758_AddUniqueConstraintToActivityTemplate.cs` - SQL Server migration

## Notes

- The debug logging uses `Console.WriteLine()` and should be replaced with proper logging (ILogger) in production
- The unique constraint will throw an exception if duplicate records are attempted to be inserted, which is the desired behavior
- Both database providers (SQLite and SQL Server) are now protected against this issue
