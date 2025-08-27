-- Sample data for testing the elective gradesheet application

-- Insert sample sections
INSERT INTO Sections (Name, SchoolYear, IsActive) VALUES 
('BSIT 31A1', '2024-2025', 1),
('BSIT 31A2', '2024-2025', 1),
('BSIT 32B1', '2024-2025', 1);

-- Insert sample students
INSERT INTO Students (LastName, FirstName, Email, SectionId) VALUES 
('Garcia', 'Juan', 'juan.garcia@example.com', 1),
('Santos', 'Maria', 'maria.santos@example.com', 1),
('Cruz', 'Pedro', 'pedro.cruz@example.com', 1),
('Reyes', 'Ana', 'ana.reyes@example.com', 2),
('Lopez', 'Carlos', 'carlos.lopez@example.com', 2);

-- Insert sample activities
INSERT INTO Activities (Tag, Period, StudentId, ActivityName, MaxPoints, Points, GithubLink, Status) VALUES 
('LAB1', 0, 1, 'Database Design', 100, 95, 'https://github.com/user/lab1', 'Completed'),
('LAB2', 0, 1, 'Entity Framework', 100, 88, 'https://github.com/user/lab2', 'Completed'),
('LAB1', 0, 2, 'Database Design', 100, 92, 'https://github.com/user2/lab1', 'Completed'),
('LAB2', 0, 2, 'Entity Framework', 100, 85, 'https://github.com/user2/lab2', 'Completed'),
('LAB1', 1, 3, 'Web API Development', 100, 78, 'https://github.com/user3/lab1', 'Completed'),
('LAB2', 1, 3, 'Authentication', 100, 82, 'https://github.com/user3/lab2', 'Completed');
