namespace ClassMate.Data

{
    public class Course
    {
        public int Id {get; set;}

        public string UserId {get; set;} = "";

        public ApplicationUser User {get; set;} = null!;

        public string Name {get; set;} = "";

        public string? Code {get; set;}

        public string? Instructor {get; set;}

        public string Color {get; set;} = "";

        public string Semester {get; set;} = "Fall";

        public int Year {get; set;} = DateTime.Today.Year;

        public bool IsArchived {get; set;} = false;

        public List<Assignment> Assignments {get; set;} = [];

        public List<Note> Notes {get; set;} = [];

        public List<StudySession> Sessions {get; set;} = [];
    }
    
}