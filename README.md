# StackUnderflow

## Database Setup - Gabriel

- We have 6 tables created to organize Courses, Assignments, Notes, StudyLogs, StudySessions and the usersSettings.

### Important Commands

1. Install the EF Core Tool:

- dotnet tool install --global dotnet-ef --version 10.0.13

2. Go to project folder

- cd src/ClassMate

3. Create or update the database

- dotnet ef database update

#### Features
 1. Course Management: Allows user to add, view, edit, delete, and archive their courses.

***All of these are only available while user is logged in****

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


