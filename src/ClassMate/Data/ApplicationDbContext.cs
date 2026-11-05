using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClassMate.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Course> Courses {get; set;}
    public DbSet<Assignment> Assignments {get; set;}
    public DbSet<Note> Notes{get; set;}
    public DbSet<StudySession> StudySessions{get; set;}
    public DbSet<StudyLog> StudyLogs{get; set;}
    public DbSet<UserSettings> UsersSettings{get; set;}

}
