    
public async Task<List<ScheduleItem>> GetItemsAsync(
    DateTime rangeStart, DateTime rangeEnd, CancellationToken cancellationToken = default)
{
    var userId = await currentUser.GetRequiredUserIdAsync();

    using var scope = scopeFactory.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var earliestSessionStart = rangeStart - MaxSessionLength;

    var sessions = await db.StudySessions
        AsNoTracking()
        .Where(session => session.UserId == userId &&
                          session.StartTime >= earliestSessionStart &&
                          session.StartTime < rangeEnd)
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
                             assignment.DueDate >= rangeStart &&
                             assignment.DueDate < rangeEnd)
            .Select(assignment => new
            {
                assignment.Id,
                assignment.Title,
                assignment.DueDate,
                assignment.Priority,
                assignment.Completed,
                CourseName = assignment.Course.Name,
                CourseColor = assignment.Course.Color
            })
            .ToListAsync(cancellationToken);

    var items = new List<ScheduleItem>(sessions.Count + assignments.Count);

    foreach (var session in sessions)
    {
        var end = session.StartTime.AddMinutes(session.DurationMinutes);

        if (end <= rangeStart)
            continue;

        items.Add(new ScheduleItem(
            Id: session.Id,
            IsAssignment: false,
            Title: "Study session",
            Course: session.CourseName,
            CourseColor: session.CourseColor,
            Details: $"{session.StartTime:h:mm tt} – {end:h:mm tt} ({FormatDuration(session.DurationMinutes)})",
            Start: session.StartTime,
            End: end));
    }

    foreach (var assignment in assignments)
    {
        var details = $"Due {assignment.DueDate:MMM d, yyyy} · {assignment.Priority} priority";

        if (assignment.Completed)
            details += " · Complete";

        items.Add(new ScheduleItem(
            Id: assignment.Id,
            IsAssignment: true,
            Title: assignment.Title,
            Course: assignment.CourseName,
            CourseColor: assignment.CourseColor,
            Details: details,
            Start: assignment.DueDate,
            End: assignment.DueDate));
    }

    return items
        .OrderBy(item => item.Start)
        .ThenBy(item => item.Title)
        .ToList();
    }

private static string FormatDuration(int minutes)
    {
        var hours = minutes / 60;
        var rest = minutes % 60;

    return (hours, rest) switch
        {
            (0, _) => $"{rest} min",
            (_, 0) => $"{hours} h",
            _ => $"{hours} h {rest} min"
        };
    }
}