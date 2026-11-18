# StackUnderflow

ClassMate is a student planner for managing courses and assignments.

## Database Setup - Gabriel

ClassMate uses Neon PostgreSQL. The application stores courses, assignments,
notes, study logs, study sessions, and user settings.

### Run ClassMate locally

1. In the Neon Console, open your development database branch. Click **Connect**,
   select **.NET**, and copy the connection string.
2. From the root of the `StackUnderflow` repository, save the connection string
   with .NET user secrets. Paste the string when PowerShell prompts you.

   ```powershell
   $connection = Read-Host "Paste your Neon .NET connection string"
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" $connection --project .\src\ClassMate\ClassMate.csproj
   ```

   Store connection strings in user secrets to keep database credentials out of Git.

3. Start the application.

   ```powershell
   dotnet run --project .\src\ClassMate\ClassMate.csproj --launch-profile http
   ```

4. Open [ClassMate](http://localhost:5184). If your development database is new,
   register a test account and add a course before creating an assignment.

ClassMate applies pending database migrations automatically at startup. To apply
them manually, install `dotnet-ef` if you need it, then run the database update
from the repository root.

```powershell
dotnet tool install --global dotnet-ef --version 10.0.13
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project .\src\ClassMate\ClassMate.csproj
```

## Features

Sign in to manage your courses and assignments.

### Course Management

- Add a course with a name, course code, instructor, color, semester, and year.
- View active and archived courses.
- Edit or delete an active course.
- Archive a course to move it out of the active list while keeping its information.
  Restore it when you want it back in the active list.

To add a course, open **Courses**, select **+**, enter the course information,
and save. The course name is required.

### Assignment Management

Students can create, view, edit, and delete assignments for their own courses.
Each saved assignment shows its title, course, due date, and priority badge.

1. Open **Assignments** from the sidebar.
2. Select a course, enter a title, and choose a due date. Select **Low**,
   **Medium**, or **High** priority. New assignments default to **Medium**.
3. Click **Save Assignment**. To update it later, click **Edit**, then
   **Save Changes**. Use **Cancel** to discard the edit.
4. Select **Due date** or **Priority** under **Sort assignments by**. Priority
   sorts **High**, then **Medium**, then **Low**. Assignments with the same
   priority appear in due-date order.
5. Use **Mark complete** or **Mark incomplete** to save a status change.
   Incomplete assignments due before today are highlighted and labeled
   **Overdue**. Completing one clears the highlight and label. Marking it
   incomplete restores them if the assignment is still overdue.
6. To remove an assignment, click **Delete**, then **Confirm delete**.

The course, title, and due date are required. A blank or whitespace-only title
prevents the assignment from being saved. Students can select only their own
courses and manage only their own assignments.

## Deployment

ClassMate uses Azure App Service for hosting and Neon PostgreSQL for its
database. Pushes to `main` trigger the GitHub Actions workflow at
`.github/workflows/deploy-azure.yml`.

For one-time Azure setup, run `az login` and `gh auth login`, then run
`bash scripts/azure-setup.sh` from the repository root.