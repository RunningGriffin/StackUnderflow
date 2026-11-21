# StackUnderflow

## Database Setup - Gabriel

- We have 6 tables created to organize Courses, Assignments, Notes, StudyLogs, StudySessions and the usersSettings.

### Important Commands

1. Install the EF Core Tool:

- dotnet tool install --global dotnet-ef --version 10.0.13

2. Go to project folder

- cd src/ClassMate

3. Point the app at your Neon database (use the **.NET** format from Neon's "Connect" dialog, ideally a separate dev branch):

- dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Database=...;Username=...;Password=...;SSL Mode=Require"

4. Create or update the database (the app also applies migrations automatically on startup)

- dotnet ef database update

### Features
 1. Course Management: Allows user to add, view, edit, delete, and archive their courses.

    Notes: All of these are only available while user is logged in

    Features: 
    -Add Courses: Add course title, professor, course code, choose the course color, year, and semester.
    -View Courses: View courses already added, current and archived.
    -Edit Courses: Be able to edit any course that is active. User can change any of the features (ex:title, code, color, etc)
    -Delete Courses: Delete any course that is created. 
    -Archive Courses: Keeps the course and associated information but marks course as inactive. Has a restore option that moves it back to active courses.

    How to Use: 
    1.Navigate to Courses page.
    2.Select the option to add new course (circle with "+")
    3.Enter course information and push save.
    4.View the course below.
    5.Use the available edit, delete, or archive buttons to manage active course.

    Validation:
    -Course Name: Required
    -Course Management: Checks to see if user is logged in.

2. Note Management: Allows user to add, view, edit, and delete their notes. It allows user to filter notes by course and be able to access notes in the course page. 

   Notes: All of these are only available while user is logged in

    Features: 
    -Add Note: Add note title, content, and select related course.
    -View Notes: View notes already added. Expand note to see notes longer than 100 characters. Notes are available to view when in course tab.
    -Filter Notes: Filter notes by course.
    -Edit Notes: Be able to edit any note. User can change any of the features (ex:title, content, course)
    -Delete Notes: Delete any note that is created. 

    How to Use: 
    1.Navigate to Notes page.
    2.Select the option to add new note (circle with "+")
    3.Enter note information and push save.
    4.View the course below.
    5.Use the available edit, or delete buttons to manage created notes.

    Validation:
    -Note Title: Required
    -Note Course Selection: Required
    -Note Management: Checks to see if user is logged in.



