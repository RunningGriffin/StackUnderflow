namespace ClassMate.Data;

public class StudySession
{
    public int Id {get; set;}

    public int CourseId {get; set;} 

    public Course Course {get; set;} = null!;

    public string UserId {get; set;} = "";

    public ApplicationUser User {get; set;} = null!;

    public DateTime StartTime {get; set;}

    public int DurationMinutes {get; set;}

    
}