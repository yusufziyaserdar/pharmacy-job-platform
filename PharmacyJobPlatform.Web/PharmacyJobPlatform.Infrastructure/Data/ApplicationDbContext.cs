using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Domain.Entities;

namespace PharmacyJobPlatform.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        public DbSet<Address> Addresses { get; set; } // ✅ EKLENDİ

        public DbSet<JobPost> JobPosts { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ConversationRequest> ConversationRequests { get; set; }
        public DbSet<WorkExperience> WorkExperiences { get; set; }
        public DbSet<UserRating> UserRatings { get; set; }
        public DbSet<ProfileComment> ProfileComments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================
            // User ↔ Address
            // ============================
            modelBuilder.Entity<User>()
                .HasOne(u => u.Address)
                .WithMany(a => a.Users)
                .HasForeignKey(u => u.AddressId)
                .OnDelete(DeleteBehavior.SetNull);

            // ============================
            // JobPost ↔ Address
            // ============================
            modelBuilder.Entity<JobPost>()
                .HasOne(j => j.Address)
                .WithMany(a => a.JobPosts)
                .HasForeignKey(j => j.AddressId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================
            // ConversationRequest
            // ============================
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

            // ============================
            // Conversation
            // ============================
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

            // ============================
            // JobApplication
            // ============================
            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.Worker)
                .WithMany()
                .HasForeignKey(ja => ja.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.JobPost)
                .WithMany()
                .HasForeignKey(ja => ja.JobPostId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================
            // WorkExperience
            // ============================
            modelBuilder.Entity<WorkExperience>()
                .HasOne(w => w.User)
                .WithMany(u => u.WorkExperiences)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================
            // UserRating
            // ============================
            modelBuilder.Entity<UserRating>()
                .HasOne(r => r.Rater)
                .WithMany(u => u.RatingsGiven)
                .HasForeignKey(r => r.RaterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRating>()
                .HasOne(r => r.RatedUser)
                .WithMany(u => u.RatingsReceived)
                .HasForeignKey(r => r.RatedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRating>()
                .HasIndex(r => new { r.RaterId, r.RatedUserId })
                .IsUnique();


            // ============================
            // ProfileComment
            // ============================
            modelBuilder.Entity<ProfileComment>()
                .HasOne(c => c.ProfileUser)
                .WithMany(u => u.CommentsReceived)
                .HasForeignKey(c => c.ProfileUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProfileComment>()
                .HasOne(c => c.AuthorUser)
                .WithMany(u => u.CommentsWritten)
                .HasForeignKey(c => c.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProfileComment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.NoAction);

        }
    }
}
