 JSON Rubric Sample
```JSON
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
```
### Prerequisites
To get started with this project, you will need the following tools installed:
* **.NET 8 SDK** or later ([Download here](https://dotnet.microsoft.com/download/dotnet/8.0))
* **SQL Server** (e.g., SQL Server Express or SQL Server LocalDB)
* An IDE like **Visual Studio** or Visual Studio Code

* ### Docker (Optional)

You can use Docker to run a containerized instance of SQL Server for this project.

1.  **Install Docker**: Ensure you have Docker Desktop installed on your machine. ([Download here](https://www.docker.com/products/docker-desktop/))
2.  **Run the SQL Server Container**: Based on the provided Docker container details, you can use the following `docker run` command to start a SQL Server instance:
    ```bash
    docker run -d --name sqlserver -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" -e "MSSQL_PID=developer" -p 1433:1433 mcr.microsoft.com/mssql/server:latest
    ```
    This command will:
      * `docker run -d`: Run the container in **detached mode** (in the background).
      * `--name sqlserver`: Name the container `sqlserver` for easy reference.
      * `-e "ACCEPT_EULA=Y"`: Set the environment variable to accept the End-User Licensing Agreement.
      * `-e "SA_PASSWORD=YourStrong!Passw0rd"`: Set the password for the `SA` (system administrator) user. **Note: You should change this to a strong password of your choice.**
      * `-e "MSSQL_PID=developer"`: Specify the product ID (PID) as `developer` for the developer edition of SQL Server.
      * `-p 1433:1433`: Map port `1433` on the host to port `1433` on the container.
      * `mcr.microsoft.com/mssql/server:latest`: Use the latest official Microsoft SQL Server image from Docker Hub.
  
   ### Installation and Setup

#### Database Setup Explained

The project uses Entity Framework Core for database management, which is configured through the `appsettings.json` file and managed by the `dotnet ef` command-line tools.

* **The Connection String**: The `appsettings.json` file stores the connection string under the key "DefaultConnection". This string provides all the necessary information for the application to connect to the SQL Server database, including the server name, database name, and authentication method.
* **Application Startup**: During application startup (in the `Program.cs` file), the connection string is read from `appsettings.json` and is used to configure the `ApplicationDbContext` with the SQL Server provider. This establishes the link between your application's data models and the physical database.
* **Applying Migrations**: The command `dotnet ef database update` is used to apply pending database migrations. This command reads the migration files and creates or updates the database schema (e.g., tables like `Students` and `Activities`) to match the models defined in the C# code. You must run this command from the **root project directory**, which is the folder containing the `.csproj` file.

#### Seeding Data and Importing (`.bacpac`)

You have two options for setting up the initial data: using a SQL script or a `.bacpac` file.

1.  **Using SQL Script (`seed_section.sql`):** This is the manual approach. After running the `dotnet ef database update` command, you can execute the `seed_section.sql` script to populate the `Sections` table.

2.  **Using a `.bacpac` file (`prelim-activities.dacpac`):** This is the recommended approach if you have an existing database. A `.bacpac` file is a compressed archive containing both the database schema and data. **If you import the database using the `prelim-activities.bacpac` file, you do not need to execute the following steps:**
    * `dotnet ef database update` (the schema and data are already included).
    * `seed_section.sql` (the sections data is already included).
    * You will also not need to use the "Upload Gradebook CSV" feature on the application's home page, as the database already contains student and activity records.

 ### Getting Started

To begin this activity, you'll need to fork and set up the repository.

1.  **Fork and Rename**: Fork this repository to your own GitHub account and rename the forked copy to **`MIDTERM_LAB1_Section_FirstName_LastName`**.
2.  **Clone and Branch**: Clone your renamed repository to your local machine and create a new branch named **`Dev`**. You will work on this new branch for all your changes.
    ```bash
    git clone https://github.com/YourUsername/MIDTERM_LAB1_Section_FirstName_LastName.git
    cd MIDTERM_LAB1_Section_FirstName_LastName
    git checkout -b Dev
    ```

The **root project directory** is the main `elective-2-gradesheet` folder containing the `.csproj` file and other core directories like `Controllers` and `Views`. All commands should be run from here.

-----

### Project Architecture

This is an ASP.NET Core **Model-View-Controller (MVC)** application.

  * **Models**: View models (`ActivityViewModel`) pass data, while entities (`Student`, `Activity`) define the database structure.
  * **Views**: The `.cshtml` files handle the UI (`Index.cshtml`, `Records.cshtml`, `StudentProfile.cshtml`).
  * **Controllers**: The `HomeController.cs` manages HTTP requests and business logic with the help of services.

-----

### `data-*` Global Attributes

These HTML5 attributes store custom data directly on elements. The application uses them to pass server-side data from views to client-side JavaScript for dynamic interactions like populating modals. To learn more about how they work, you can refer to the [MDN Web Docs](https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/data-*).

-----

### Installation & Setup

1.  **Prerequisites**: Install **.NET 8 SDK** ([Download here](https://dotnet.microsoft.com/download/dotnet/8.0)), **SQL Server**, and an IDE.
2.  **Database Connection**:
      * Update the "DefaultConnection" string in `appsettings.json` to point to your SQL Server instance.
      * The `Program.cs` file uses this connection string to set up the database context.
3.  **Database Initialization**: You have two options:
      * **Manual Setup**: Run `dotnet ef database update` from the root directory to create the schema, then execute `seed_section.sql` to populate the `Sections` table.
      * **Using a `.bacpac` file**: Recommended for pre-populated data. Importing `prelim-activities.bacpac` automatically sets up the schema, seeds the data, and includes activities, so you don't need to run `dotnet ef database update`, `seed_section.sql`, or use the CSV import feature.
4.  **Run**: Execute `dotnet run` from the root directory.

-----

### Instructions for Changes

Refactor the UI without touching the controller or model logic.

1.  **Move JavaScript**: Move the `<script>` block content from `Index.cshtml`, `Records.cshtml`, and `StudentProfile.cshtml` into new files in `wwwroot/js/views/`. **Make sure to reference these new files in the corresponding layout where they are used.**

2.  **Implement Partial Views**: Extract reusable components into `Views/Shared/Partials/`.

      * Move the activity modal's HTML and JavaScript from `StudentProfile.cshtml` into `_EditActivityModalPartial.cshtml`. This partial view does not require a model.
      * Move the filter and bulk add forms from `StudentProfile.cshtml` into `_StudentProfileFormsPartial.cshtml`. This partial view will require the `StudentProfileViewModel`.

3.  **Additional Tasks**:

      * **Student Profile Tooltip**: Add a tooltip that says "Click to Open Profile" to the student's name on the `Records.cshtml` page. You can do this by adding `data-bs-toggle="tooltip"` and `title="Click to Open Profile"` to the anchor tag (`<a>`).
      * **Navigation**: Add a "Back to Records" link or button on the `StudentProfile.cshtml` page to easily return to the list of records.
      * **Default Page**: Change the application's default landing page from `Home/Index` to `Home/Records`. This can be done by modifying the routing configuration (e.g., in `Program.cs`).
      * **Filter Functionality**: In `Records.cshtml`, the current filter form resets to "All Sections" and "All Periods" upon submission. Add a dedicated **"Reset Filter"** button next to the existing filter button. This new button should clear all filter inputs (`searchString`, `sectionId`, and `period`) and submit the form to display all records.
      * **Design**: Feel free to improve the overall design and layout of the pages to be your own.
      * **Blog/Reflection**: Create a new Controller named **`DevController`** with an **`Index`** view. In the `Index.cshtml` file for this controller, write a reflection or blog post about your findings and experiences while working on this activity.
  
 

