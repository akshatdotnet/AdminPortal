namespace PMS.Domain.Constants;

public static class DomainConstants
{
    public static class Project
    {
        public const int NameMaxLength = 150;
        public const int DescriptionMaxLength = 1000;
    }

    public static class Task
    {
        public const int TitleMaxLength = 200;
        public const int DescriptionMaxLength = 2000;
    }

    public static class User
    {
        public const int NameMaxLength = 100;
        public const int EmailMaxLength = 150;
    }

    public static class TimeLog
    {
        public const int NotesMaxLength = 500;
    }
}