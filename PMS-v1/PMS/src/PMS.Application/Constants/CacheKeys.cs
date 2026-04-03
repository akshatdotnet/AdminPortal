namespace PMS.Application.Constants;

public static class CacheKeys
{
    public static class Projects
    {
        public const string Prefix = "projects:";
        public const string AllActive = "projects:active";
        public static string ById(int id) => $"projects:{id}";
    }

    public static class Users
    {
        public const string Prefix = "users:";
        public const string AllActive = "users:active";
    }

    public static class Tasks
    {
        public const string Prefix = "tasks:";
        public static string ById(int id) => $"tasks:{id}";
        public static string ByProject(int id) => $"tasks:project:{id}";
    }
}