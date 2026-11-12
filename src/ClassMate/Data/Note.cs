namespace ClassMate.Data;

public class Note
{
    public int Id {get; set;}

    public int CourseId {get; set;} 

    public Course Course {get; set;} = null!;

    public string UserId {get; set;} = "";

    public ApplicationUser User {get; set;} = null!;

    public string Title {get; set;} ="";

    public string Content {get; set;} ="";

    
}