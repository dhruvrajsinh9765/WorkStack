using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkStack.Models;
using WorkTask = WorkStack.Models.Task;

namespace WorkStack.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Workspace> Workspaces => Set<Workspace>();

        public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();

        public DbSet<Board> Boards => Set<Board>();

        public DbSet<List> Lists => Set<List>();

        public DbSet<WorkTask> Tasks => Set<WorkTask>();

        public DbSet<TaskAssignee> TaskAssignees => Set<TaskAssignee>();

        public DbSet<Comment> Comments => Set<Comment>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<WorkspaceMember>()
                .HasIndex(member => new { member.WorkspaceId, member.UserId })
                .IsUnique();

            builder.Entity<TaskAssignee>()
                .HasKey(assignee => new { assignee.TaskId, assignee.UserId });

            builder.Entity<Workspace>()
                .Property(workspace => workspace.Name)
                .HasMaxLength(100);

            builder.Entity<Workspace>()
                .Property(workspace => workspace.Description)
                .HasMaxLength(500);

            builder.Entity<Workspace>()
                .HasIndex(workspace => workspace.OwnerId);

            builder.Entity<Board>()
                .Property(board => board.Name)
                .HasMaxLength(100);

            builder.Entity<Board>()
                .Property(board => board.Description)
                .HasMaxLength(500);

            builder.Entity<Board>()
                .HasIndex(board => board.WorkspaceId);

            builder.Entity<List>()
                .Property(list => list.Name)
                .HasMaxLength(100);

            builder.Entity<List>()
                .HasIndex(list => list.BoardId);

            builder.Entity<WorkTask>()
                .Property(task => task.Title)
                .HasMaxLength(200);

            builder.Entity<WorkTask>()
                .HasIndex(task => task.ListId);

            builder.Entity<WorkTask>()
                .HasIndex(task => task.CreatorId);

            builder.Entity<WorkTask>()
                .HasIndex(task => task.DueDate);

            builder.Entity<TaskAssignee>()
                .HasIndex(assignee => assignee.UserId);

            builder.Entity<Comment>()
                .Property(comment => comment.Content)
                .HasMaxLength(2000);

            builder.Entity<Comment>()
                .HasIndex(comment => comment.TaskId);

            builder.Entity<Comment>()
                .HasIndex(comment => comment.UserId);

            builder.Entity<Workspace>()
                .HasOne(workspace => workspace.Owner)
                .WithMany()
                .HasForeignKey(workspace => workspace.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Workspace>()
                .HasMany(workspace => workspace.Members)
                .WithOne(member => member.Workspace)
                .HasForeignKey(member => member.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WorkspaceMember>()
                .HasOne(member => member.User)
                .WithMany()
                .HasForeignKey(member => member.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Workspace>()
                .HasMany(workspace => workspace.Boards)
                .WithOne(board => board.Workspace)
                .HasForeignKey(board => board.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Board>()
                .HasMany(board => board.Lists)
                .WithOne(list => list.Board)
                .HasForeignKey(list => list.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<List>()
                .HasMany(list => list.Tasks)
                .WithOne(task => task.List)
                .HasForeignKey(task => task.ListId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<WorkTask>()
                .HasOne(task => task.Creator)
                .WithMany()
                .HasForeignKey(task => task.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<WorkTask>()
                .HasMany(task => task.Assignees)
                .WithOne(assignee => assignee.Task)
                .HasForeignKey(assignee => assignee.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TaskAssignee>()
                .HasOne(assignee => assignee.User)
                .WithMany()
                .HasForeignKey(assignee => assignee.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<WorkTask>()
                .HasMany(task => task.Comments)
                .WithOne(comment => comment.Task)
                .HasForeignKey(comment => comment.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Comment>()
                .HasOne(comment => comment.User)
                .WithMany()
                .HasForeignKey(comment => comment.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
