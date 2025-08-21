 JSON Rubric Sample
[
  {
    "name": "Student class properties and methods",
    "points": 20,
    "keywords": [
      "FirstName",
      "LastName",
      "SetFirstName",
      "SetLastName",
      "Title",
      "Course",
      "Section",
      "Birthday",
      "SetGender",
      "Gender"
    ],
    "files": ["Models/Student.cs"]
  },
  {
    "name": "Gender enum defined",
    "points": 10,
    "keywords": ["enum", "Gender", "Unknown", "Male", "Female"],
    "files": ["Models/Student.cs"]
  },
  {
    "name": "Computed properties FullName and Age",
    "points": 15,
    "keywords": ["FullName", "Age", "Birthday", "Title", "FirstName", "LastName"],
    "files": ["Models/Student.cs"]
  },
  {
    "name": "Index.cshtml creates List<Student> with 10 entries",
    "points": 20,
    "keywords": ["Index.cshtml", "List<Student>", "new Student", "Add", "10"],
    "files": ["Views/Home/Index.cshtml"]
  },
  {
    "name": "Index.cshtml sets Student properties",
    "points": 10,
    "keywords": ["SetFirstName", "SetLastName", "SetGender", "Title", "Course", "Section", "Birthday"],
    "files": ["Views/Home/Index.cshtml"]
  },
  {
    "name": "Index.cshtml displays Student table",
    "points": 20,
    "keywords": [
      "table",
      "FullName",
      "Gender",
      "Course",
      "Section",
      "Birthday",
      "Age",
      "foreach"
    ],
    "files": ["Views/Home/Index.cshtml"]
  },
  {
    "name": "No controller logic used",
    "points": 5,
    "keywords": ["Index.cshtml only", "no controller"],
    "files": ["Views/Home/Index.cshtml"]
  }
]
