using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmacyJobPlatform.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<JobPost> JobPosts { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ConversationRequest> ConversationRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ConversationRequest>()
                .HasOne(cr => cr.FromUser)
                .WithMany()
                .HasForeignKey(cr => cr.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConversationRequest>()
                .HasOne(cr => cr.ToUser)
                .WithMany()
                .HasForeignKey(cr => cr.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.User1)
                .WithMany()
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.User2)
                .WithMany()
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.Worker)
                .WithMany()
                .HasForeignKey(ja => ja.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // JobApplication → JobPost
            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.JobPost)
                .WithMany()
                .HasForeignKey(ja => ja.JobPostId)
                .OnDelete(DeleteBehavior.Cascade);

        }


    }
}
