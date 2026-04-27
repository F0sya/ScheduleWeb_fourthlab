namespace ScheduleWeb.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        public string FullName { get; set; }

        public List<Lesson> Lessons { get; set; } = new List<Lesson>();
    }

    public class StudyGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Lesson> Lessons { get; set; } = new List<Lesson>();
    }

    public class Lesson
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Subject { get; set; }
        public string Time { get; set; }
        public string Room { get; set; }
        public int Week { get; set; }
        public string LessonType { get; set; }

        public int TeacherId { get; set; }
        public Teacher Teacher { get; set; }

        public int StudyGroupId { get; set; }
        public StudyGroup Group { get; set; }
    }
}