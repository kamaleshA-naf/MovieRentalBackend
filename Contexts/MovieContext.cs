using Microsoft.EntityFrameworkCore;
using MovieRentalApp.Models;

namespace MovieRentalApp.Contexts
{
    public class MovieContext : DbContext
    {
        public MovieContext(
            DbContextOptions<MovieContext> options)
            : base(options) { }

        // ── DbSets ────────────────────────────────────────────────
        public DbSet<User> Users { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<MovieGenre> MovieGenres { get; set; }
        public DbSet<Rental> Rentals { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Cart> Carts { get; set; }

        public DbSet<MovieRating> MovieRatings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── MovieGenre (composite key) ────────────────────────
            modelBuilder.Entity<MovieGenre>()
                .HasKey(mg => new { mg.MovieId, mg.GenreId });

            // Deleting a Movie → removes its MovieGenre rows (safe)
            modelBuilder.Entity<MovieGenre>()
                .HasOne(mg => mg.Movie)
                .WithMany(m => m.MovieGenres)
                .HasForeignKey(mg => mg.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            // Deleting a Genre → does NOT delete movies
            // Just removes the link row
            modelBuilder.Entity<MovieGenre>()
                .HasOne(mg => mg.Genre)
                .WithMany(g => g.MovieGenres)
                .HasForeignKey(mg => mg.GenreId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── User: Role stored as string ───────────────────────
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder.Entity<User>()
                .Property(u => u.UserId)
                .UseIdentityColumn(seed: 1, increment: 1);

            // ── Rental ────────────────────────────────────────────
            // Deleting a User → restrict (don't silently wipe rentals)
            modelBuilder.Entity<Rental>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Deleting a Movie → restrict (keep rental history)
            modelBuilder.Entity<Rental>()
                .HasOne(r => r.Movie)
                .WithMany(m => m.Rentals)
                .HasForeignKey(r => r.MovieId)
                .OnDelete(DeleteBehavior.Restrict);



            modelBuilder.Entity<MovieRating>()
                .HasOne(r => r.Movie)
                .WithMany()
                .HasForeignKey(r => r.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MovieRating>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One rating record per user per movie
            modelBuilder.Entity<MovieRating>()
                .HasIndex(r => new { r.UserId, r.MovieId })
                .IsUnique();

            // ── Payment ───────────────────────────────────────────
            // Deleting a User → restrict (keep payment records)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Deleting a Movie → restrict (keep payment history)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Movie)
                .WithMany()
                .HasForeignKey(p => p.MovieId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            // ── Movie ─────────────────────────────────────────────
            modelBuilder.Entity<Movie>()
                .Property(m => m.RentalPrice)
                .HasColumnType("decimal(18,2)");

            // ── Wishlist ──────────────────────────────────────────
            // Deleting a User → remove their wishlist (safe)
            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Deleting a Movie → remove from all wishlists (safe)
            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.Movie)
                .WithMany(m => m.Wishlists)
                .HasForeignKey(w => w.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique: one entry per user per movie
            modelBuilder.Entity<Wishlist>()
                .HasIndex(w => new { w.UserId, w.MovieId })
                .IsUnique();

            // ── Cart ──────────────────────────────────────────────
            // Deleting a User → remove their cart (safe)
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Deleting a Movie → remove from all carts (safe)
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Movie)
                .WithMany()
                .HasForeignKey(c => c.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique: one cart entry per user per movie
            modelBuilder.Entity<Cart>()
                .HasIndex(c => new { c.UserId, c.MovieId })
                .IsUnique();

            modelBuilder.Entity<Cart>()
                .Property(c => c.DurationDays)
                .HasDefaultValue(7);

            // ── AuditLog ──────────────────────────────────────────
            modelBuilder.Entity<AuditLog>()
                .HasKey(a => a.LogId);

            // Deleting a User → restrict (keep audit trail)
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Notification ──────────────────────────────────────
            // Deleting a User → remove their notifications (safe)
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);





            // ── Indexes ───────────────────────────────────────────
            modelBuilder.Entity<Movie>()
                .HasIndex(m => m.Title)
                .HasDatabaseName("IX_Movie_Title");

            modelBuilder.Entity<Movie>()
                .HasIndex(m => m.ReleaseYear)
                .HasDatabaseName("IX_Movie_ReleaseYear");

            modelBuilder.Entity<Movie>()
                .HasIndex(m => m.Director)
                .HasDatabaseName("IX_Movie_Director");

            modelBuilder.Entity<MovieGenre>()
                .HasIndex(mg => mg.GenreId)
                .HasDatabaseName("IX_MovieGenre_GenreId");

            modelBuilder.Entity<MovieGenre>()
                .HasIndex(mg => mg.MovieId)
                .HasDatabaseName("IX_MovieGenre_MovieId");

            modelBuilder.Entity<Rental>()
                .Ignore(r => r.Status);

            modelBuilder.Entity<Rental>()
                .Property(r => r.StoredStatus)
                .HasColumnName("Status");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.UserId)
                .HasDatabaseName("IX_Payment_UserId");

            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.UserId)
                .HasDatabaseName("IX_Cart_UserId");
        }
    }
}