namespace ClassMate.Data;

public enum Priority{
    High,
    Medium,
    Low

}

public class Assignment
{
    public int Id {get; set;}
    
    public int CourseId {get; set;} 

    public Course Course {get; set;} = null!;

    public string UserId {get; set;} = "";

    public ApplicationUser User {get; set;} = null!;

    public string Title {get; set;} = "";

    public DateTime DueDate {get; set;}

    public Priority Priority {get; set;} = Priority.Medium;

    public bool Completed {get; set;}= false;


    
}