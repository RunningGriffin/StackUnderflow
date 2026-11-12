using ClassMate.Data;
using Microsoft.EntityFrameworkCore;

namespace ClassMate.Services;

public class CourseService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CourseService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    // READ: Get the logged-in student's courses.
    public async Task<List<Course>> GetCoursesAsync(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        return await db.Courses
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    // CREATE or UPDATE a course.
    public async Task<bool> SaveCourseAsync(
        string userId, Course input, int? editingId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        if (editingId.HasValue)
        {
            var existing = await db.Courses
                .FirstOrDefaultAsync(c =>
                    c.Id == editingId.Value &&
                    c.UserId == userId);

            if (existing is null)
                return false;

            existing.Name = input.Name.Trim();
            existing.Code = input.Code;
            existing.Instructor = input.Instructor;
            existing.Color = input.Color;
            existing.Semester = input.Semester;
            existing.Year = input.Year;
        }
        else
        {
            var course = new Course
            {
                Name = input.Name.Trim(),
                Code = input.Code,
                Instructor = input.Instructor,
                Color = input.Color,
                Semester = input.Semester,
                Year = input.Year,
                UserId = userId,
                IsArchived = false
            };

            db.Courses.Add(course);
        }

        await db.SaveChangesAsync();
        return true;
    }

    // Delete a course 
    public async Task<bool> DeleteCourseAsync(
        int courseId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var course = await db.Courses
            .Include(c => c.Assignments)
            .Include(c => c.Notes)
            .Include(c => c.Sessions)
            .FirstOrDefaultAsync(c =>
                c.Id == courseId &&
                c.UserId == userId);

        if (course is null)
            return false;

        db.Assignments.RemoveRange(course.Assignments);
        db.Notes.RemoveRange(course.Notes);
        db.StudySessions.RemoveRange(course.Sessions);

        db.Courses.Remove(course);

        await db.SaveChangesAsync();
        return true;
    }

    // Aarchive or restore course
    public async Task<bool> SetArchiveStatusAsync(
        int courseId, string userId, bool isArchived)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var course = await db.Courses
            .FirstOrDefaultAsync(c =>
                c.Id == courseId &&
                c.UserId == userId);

        if (course is null)
            return false;

        course.IsArchived = isArchived;

        await db.SaveChangesAsync();
        return true;
    }
}
