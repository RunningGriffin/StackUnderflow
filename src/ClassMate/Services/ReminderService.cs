using System.Text.RegularExpressions;
using ClassMate.Data;
using Microsoft.EntityFrameworkCore;

namespace ClassMate.Services;

public sealed record Reminder(
    int Id,
    bool IsAssignment,
    string Title,
    string Course,
    string CourseColor,
    DateTime DueAt,
    DateTime? EndsAt);

public sealed record ReminderSnapshot(double LeadHours, List<Reminder> Items);

public sealed class ReminderService(IServiceScopeFactory scopeFactory, CurrentUser currentUser)
{
    public const double DefaultLeadHours = 24;
    public const double MaxLeadHours = 24 * 30;
    private static readonly Regex HexColor = new("^#[0-9a-fA-F]{3,8}$", RegexOptions.Compiled);
    public async Task<double> GetLeadHoursAsync(CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetRequiredUserIdAsync();

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await ReadLeadHoursAsync(db, userId, cancellationToken);
    }

    public async Task SaveLeadHoursAsync(double hours, CancellationToken cancellationToken = default)
    {
        if (hours <= 0 || hours > MaxLeadHours)
            throw new ArgumentOutOfRangeException(nameof(hours), hours, "Lead time is out of range");
        
        var userId = await currentUser.GetRequiredUserIdAsync();

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var settings = await db.UsersSettings
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (settings is null)
        {
            settings = new UserSettings { UserId = userId };
            db.UsersSettings.Add(settings);
        }

        settings.HourReminderLeadTime = hours;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ReminderSnapshot> GetUpcomingAsync(
        DateTime now, CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetRequiredUserIdAsync();

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var leadHours = await ReadLeadHoursAsync(db, userId, cancellationToken);
        var until = now.AddHours(leadHours);

        var sessions = await db.StudySessions
            .AsNoTracking()
            .Where(session => session.UserId == userId &&
                              session.StartTime > now &&
                              session.StartTime <= until)
            .Select(session => new
            {
                session.Id,
                session.StartTime,
                session.DurationMinutes,
                CourseName = session.Course.Name,
                CourseColor = session.Course.Color
            })
            .ToListAsync(cancellationToken);

        var assignments = await db.Assignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == userId &&
                                 !assignment.Completed &&
                                 assignment.DueDate >= now.Date &&
                                 assignment.DueDate < until.Date.AddDays(1))
            .Select(assignment => new
            {
                assignment.Id,
                assignment.Title,
                assignment.DueDate,
                CourseName = assignment.Course.Name,
                CourseColor = assignment.Course.Color
            })
            .ToListAsync(cancellationToken);

        var items = new List<Reminder>(sessions.Count + assignments.Count);

        foreach (var session in sessions)
        {
            items.Add(new Reminder(
                session.Id,
                IsAssignment: false,
                Title: "Study session",
                Course: session.CourseName,
                CourseColor: SafeColor(session.CourseColor),
                DueAt: session.StartTime,
                EndsAt: session.StartTime.AddMinutes(session.DurationMinutes)));
        }

        foreach (var assignment in assignments)
        {
            var dueAt = assignment.DueDate.Date.AddDays(1).AddMinutes(-1);

            if (dueAt <= now || dueAt > until)
                continue;
            
            items.Add(new Reminder(
                assignment.Id,
                IsAssignment: true,
                Title: assignment.Title,
                Course: assignment.CourseName,
                CourseColor: SafeColor(assignment.CourseColor),
                DueAt: dueAt,
                EndsAt: null));
        }

        return new ReminderSnapshot(
            leadHours,
            items.OrderBy(item => item.DueAt).ThenBy(item => item.Title).ToList());
    }

    public static string DescribeLead(double hours)
    {
        if (hours >= 24 && hours % 24 == 0)
        {
            var days = (int)(hours / 24);
            return days == 1 ? "1 day" : $"{days} days";
        }

        return hours == 1 ? "1 hour" : $"{hours:0.##} hours";
    }

    private static async Task<double> ReadLeadHoursAsync(
        ApplicationDbContext db, string userId, CancellationToken cancellationToken)
    {
        var hours = await db.UsersSettings
            .AsNoTracking()
            .Where(settings => settings.UserId == userId)
            .Select(settings => settings.HourReminderLeadTime)
            .FirstOrDefaultAsync(cancellationToken);

        return hours > 0 ? hours : DefaultLeadHours;
    }

    private static string SafeColor(string? color) =>
        color is not null && HexColor.IsMatch(color) ? color : "var(--bs-secondary)";
}