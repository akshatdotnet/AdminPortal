using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Domain.Entities;
using PMS.Domain.Enums;
using TaskStatus = PMS.Domain.Enums.TaskStatus;

namespace PMS.Infrastructure.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
    {
        try
        {
            await db.Database.MigrateAsync();

            if (await db.Users.AnyAsync()) return;

            logger.LogInformation("Seeding database...");

            var users = await SeedUsersAsync(db);
            var projects = await SeedProjectsAsync(db);
            var tasks = await SeedTasksAsync(db, projects, users);
            await SeedTimeLogsAsync(db, tasks);

            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    // ── Users ────────────────────────────────────────────────────────────────

    private static async Task<List<User>> SeedUsersAsync(ApplicationDbContext db)
    {
        var users = new List<User>
        {
            new() { FirstName = "Amit",   LastName = "Sharma",  Email = "amit.dev@pms.dev",      Role = UserRole.Developer.ToString(),     IsActive = true },
            new() { FirstName = "Neha",   LastName = "Verma",   Email = "neha.dev@pms.dev",      Role = UserRole.Developer.ToString(),     IsActive = true },
            new() { FirstName = "Rahul",  LastName = "Patel",   Email = "rahul.dev@pms.dev",     Role = UserRole.Developer.ToString(),     IsActive = true },
            new() { FirstName = "Priya",  LastName = "Singh",   Email = "priya.qa@pms.dev",      Role = UserRole.QA.ToString(),            IsActive = true },
            new() { FirstName = "Karan",  LastName = "Mehta",   Email = "karan.pm@pms.dev",      Role = UserRole.ProjectManager.ToString(),IsActive = true },
            new() { FirstName = "Sneha",  LastName = "Iyer",    Email = "sneha.lead@pms.dev",    Role = UserRole.ProjectLead.ToString(),   IsActive = true },
            new() { FirstName = "Rohit",  LastName = "Agarwal", Email = "rohit.ba@pms.dev",      Role = UserRole.BusinessAnalyst.ToString(),IsActive = true },
            new() { FirstName = "Vikas",  LastName = "Reddy",   Email = "vikas.tech@pms.dev",    Role = UserRole.TechHead.ToString(),      IsActive = true },
            new() { FirstName = "Anjali", LastName = "Kapoor",  Email = "anjali.sales@pms.dev",  Role = UserRole.Sales.ToString(),         IsActive = true },
            new() { FirstName = "Pooja",  LastName = "Nair",    Email = "pooja.marketing@pms.dev",Role = UserRole.Marketing.ToString(),    IsActive = true },
            new() { FirstName = "Client", LastName = "One",     Email = "client1@company.com",   Role = UserRole.Client.ToString(),        IsActive = true },
            new() { FirstName = "Client", LastName = "Two",     Email = "client2@company.com",   Role = UserRole.Client.ToString(),        IsActive = true },
        };

        await db.Users.AddRangeAsync(users);
        await db.SaveChangesAsync();
        return users;
    }

    // ── Projects ─────────────────────────────────────────────────────────────

    private static async Task<List<Project>> SeedProjectsAsync(ApplicationDbContext db)
    {
        var projects = new List<Project>
        {
            new() { Name = "CRM & Customer Engagement Platform",      Description = "Manage leads, sales pipeline, and customer lifecycle.",              StartDate = DateTime.UtcNow.AddDays(-40), EndDate = DateTime.UtcNow.AddDays(120), Status = ProjectStatus.Active },
            new() { Name = "Omnichannel E-Commerce Marketplace",      Description = "Multi-vendor platform with payments and logistics integration.",     StartDate = DateTime.UtcNow.AddDays(-30), EndDate = DateTime.UtcNow.AddDays(150), Status = ProjectStatus.Active },
            new() { Name = "AI-Powered Chatbot Support System",       Description = "NLP-based chatbot for customer service automation.",                StartDate = DateTime.UtcNow.AddDays(-20), EndDate = DateTime.UtcNow.AddDays(90),  Status = ProjectStatus.Active },
            new() { Name = "HR & Payroll Management System",          Description = "Employee lifecycle, payroll, and attendance tracking.",             StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(120), Status = ProjectStatus.Active },
            new() { Name = "Digital Banking & Wallet App",            Description = "Secure mobile banking with UPI and payment gateway.",               StartDate = DateTime.UtcNow.AddDays(-60), EndDate = DateTime.UtcNow.AddDays(60),  Status = ProjectStatus.Active },
            new() { Name = "Inventory & Warehouse Automation System", Description = "Real-time stock tracking with barcode/RFID support.",               StartDate = DateTime.UtcNow.AddDays(-25), EndDate = DateTime.UtcNow.AddDays(100), Status = ProjectStatus.Active },
            new() { Name = "SaaS Project Management Tool",            Description = "Task tracking, team collaboration, and reporting dashboard.",        StartDate = DateTime.UtcNow.AddDays(-35), EndDate = DateTime.UtcNow.AddDays(140), Status = ProjectStatus.Active },
            new() { Name = "Healthcare Telemedicine Platform",        Description = "Online doctor consultation with video and prescriptions.",          StartDate = DateTime.UtcNow.AddDays(-50), EndDate = DateTime.UtcNow.AddDays(180), Status = ProjectStatus.Active },
            new() { Name = "Online Learning & LMS Platform",          Description = "Course management with video streaming and quizzes.",               StartDate = DateTime.UtcNow.AddDays(-15), EndDate = DateTime.UtcNow.AddDays(110), Status = ProjectStatus.Active },
            new() { Name = "AI-Based Resume Screening System",        Description = "Automated CV filtering using machine learning.",                    StartDate = DateTime.UtcNow.AddDays(-5),  EndDate = DateTime.UtcNow.AddDays(80),  Status = ProjectStatus.Active },
            new() { Name = "Microservices API Gateway",               Description = "Centralized gateway for routing and authentication.",               StartDate = DateTime.UtcNow.AddDays(-45), EndDate = DateTime.UtcNow.AddDays(60),  Status = ProjectStatus.Active },
            new() { Name = "Real-Time Chat & Collaboration App",      Description = "Team messaging system with file sharing.",                          StartDate = DateTime.UtcNow.AddDays(-20), EndDate = DateTime.UtcNow.AddDays(90),  Status = ProjectStatus.Active },
            new() { Name = "DevOps CI/CD Automation Platform",        Description = "Pipeline automation with Docker and Kubernetes.",                   StartDate = DateTime.UtcNow.AddDays(-70), EndDate = DateTime.UtcNow.AddDays(50),  Status = ProjectStatus.Active },
            new() { Name = "Logistics & Fleet Tracking System",       Description = "GPS tracking and route optimization for delivery.",                 StartDate = DateTime.UtcNow.AddDays(-25), EndDate = DateTime.UtcNow.AddDays(120), Status = ProjectStatus.Active },
            new() { Name = "Food Delivery & Aggregator Platform",     Description = "Restaurant listing, ordering, and delivery tracking.",              StartDate = DateTime.UtcNow.AddDays(-30), EndDate = DateTime.UtcNow.AddDays(100), Status = ProjectStatus.Active },
            new() { Name = "Social Media Analytics Dashboard",        Description = "Track engagement, trends, and user insights.",                     StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(90),  Status = ProjectStatus.Active },
            new() { Name = "Online Booking & Reservation System",     Description = "Hotel, travel, and ticket booking engine.",                         StartDate = DateTime.UtcNow.AddDays(-40), EndDate = DateTime.UtcNow.AddDays(130), Status = ProjectStatus.Active },
            new() { Name = "Cybersecurity Threat Detection System",   Description = "Monitor and detect anomalies in real-time.",                       StartDate = DateTime.UtcNow.AddDays(-60), EndDate = DateTime.UtcNow.AddDays(150), Status = ProjectStatus.Active },
            new() { Name = "Blockchain-Based Payment System",         Description = "Secure and decentralized transaction processing.",                  StartDate = DateTime.UtcNow.AddDays(-80), EndDate = DateTime.UtcNow.AddDays(60),  Status = ProjectStatus.Active },
            new() { Name = "IoT Smart Home Automation Platform",      Description = "Control devices with mobile and cloud integration.",                StartDate = DateTime.UtcNow.AddDays(-35), EndDate = DateTime.UtcNow.AddDays(120), Status = ProjectStatus.Active },
            new() { Name = "AI Recommendation Engine",               Description = "Personalized suggestions for products and content.",                StartDate = DateTime.UtcNow.AddDays(-15), EndDate = DateTime.UtcNow.AddDays(90),  Status = ProjectStatus.Active },
            new() { Name = "Document Management System (DMS)",        Description = "Secure storage, search, and version control.",                      StartDate = DateTime.UtcNow.AddDays(-25), EndDate = DateTime.UtcNow.AddDays(100), Status = ProjectStatus.Active },
            new() { Name = "Subscription Billing SaaS Platform",      Description = "Recurring billing and invoicing system.",                           StartDate = DateTime.UtcNow.AddDays(-20), EndDate = DateTime.UtcNow.AddDays(110), Status = ProjectStatus.Active },
            new() { Name = "Recruitment & Applicant Tracking System", Description = "Manage hiring pipeline and interviews.",                            StartDate = DateTime.UtcNow.AddDays(-30), EndDate = DateTime.UtcNow.AddDays(120), Status = ProjectStatus.Active },
            new() { Name = "Online Gaming Backend Platform",          Description = "Multiplayer game server with leaderboard.",                         StartDate = DateTime.UtcNow.AddDays(-50), EndDate = DateTime.UtcNow.AddDays(140), Status = ProjectStatus.Active },
            new() { Name = "Video Streaming Platform",               Description = "OTT platform with adaptive streaming.",                             StartDate = DateTime.UtcNow.AddDays(-60), EndDate = DateTime.UtcNow.AddDays(180), Status = ProjectStatus.Active },
            new() { Name = "AI Fraud Detection System",              Description = "Detect suspicious financial transactions.",                         StartDate = DateTime.UtcNow.AddDays(-40), EndDate = DateTime.UtcNow.AddDays(100), Status = ProjectStatus.Active },
            new() { Name = "Data Warehouse & BI Dashboard",          Description = "ETL pipelines with reporting and analytics.",                       StartDate = DateTime.UtcNow.AddDays(-70), EndDate = DateTime.UtcNow.AddDays(150), Status = ProjectStatus.Active },
            new() { Name = "Event Management Platform",              Description = "Manage events, tickets, and attendees.",                            StartDate = DateTime.UtcNow.AddDays(-15), EndDate = DateTime.UtcNow.AddDays(90),  Status = ProjectStatus.Active },
            new() { Name = "AI Voice Assistant Integration System",  Description = "Voice-enabled commands using NLP.",                                StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(80),  Status = ProjectStatus.Active },
        };

        await db.Projects.AddRangeAsync(projects);
        await db.SaveChangesAsync();
        return projects;
    }

    // ── Tasks ─────────────────────────────────────────────────────────────────

    private static readonly Dictionary<string, List<(string Title, string Description, TaskPriority Priority)>> PhaseTemplates = new()
    {
        ["Planning & Requirement"] =
        [
            ("Requirement Gathering", "Collect business requirements",  TaskPriority.High),
            ("Stakeholder Meeting",   "Discuss project goals",          TaskPriority.High),
            ("Define Scope",          "Finalize project scope",         TaskPriority.High),
            ("Create BRD",            "Document requirements",          TaskPriority.High),
            ("Sprint Planning",       "Plan initial sprint",            TaskPriority.Medium),
        ],
        ["Design (HLD/LLD)"] =
        [
            ("Architecture Design", "High-level system design", TaskPriority.High),
            ("Database Design",     "ER diagram & schema",      TaskPriority.High),
            ("API Design",          "Define endpoints",         TaskPriority.High),
            ("UI/UX Design",        "Wireframes and mockups",   TaskPriority.Medium),
            ("LLD Preparation",     "Detailed design",          TaskPriority.High),
        ],
        ["Setup & Architecture"] =
        [
            ("Setup Repository",       "Initialize Git",        TaskPriority.High),
            ("Setup CI/CD",            "Pipeline setup",        TaskPriority.High),
            ("Configure Environments", "Dev/QA/Prod setup",     TaskPriority.High),
            ("Setup Authentication",   "Auth system",           TaskPriority.High),
            ("Setup Logging",          "Logging framework",     TaskPriority.Medium),
        ],
        ["Backend Development"] =
        [
            ("User Module",        "CRUD users",               TaskPriority.High),
            ("Project Module",     "Manage projects",          TaskPriority.High),
            ("Task Module",        "Task operations",          TaskPriority.High),
            ("Time Tracking Module","Track time logs",         TaskPriority.High),
            ("API Development",    "Core APIs",                TaskPriority.High),
            ("Validation",         "Input validations",        TaskPriority.High),
            ("Exception Handling", "Global error handling",    TaskPriority.High),
            ("Unit Testing",       "Backend tests",            TaskPriority.High),
        ],
        ["Frontend Development"] =
        [
            ("Setup Frontend",    "Initialize UI project", TaskPriority.High),
            ("Login UI",          "Authentication screens", TaskPriority.High),
            ("Dashboard UI",      "Overview screen",       TaskPriority.High),
            ("Project UI",        "Project screens",       TaskPriority.High),
            ("Task UI",           "Task management UI",    TaskPriority.High),
            ("Timer UI",          "Start/Stop timer",      TaskPriority.High),
            ("API Integration",   "Connect APIs",          TaskPriority.High),
            ("Responsive Design", "Mobile support",        TaskPriority.Medium),
        ],
        ["Integration"] =
        [
            ("Frontend-Backend Integration", "Connect UI with APIs",       TaskPriority.High),
            ("Third-party Integration",       "Payment/Email APIs",         TaskPriority.Medium),
            ("Data Sync",                     "Ensure data consistency",    TaskPriority.Medium),
        ],
        ["Testing & QA"] =
        [
            ("Write Test Cases",    "Prepare QA scenarios",  TaskPriority.High),
            ("Functional Testing",  "Feature testing",       TaskPriority.High),
            ("Integration Testing", "Module interaction",    TaskPriority.High),
            ("Regression Testing",  "Check stability",       TaskPriority.High),
            ("Bug Fixing",          "Resolve issues",        TaskPriority.High),
        ],
        ["Security & Performance"] =
        [
            ("Security Testing",   "Vulnerability checks", TaskPriority.High),
            ("Performance Testing","Load testing",         TaskPriority.Medium),
            ("Optimize Queries",   "DB optimization",      TaskPriority.Medium),
            ("Caching",            "Implement caching",    TaskPriority.Medium),
        ],
        ["Deployment & DevOps"] =
        [
            ("Prepare Release",    "Release planning",       TaskPriority.Medium),
            ("Deploy to Staging",  "Staging deployment",     TaskPriority.High),
            ("Smoke Testing",      "Post-deploy checks",     TaskPriority.High),
            ("Deploy to Production","Go live",               TaskPriority.High),
            ("Setup Backup",       "Backup strategy",        TaskPriority.High),
        ],
        ["Monitoring & Maintenance"] =
        [
            ("Monitor Logs",           "Track logs",       TaskPriority.Medium),
            ("Fix Production Issues",  "Resolve live bugs",TaskPriority.High),
            ("Performance Monitoring", "Track metrics",    TaskPriority.Medium),
            ("User Feedback",          "Collect feedback", TaskPriority.Low),
            ("Enhancements",           "New features",     TaskPriority.Medium),
        ],
    };

    private static async Task<List<ProjectTask>> SeedTasksAsync(
        ApplicationDbContext db,
        List<Project> projects,
        List<User> users)
    {
        var random = new Random();
        var tasks = new List<ProjectTask>();

        foreach (var project in projects)
        {
            foreach (var (phase, templates) in PhaseTemplates)
            {
                foreach (var (title, description, priority) in templates)
                {
                    tasks.Add(new ProjectTask
                    {
                        Title = $"{phase} - {title}",
                        Description = $"{description} for {project.Name}",
                        Phase = phase,                                        // ← THE FIX
                        Status = (TaskStatus)random.Next(0, 3),
                        Priority = priority,
                        ProjectId = project.Id,
                        AssignedUserId = users[random.Next(users.Count)].Id,
                        DueDate = DateTime.UtcNow.AddDays(random.Next(5, 60)),
                    });
                }
            }
        }

        await db.Tasks.AddRangeAsync(tasks);
        await db.SaveChangesAsync();
        return tasks;
    }

    // ── Time Logs ─────────────────────────────────────────────────────────────

    private static async Task SeedTimeLogsAsync(ApplicationDbContext db, List<ProjectTask> tasks)
    {
        var random = new Random();
        var timeLogs = new List<TaskTimeLog>();
        var remainingHours = 90.0;

        while (remainingHours > 0)
        {
            var task = tasks[random.Next(tasks.Count)];
            var hours = Math.Min(random.Next(1, 4), (int)Math.Ceiling(remainingHours));
            var start = DateTime.UtcNow.AddDays(-random.Next(1, 15)).AddHours(-random.Next(1, 5));

            timeLogs.Add(new TaskTimeLog
            {
                TaskId = task.Id,
                UserId = task.AssignedUserId,
                StartTime = start,
                EndTime = start.AddHours(hours),
                Notes = $"Worked {hours}h on {task.Title}",
            });

            remainingHours -= hours;
        }

        await db.TimeLogs.AddRangeAsync(timeLogs);
        await db.SaveChangesAsync();
    }
}