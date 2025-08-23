-- Data Migration Script: Transfer existing Activity data to new structure
-- Run this script AFTER applying the RefactorToNewActivityStructure EF migration

BEGIN TRANSACTION;

BEGIN TRY
    PRINT 'Starting data migration from Activities to new structure...'

    -- Step 1: Create unique activity definitions in Activities_New table
    -- Group existing activities by unique combinations and create templates
    INSERT INTO Activities_New (Name, SectionId, SchoolYear, Period, MaxPoints, Tag, Description, CreatedDate, UpdatedDate, IsActive)
    SELECT DISTINCT 
        a.ActivityName as Name,
        s.SectionId,
        s.SchoolYear,
        a.Period,
        MAX(a.MaxPoints) as MaxPoints,  -- Use the max points if there are variations
        COALESCE(a.Tag, 'Other') as Tag,
        'Migrated from legacy Activity table' as Description,
        MIN(GETUTCDATE()) as CreatedDate,
        GETUTCDATE() as UpdatedDate,
        1 as IsActive
    FROM Activities a
    INNER JOIN Students st ON a.StudentId = st.Id
    INNER JOIN Sections s ON st.SectionId = s.Id
    GROUP BY a.ActivityName, s.SectionId, s.SchoolYear, a.Period, COALESCE(a.Tag, 'Other')
    ORDER BY s.SchoolYear, s.Name, a.Period, a.ActivityName

    PRINT 'Created ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' activity definitions in Activities_New table'

    -- Step 2: Create student activity records in StudentActivities table
    INSERT INTO StudentActivities (StudentId, ActivityId, Points, Status, GithubLink, SubmissionDate, CreatedDate, UpdatedDate)
    SELECT 
        a.StudentId,
        an.ActivityId,
        a.Points,
        a.Status,
        a.GithubLink,
        CASE 
            WHEN a.Status = 'Turned in' THEN GETUTCDATE() 
            ELSE NULL 
        END as SubmissionDate,
        GETUTCDATE() as CreatedDate,
        GETUTCDATE() as UpdatedDate
    FROM Activities a
    INNER JOIN Students s ON a.StudentId = s.Id
    INNER JOIN Sections sec ON s.SectionId = sec.Id
    INNER JOIN Activities_New an ON 
        an.Name = a.ActivityName 
        AND an.SectionId = sec.Id 
        AND an.SchoolYear = sec.SchoolYear 
        AND an.Period = a.Period
        AND an.Tag = COALESCE(a.Tag, 'Other')

    PRINT 'Created ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' student activity records in StudentActivities table'

    -- Step 3: Verify the migration
    DECLARE @OriginalCount INT, @NewActivityCount INT, @NewStudentActivityCount INT

    SELECT @OriginalCount = COUNT(*) FROM Activities
    SELECT @NewActivityCount = COUNT(*) FROM Activities_New  
    SELECT @NewStudentActivityCount = COUNT(*) FROM StudentActivities

    PRINT 'Migration Summary:'
    PRINT '  Original Activities: ' + CAST(@OriginalCount AS VARCHAR(10))
    PRINT '  New Activity Templates: ' + CAST(@NewActivityCount AS VARCHAR(10))
    PRINT '  New Student Activities: ' + CAST(@NewStudentActivityCount AS VARCHAR(10))

    -- Verify that we haven't lost any data
    IF @OriginalCount != @NewStudentActivityCount
    BEGIN
        PRINT 'WARNING: Mismatch in record counts. Please review the migration.'
        -- Don't rollback automatically - let admin decide
    END
    ELSE
    BEGIN
        PRINT 'SUCCESS: All records migrated successfully!'
    END

    -- Step 4: Add sample rubric JSON for some activities (optional)
    -- This demonstrates how rubrics can be stored
    UPDATE Activities_New 
    SET RubricJson = '{
        "criteria": [
            {
                "name": "Code Quality",
                "weight": 0.4,
                "maxPoints": 40,
                "levels": [
                    {"score": 4, "description": "Excellent: Clean, well-documented, follows best practices"},
                    {"score": 3, "description": "Good: Mostly clean code with minor issues"},
                    {"score": 2, "description": "Fair: Functional but needs improvement"},
                    {"score": 1, "description": "Poor: Difficult to read, many issues"}
                ]
            },
            {
                "name": "Functionality",
                "weight": 0.4,
                "maxPoints": 40,
                "levels": [
                    {"score": 4, "description": "All requirements met perfectly"},
                    {"score": 3, "description": "Most requirements met"},
                    {"score": 2, "description": "Some requirements met"},
                    {"score": 1, "description": "Few requirements met"}
                ]
            },
            {
                "name": "Creativity/Innovation",
                "weight": 0.2,
                "maxPoints": 20,
                "levels": [
                    {"score": 4, "description": "Highly creative solution"},
                    {"score": 3, "description": "Some creative elements"},
                    {"score": 2, "description": "Standard approach"},
                    {"score": 1, "description": "Basic implementation"}
                ]
            }
        ],
        "totalPoints": 100,
        "gradingScale": {
            "A": {"min": 90, "max": 100},
            "B": {"min": 80, "max": 89},
            "C": {"min": 70, "max": 79},
            "D": {"min": 60, "max": 69},
            "F": {"min": 0, "max": 59}
        }
    }'
    WHERE Tag IN ('Assignment', 'Hands-on') 
    AND MaxPoints >= 50  -- Only add detailed rubrics for substantial activities

    PRINT 'Added sample rubrics to ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' activities'

    COMMIT TRANSACTION;
    PRINT 'Data migration completed successfully!'

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE()
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY()
    DECLARE @ErrorState INT = ERROR_STATE()
    
    PRINT 'Migration failed with error: ' + @ErrorMessage
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState)
END CATCH

-- Instructions for next steps:
/*
After running this script successfully:

1. Test the application with the new data structure
2. Update all application code to use NewActivity and StudentActivity entities
3. Once everything is working, you can drop the old Activities table:
   DROP TABLE Activities;
4. Rename Activities_New to Activities:
   EXEC sp_rename 'Activities_New', 'Activities';
   EXEC sp_rename 'PK_Activities_New', 'PK_Activities';
   -- Update foreign key names as needed

5. Update the ApplicationDbContext to use the renamed table
*/
