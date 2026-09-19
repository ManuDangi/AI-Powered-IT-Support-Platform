using AIITSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIITSupport.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<TicketCategory> TicketCategories => Set<TicketCategory>();
    public DbSet<AIAnalysis> AIAnalyses => Set<AIAnalysis>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<PolicyRule> PolicyRules => Set<PolicyRule>();
    public DbSet<TicketAction> TicketActions => Set<TicketAction>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ----- Composite key for the join table -----
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        // ----- Uniqueness -----
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.TicketNumber)
            .IsUnique();

        // ----- Avoid accidental multi-cascade-path errors on SQL Server -----
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.CreatedByUser)
            .WithMany(u => u.Tickets)
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Approval>()
            .HasOne(a => a.Reviewer)
            .WithMany()
            .HasForeignKey(a => a.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // ----- Seed the three fixed roles -----
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Employee" },
            new Role { Id = 2, Name = "SupportAgent" },
            new Role { Id = 3, Name = "Admin" }
        );

        // ----- Seed starter ticket categories -----
        modelBuilder.Entity<TicketCategory>().HasData(
            new TicketCategory { Id = 1, Name = "VPN" },
            new TicketCategory { Id = 2, Name = "Password" },
            new TicketCategory { Id = 3, Name = "Laptop" },
            new TicketCategory { Id = 4, Name = "Email" },
            new TicketCategory { Id = 5, Name = "Software" },
            new TicketCategory { Id = 6, Name = "Network" },
            new TicketCategory { Id = 7, Name = "Access" },
            new TicketCategory { Id = 8, Name = "Security" }
        );
    }
}
