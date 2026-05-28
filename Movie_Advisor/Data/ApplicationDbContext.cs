using Microsoft.EntityFrameworkCore;
using Movie_Advisor.Models;
using MovieAdvisor.Models;

namespace Movie_Advisor.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<OtpVerification> OtpVerifications { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Watchlist> Watchlists { get; set; }

        // ✅ ADD THIS: WatchedMovies DbSet
        public DbSet<WatchedMovie> WatchedMovies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(bool))
                    {
                        property.SetValueConverter(
                            new Microsoft.EntityFrameworkCore.Storage.ValueConversion.BoolToZeroOneConverter<int>());
                    }
                }
            }

            modelBuilder.Entity<OtpVerification>(entity =>
            {
                entity.ToTable("OTP_VERIFICATIONS");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("OTP_ID");
                entity.Property(e => e.Email).HasColumnName("EMAIL").HasMaxLength(100);
                entity.Property(e => e.OtpCode).HasColumnName("OTP_CODE").HasMaxLength(6);
                entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT");
                entity.Property(e => e.ExpiresAt).HasColumnName("EXPIRES_AT");
                entity.Property(e => e.Attempts).HasColumnName("ATTEMPTS");

                entity.Property(e => e.IsUsed)
                    .HasColumnName("IS_USED")
                    .HasConversion<int>();

                entity.Property(e => e.VerifiedAt).HasColumnName("VERIFIED_AT");

                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.OtpCode);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("USERS");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("USER_ID");
                entity.Property(e => e.Username).HasColumnName("USERNAME").HasMaxLength(50);
                entity.Property(e => e.Email).HasColumnName("EMAIL").HasMaxLength(100);
                entity.Property(e => e.PasswordHash).HasColumnName("PASSWORD_HASH").HasMaxLength(256);
                entity.Property(e => e.FirstName).HasColumnName("FIRST_NAME").HasMaxLength(50);
                entity.Property(e => e.LastName).HasColumnName("LAST_NAME").HasMaxLength(50);
                entity.Property(e => e.DateOfBirth).HasColumnName("DATE_OF_BIRTH");
                entity.Property(e => e.Gender).HasColumnName("GENDER").HasMaxLength(10);
                entity.Property(e => e.ProfilePicture).HasColumnName("PROFILE_PICTURE").HasMaxLength(500);
                entity.Property(e => e.Bio).HasColumnName("BIO").HasMaxLength(500);
                entity.Property(e => e.GoogleId).HasColumnName("GOOGLE_ID").HasMaxLength(255);

                entity.Property(e => e.Role)
                    .HasColumnName("ROLE")
                    .HasConversion<int>();

                entity.Property(e => e.IsActive)
                    .HasColumnName("IS_ACTIVE")
                    .HasConversion<int>();

                entity.Property(e => e.IsVerified)
                    .HasColumnName("IS_VERIFIED")
                    .HasConversion<int>();

                entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT");
                entity.Property(e => e.UpdatedAt).HasColumnName("UPDATED_AT");
                entity.Property(e => e.LastLogin).HasColumnName("LAST_LOGIN");
                entity.Property(e => e.PasswordResetToken).HasColumnName("PASSWORD_RESET_TOKEN").HasMaxLength(255);
                entity.Property(e => e.PasswordResetTokenExpiry).HasColumnName("PASSWORD_RESET_TOKEN_EXPIRY");

                entity.Ignore(e => e.Name);
                entity.Ignore(e => e.CreatedDate);
                entity.Ignore(e => e.Password);
                entity.Ignore(e => e.UpdatedDate);
                entity.Ignore(e => e.LastLoginDate);

                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            modelBuilder.Entity<Movie>(entity =>
            {
                entity.ToTable("MOVIES");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("MOVIE_ID");
                entity.Property(e => e.Title).HasColumnName("TITLE");
                entity.Property(e => e.Description).HasColumnName("PLOT");
                entity.Property(e => e.Language).HasColumnName("LANGUAGE");
                entity.Property(e => e.Country).HasColumnName("COUNTRY");
                entity.Property(e => e.ReleaseDate).HasColumnName("RELEASE_DATE");
                entity.Property(e => e.Duration).HasColumnName("DURATION");
                entity.Property(e => e.Rating).HasColumnName("RATING").HasColumnType("NUMBER(3,1)");
                entity.Property(e => e.PosterUrl).HasColumnName("POSTER_URL");
                entity.Property(e => e.BackdropUrl).HasColumnName("BACKDROP_URL");
                entity.Property(e => e.TrailerUrl).HasColumnName("TRAILER_URL");
                entity.Property(e => e.ImdbId).HasColumnName("IMDB_ID");
                entity.Property(e => e.TmdbId).HasColumnName("TMDB_ID");
                entity.Property(e => e.Budget).HasColumnName("BUDGET").HasColumnType("NUMBER(15,2)");
                entity.Property(e => e.Revenue).HasColumnName("REVENUE").HasColumnType("NUMBER(15,2)");
                entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT");
                entity.Property(e => e.Genre).HasColumnName("GENRE").HasMaxLength(500);

                entity.Ignore(e => e.ReleaseYear);
                entity.Ignore(e => e.AverageRating);
                entity.Ignore(e => e.DurationMinutes);
                entity.Ignore(e => e.Year);
                entity.Ignore(e => e.DateAdded);
                entity.Ignore(e => e.GenreList);
            });

            modelBuilder.Entity<Genre>(entity =>
            {
                entity.ToTable("GENRES");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("GENRE_ID");
                entity.Property(e => e.Name).HasColumnName("NAME");

                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<UserPreference>(entity =>
            {
                entity.ToTable("USER_GENRE_PREFERENCES");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("PREFERENCE_ID");
                entity.Property(e => e.UserId).HasColumnName("USER_ID");
                entity.Property(e => e.GenreName).HasColumnName("GENRE_NAME");
                entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT");

                entity.HasOne(e => e.User)
                      .WithMany(u => u.UserGenrePreferences)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.ToTable("ACTIVITY_LOGS");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("LOG_ID");
                entity.Property(e => e.UserName).HasColumnName("USER_NAME");
                entity.Property(e => e.ActionType).HasColumnName("ACTION_TYPE");
                entity.Property(e => e.Description).HasColumnName("DESCRIPTION");
                entity.Property(e => e.Timestamp).HasColumnName("TIMESTAMP");

                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.ActionType);
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.ToTable("COMMENTS");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("COMMENT_ID");

                entity.Property(e => e.MovieId)
                    .HasColumnName("MOVIE_ID")
                    .IsRequired();

                entity.Property(e => e.UserId)
                    .HasColumnName("USER_ID")
                    .IsRequired();

                entity.Property(e => e.CommentText)
                    .HasColumnName("COMMENT_TEXT")
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.Property(e => e.IsApproved)
                    .HasColumnName("IS_APPROVED")
                    .HasDefaultValue(0);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CREATED_AT")
                    .HasDefaultValueSql("SYSTIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("UPDATED_AT");

                entity.HasOne(c => c.Movie)
                    .WithMany()
                    .HasForeignKey(c => c.MovieId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.MovieId)
                    .HasDatabaseName("IDX_COMMENTS_MOVIE");

                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IDX_COMMENTS_USER");

                entity.HasIndex(e => e.IsApproved)
                    .HasDatabaseName("IDX_COMMENTS_APPROVED");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IDX_COMMENTS_CREATED");
            });

            modelBuilder.Entity<Watchlist>(entity =>
            {
                entity.ToTable("WATCHLISTS");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("WATCHLIST_ID");

                entity.Property(e => e.UserId)
                    .HasColumnName("USER_ID")
                    .IsRequired();

                entity.Property(e => e.MovieId)
                    .HasColumnName("MOVIE_ID")
                    .IsRequired();

                entity.Property(e => e.AddedAt)
                    .HasColumnName("ADDED_AT")
                    .HasDefaultValueSql("SYSTIMESTAMP");

                entity.HasOne(w => w.User)
                    .WithMany()
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(w => w.Movie)
                    .WithMany()
                    .HasForeignKey(w => w.MovieId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IDX_WATCHLIST_USER");

                entity.HasIndex(e => e.MovieId)
                    .HasDatabaseName("IDX_WATCHLIST_MOVIE");

                entity.HasIndex(e => new { e.UserId, e.MovieId })
                    .IsUnique()
                    .HasDatabaseName("IDX_WATCHLIST_USER_MOVIE");
            });

            // ✅ ADD THIS: WatchedMovie Configuration
            modelBuilder.Entity<WatchedMovie>(entity =>
            {
                entity.ToTable("WATCHED_MOVIES");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("WATCHED_ID");

                entity.Property(e => e.UserId)
                    .HasColumnName("USER_ID")
                    .IsRequired();

                entity.Property(e => e.MovieId)
                    .HasColumnName("MOVIE_ID")
                    .IsRequired();

                entity.Property(e => e.WatchedAt)
                    .HasColumnName("WATCHED_AT")
                    .HasDefaultValueSql("SYSTIMESTAMP");

                // Configure relationships
                entity.HasOne(w => w.User)
                    .WithMany()
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(w => w.Movie)
                    .WithMany()
                    .HasForeignKey(w => w.MovieId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes for performance
                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IDX_WATCHED_USER");

                entity.HasIndex(e => e.MovieId)
                    .HasDatabaseName("IDX_WATCHED_MOVIE");

                // Unique constraint: one user can mark a movie as watched only once
                entity.HasIndex(e => new { e.UserId, e.MovieId })
                    .IsUnique()
                    .HasDatabaseName("IDX_WATCHED_USER_MOVIE");
            });
        }
    }
}