namespace ClassMate.Data;

public class StudyLog
{
    public int Id {get; set;}

    public string UserId {get; set;} = "";

    public ApplicationUser User {get; set;} = null!;

    public DateTime DateOfLog {get; set;}

    public double Hours {get; set;}
    
}