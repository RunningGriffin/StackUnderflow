using Microsoft.EntityFrameworkCore;
namespace ClassMate.Data;


public enum Theme{
    Light,
    Dark
}

[Index(nameof(UserId), IsUnique=true)]
public class UserSettings
{
    public int Id {get; set;}

    public string UserId {get; set;} = "";

    public ApplicationUser User {get; set;} = null!;

    public Theme Theme {get; set;}= Theme.Light;

    public double HourWeeklyTimeGoal {get; set;}

    public double HourReminderLeadTime {get; set;}
    
}