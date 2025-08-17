-- This script adds initial data to the Sections table.
-- It first checks if the sections already exist to prevent duplicates.
IF NOT EXISTS (SELECT 1 FROM Sections WHERE Name = 'BSIT-31A1')
BEGIN
    INSERT INTO Sections (Name, SchoolYear, IsActive) 
    VALUES ('BSIT-31A1', '2025-2026', 1);
END

IF NOT EXISTS (SELECT 1 FROM Sections WHERE Name = 'BSIT-31A2')
BEGIN
    INSERT INTO Sections (Name, SchoolYear, IsActive) 
    VALUES ('BSIT-31A2', '2025-2026', 1);
END

IF NOT EXISTS (SELECT 1 FROM Sections WHERE Name = 'BSIT-31A3')
BEGIN
    INSERT INTO Sections (Name, SchoolYear, IsActive) 
    VALUES ('BSIT-31A3', '2025-2026', 1);
END