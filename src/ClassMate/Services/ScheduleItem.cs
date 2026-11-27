namespace ClassMate.Services;

public sealed record ScheduleItem(
    int Id,
    bool IsAssignment,
    string Title,
    string Course,
    string CourseColor,
    string Details,
    DateTime Start,
    DateTime End)
{
    public bool OccursOn(DateTime day)
    {
        var date = day.Date;
        var last = End > Start ? End.AddTicks(-1) : Start;

        return Start.Date <= date && date <= last.Date;
    }
}