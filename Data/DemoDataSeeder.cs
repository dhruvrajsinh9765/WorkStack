using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkStack.Models;
using WorkStack.Models.Enums;
using WorkTask = WorkStack.Models.Task;

namespace WorkStack.Data;

public static class DemoDataSeeder
{
    private const string DemoWorkspaceName = "WorkStack Demo Workspace";
    private const string DemoPassword = "Demo@12345";

    public static async System.Threading.Tasks.Task SeedAsync(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager)
    {
        if (await context.Workspaces.AnyAsync(
                w => w.Name == DemoWorkspaceName))
        {
            return;
        }

        var now = DateTime.UtcNow;

        var owner = await GetOrCreateUserAsync(
            userManager,
            "demo.owner@workstack.com",
            "DemoOwner");

        var manager = await GetOrCreateUserAsync(
            userManager,
            "demo.manager@workstack.com",
            "DemoManager");

        var member = await GetOrCreateUserAsync(
            userManager,
            "demo.member@workstack.com",
            "DemoMember");

        var workspace = new Workspace
        {
            Name = DemoWorkspaceName,

            Description =
                "Demo workspace for the WorkStack college presentation.",

            OwnerId = owner.Id,

            CreatedAt = now,

            UpdatedAt = now
        };

        workspace.Members.Add(
            new WorkspaceMember
            {
                UserId = owner.Id,

                Role = WorkspaceRole.Owner,

                JoinedAt = now
            });

        workspace.Members.Add(
            new WorkspaceMember
            {
                UserId = manager.Id,

                Role = WorkspaceRole.Manager,

                JoinedAt = now
            });

        workspace.Members.Add(
            new WorkspaceMember
            {
                UserId = member.Id,

                Role = WorkspaceRole.Member,

                JoinedAt = now
            });

        context.Workspaces.Add(workspace);

        await context.SaveChangesAsync();


        // ============================================================
        // BOARD
        // ============================================================

        var board = new Board
        {
            WorkspaceId = workspace.Id,

            Name = "Product Launch",

            Description =
                "Main board for planning and tracking the product launch.",

            CreatedAt = now,

            UpdatedAt = now
        };

        context.Boards.Add(board);

        await context.SaveChangesAsync();


        // ============================================================
        // LISTS
        // ============================================================

        var backlog = new WorkStack.Models.List
        {
            BoardId = board.Id,

            Name = "Backlog",

            Position = 1,

            CreatedAt = now,

            UpdatedAt = now
        };

        var inProgress = new WorkStack.Models.List
        {
            BoardId = board.Id,

            Name = "In Progress",

            Position = 2,

            CreatedAt = now,

            UpdatedAt = now
        };

        var review = new WorkStack.Models.List
        {
            BoardId = board.Id,

            Name = "Review",

            Position = 3,

            CreatedAt = now,

            UpdatedAt = now
        };

        var done = new WorkStack.Models.List
        {
            BoardId = board.Id,

            Name = "Done",

            Position = 4,

            CreatedAt = now,

            UpdatedAt = now
        };

        context.Lists.AddRange(
            backlog,
            inProgress,
            review,
            done);

        await context.SaveChangesAsync();


        // ============================================================
        // TASKS
        // ============================================================

        var tasks = new[]
        {
            new WorkTask
            {
                ListId = backlog.Id,

                Title = "Finalize project requirements",

                Description =
                    "Review the final functional requirements before the presentation.",

                CreatorId = owner.Id,

                Priority = TaskPriority.High,

                DueDate = now.AddDays(2),

                CreatedAt = now,

                UpdatedAt = now
            },

            new WorkTask
            {
                ListId = backlog.Id,

                Title = "Prepare database documentation",

                Description =
                    "Document the main entities and relationships used by WorkStack.",

                CreatorId = manager.Id,

                Priority = TaskPriority.Medium,

                DueDate = now.AddDays(5),

                CreatedAt = now,

                UpdatedAt = now
            },

            new WorkTask
            {
                ListId = inProgress.Id,

                Title = "Polish dashboard UI",

                Description =
                    "Complete the dashboard styling and make the demo experience consistent.",

                CreatorId = owner.Id,

                Priority = TaskPriority.Urgent,

                DueDate = now.AddDays(1),

                CreatedAt = now,

                UpdatedAt = now
            },

            new WorkTask
            {
                ListId = inProgress.Id,

                Title = "Test role permissions",

                Description =
                    "Verify Owner, Manager, and Member permissions before the demo.",

                CreatorId = manager.Id,

                Priority = TaskPriority.High,

                DueDate = now.AddDays(1),

                CreatedAt = now,

                UpdatedAt = now
            },

            new WorkTask
            {
                ListId = review.Id,

                Title = "Review task assignment flow",

                Description =
                    "Check that tasks can be assigned only to workspace members.",

                CreatorId = owner.Id,

                Priority = TaskPriority.Medium,

                DueDate = now.AddDays(3),

                CreatedAt = now,

                UpdatedAt = now
            },

            new WorkTask
            {
                ListId = review.Id,

                Title = "Check validation messages",

                Description =
                    "Verify required fields and access-denied behaviour.",

                CreatorId = member.Id,

                Priority = TaskPriority.Low,

                DueDate = now.AddDays(6),

                CreatedAt = now,

                UpdatedAt = now
            },

            new WorkTask
            {
                ListId = done.Id,

                Title = "Implement authentication",

                Description =
                    "Register, login, logout, and Identity-based authentication are complete.",

                CreatorId = owner.Id,

                Priority = TaskPriority.High,

                DueDate = now.AddDays(-2),

                CreatedAt = now.AddDays(-5),

                UpdatedAt = now.AddDays(-2)
            },

            new WorkTask
            {
                ListId = done.Id,

                Title = "Implement workspace management",

                Description =
                    "Workspace creation, members, roles, and access control are complete.",

                CreatorId = manager.Id,

                Priority = TaskPriority.Medium,

                DueDate = now.AddDays(-1),

                CreatedAt = now.AddDays(-4),

                UpdatedAt = now.AddDays(-1)
            }
        };

        context.Tasks.AddRange(tasks);

        await context.SaveChangesAsync();


        // ============================================================
        // TASK ASSIGNMENTS
        // ============================================================

        context.TaskAssignees.AddRange(

            new TaskAssignee
            {
                TaskId = tasks[0].Id,
                UserId = manager.Id
            },

            new TaskAssignee
            {
                TaskId = tasks[1].Id,
                UserId = member.Id
            },

            new TaskAssignee
            {
                TaskId = tasks[2].Id,
                UserId = owner.Id
            },

            new TaskAssignee
            {
                TaskId = tasks[3].Id,
                UserId = manager.Id
            },

            new TaskAssignee
            {
                TaskId = tasks[4].Id,
                UserId = owner.Id
            },

            new TaskAssignee
            {
                TaskId = tasks[4].Id,
                UserId = member.Id
            },

            new TaskAssignee
            {
                TaskId = tasks[5].Id,
                UserId = member.Id
            },

            new TaskAssignee
            {
                TaskId = tasks[6].Id,
                UserId = owner.Id
            },

            new TaskAssignee
            {
                TaskId = tasks[7].Id,
                UserId = manager.Id
            });

        await context.SaveChangesAsync();


        // ============================================================
        // COMMENTS
        // ============================================================

        context.Comments.AddRange(

            new Comment
            {
                TaskId = tasks[0].Id,

                UserId = owner.Id,

                Content =
                    "Let's make sure the final requirements are ready before the demo.",

                CreatedAt = now.AddHours(-5),

                UpdatedAt = now.AddHours(-5)
            },

            new Comment
            {
                TaskId = tasks[2].Id,

                UserId = manager.Id,

                Content =
                    "Dashboard looks good. We should highlight the task counters during the presentation.",

                CreatedAt = now.AddHours(-3),

                UpdatedAt = now.AddHours(-3)
            },

            new Comment
            {
                TaskId = tasks[4].Id,

                UserId = member.Id,

                Content =
                    "I verified the assignment workflow and the workspace membership check.",

                CreatedAt = now.AddHours(-2),

                UpdatedAt = now.AddHours(-2)
            },

            new Comment
            {
                TaskId = tasks[6].Id,

                UserId = owner.Id,

                Content =
                    "Authentication flow is ready for the final demo.",

                CreatedAt = now.AddDays(-1),

                UpdatedAt = now.AddDays(-1)
            });

        await context.SaveChangesAsync();
    }


    // ============================================================
    // CREATE DEMO USER
    // ============================================================

    private static async System.Threading.Tasks.Task<IdentityUser>
        GetOrCreateUserAsync(
            UserManager<IdentityUser> userManager,
            string email,
            string userName)
    {
        var user =
            await userManager.FindByEmailAsync(email);

        if (user is not null)
        {
            return user;
        }

        user = new IdentityUser
        {
            UserName = userName,

            Email = email,

            EmailConfirmed = true
        };

        var result =
            await userManager.CreateAsync(
                user,
                DemoPassword);

        if (!result.Succeeded)
        {
            var errors =
                string.Join(
                    "; ",
                    result.Errors.Select(
                        error => error.Description));

            throw new InvalidOperationException(
                $"Could not create demo user {email}: {errors}");
        }

        return user;
    }
}