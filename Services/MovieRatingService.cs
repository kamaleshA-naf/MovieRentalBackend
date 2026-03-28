using Microsoft.EntityFrameworkCore;
using MovieRentalApp.Contexts;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;

namespace MovieRentalApp.Services
{
    public class MovieRatingService : IMovieRatingService
    {
        private readonly IRepository<int, MovieRating> _ratingRepository;
        private readonly IRepository<int, Movie> _movieRepository;
        private readonly IRepository<int, User> _userRepository;
        private readonly IRepository<int, MovieGenre> _movieGenreRepository;
        private readonly IRepository<int, Genre> _genreRepository;
        private readonly AuditLogService _auditLog;
        private readonly MovieContext _context;

        public MovieRatingService(
            IRepository<int, MovieRating> ratingRepository,
            IRepository<int, Movie> movieRepository,
            IRepository<int, User> userRepository,
            IRepository<int, MovieGenre> movieGenreRepository,
            IRepository<int, Genre> genreRepository,
            AuditLogService auditLog,
            MovieContext context)
        {
            _ratingRepository = ratingRepository;
            _movieRepository = movieRepository;
            _userRepository = userRepository;
            _movieGenreRepository = movieGenreRepository;
            _genreRepository = genreRepository;
            _auditLog = auditLog;
            _context = context;
        }

        // ── RATE MOVIE ────────────────────────────────────────────
        public async Task<MovieRatingResponseDto> RateMovie(
            int movieId, int userId, MovieRatingCreateDto dto)
        {
            var movie = await _movieRepository.GetByIdAsync(movieId);
            if (movie == null)
                throw new EntityNotFoundException("Movie", movieId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException("User", userId);

            var existing = await _ratingRepository.FindAsync(
                r => r.MovieId == movieId && r.UserId == userId);

            MovieRating rating;

            if (existing.Any())
            {
                rating = existing.First();

                // Toggle: same value = remove rating
                if (!rating.IsRemoved && rating.RatingValue == dto.RatingValue)
                {
                    rating.IsRemoved = true;
                    rating.UpdatedAt = DateTime.UtcNow;
                    await _ratingRepository.UpdateAsync(rating.Id, rating);

                    await _auditLog.LogAsync(
                        user.UserId, user.UserName, user.Role.ToString(),
                        $"Removed rating for '{movie.Title}'.", "");

                    await UpdateMovieAverageRating(movieId, movie);
                    return BuildResponse(rating, movie, user, removed: true);
                }

                // Update to new value
                var oldLabel = GetLabel(rating.RatingValue);
                rating.RatingValue = dto.RatingValue;
                rating.IsRemoved = false;
                rating.UpdatedAt = DateTime.UtcNow;
                await _ratingRepository.UpdateAsync(rating.Id, rating);

                await _auditLog.LogAsync(
                    user.UserId, user.UserName, user.Role.ToString(),
                    $"Updated rating for '{movie.Title}' from '{oldLabel}' " +
                    $"to '{GetLabel(dto.RatingValue)}'.", "");
            }
            else
            {
                // First time rating
                rating = new MovieRating
                {
                    MovieId = movieId,
                    UserId = userId,
                    RatingValue = dto.RatingValue,
                    RatedAt = DateTime.UtcNow,
                    IsRemoved = false
                };
                await _ratingRepository.AddAsync(rating);

                await _auditLog.LogAsync(
                    user.UserId, user.UserName, user.Role.ToString(),
                    $"Rated '{movie.Title}' as '{GetLabel(dto.RatingValue)}'.", "");
            }

            // Sync Movie.Rating with the live average from customer submissions
            await UpdateMovieAverageRating(movieId, movie);
            return BuildResponse(rating, movie, user, removed: false);
        }

        // Recalculates Movie.Rating from all active customer ratings
        private async Task UpdateMovieAverageRating(int movieId, Movie movie)
        {
            var allRatings = (await _ratingRepository
                .FindAsync(r => r.MovieId == movieId && !r.IsRemoved))
                .ToList();

            movie.Rating = allRatings.Any()
                ? Math.Round(allRatings.Average(r => r.RatingValue), 1)
                : 0;

            await _movieRepository.UpdateAsync(movieId, movie);
        }

        // ── REMOVE RATING ─────────────────────────────────────────
        public async Task<MovieRatingResponseDto> RemoveRating(int movieId, int userId)
        {
            var existing = await _ratingRepository.FindAsync(
                r => r.MovieId == movieId && r.UserId == userId);
            var rating = existing.FirstOrDefault();
            if (rating == null)
                throw new EntityNotFoundException("No rating found for this movie.");

            var movie = await _movieRepository.GetByIdAsync(movieId);
            var user = await _userRepository.GetByIdAsync(userId);

            rating.IsRemoved = true;
            rating.UpdatedAt = DateTime.UtcNow;
            await _ratingRepository.UpdateAsync(rating.Id, rating);

            await _auditLog.LogAsync(
                userId, user!.UserName, user.Role.ToString(),
                $"Explicitly removed rating for '{movie!.Title}'.", "");

            return BuildResponse(rating, movie!, user!, removed: true);
        }

        // ── GET SUMMARY ───────────────────────────────────────────
        public async Task<MovieRatingSummaryDto> GetMovieRatingSummary(int movieId)
        {
            var movie = await _movieRepository.GetByIdAsync(movieId);
            if (movie == null)
                throw new EntityNotFoundException("Movie", movieId);

            var ratings = (await _ratingRepository
                .FindAsync(r => r.MovieId == movieId && !r.IsRemoved))
                .ToList();

            return new MovieRatingSummaryDto
            {
                MovieId = movieId,
                MovieTitle = movie.Title,
                TotalRatings = ratings.Count,
                AverageRating = ratings.Any()
                    ? Math.Round(ratings.Average(r => r.RatingValue), 1) : 0,
                NotForMeCount = ratings.Count(r => r.RatingValue == 1),
                LikeCount = ratings.Count(r => r.RatingValue == 2),
                LoveCount = ratings.Count(r => r.RatingValue == 3)
            };
        }

        // ── GET USER RATING FOR MOVIE ─────────────────────────────
        public async Task<MovieRatingResponseDto?> GetUserRatingForMovie(
            int movieId, int userId)
        {
            var ratings = await _ratingRepository.FindAsync(
                r => r.MovieId == movieId && r.UserId == userId && !r.IsRemoved);
            var rating = ratings.FirstOrDefault();
            if (rating == null) return null;

            var movie = await _movieRepository.GetByIdAsync(movieId);
            var user = await _userRepository.GetByIdAsync(userId);
            return BuildResponse(rating, movie!, user!, removed: false);
        }

        // ── GET USER RATINGS ──────────────────────────────────────
        public async Task<IEnumerable<MovieRatingResponseDto>> GetUserRatings(int userId)
        {
            var ratings = await _ratingRepository
                .GetAllWithIncludeAsync(r => r.Movie, r => r.User);
            return ratings
                .Where(r => r.UserId == userId && !r.IsRemoved)
                .OrderByDescending(r => r.RatedAt)
                .Select(r => BuildResponse(r, r.Movie!, r.User!, removed: false));
        }

        // ── GET USER GENRE PREFERENCES ────────────────────────────
        public async Task<UserGenrePreferenceDto> GetUserGenrePreferences(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException("User", userId);

            var userRatings = (await _ratingRepository
                .FindAsync(r => r.UserId == userId && !r.IsRemoved))
                .ToList();

            var allGenres = await _genreRepository.GetAllAsync();
            var genreDict = allGenres.ToDictionary(g => g.Id, g => g.Name);
            var movieGenres = (await _movieGenreRepository.GetAllAsync()).ToList();

            var genreRatings = new Dictionary<int, List<int>>();
            foreach (var rating in userRatings)
            {
                var genres = movieGenres
                    .Where(mg => mg.MovieId == rating.MovieId)
                    .Select(mg => mg.GenreId);

                foreach (var genreId in genres)
                {
                    if (!genreRatings.ContainsKey(genreId))
                        genreRatings[genreId] = new List<int>();
                    genreRatings[genreId].Add(rating.RatingValue);
                }
            }

            var preferences = genreRatings.Select(kv => new GenreRatingDto
            {
                GenreName = genreDict.ContainsKey(kv.Key)
                                    ? genreDict[kv.Key] : "Unknown",
                AverageRating = Math.Round(kv.Value.Average(), 1),
                TotalRatings = kv.Value.Count
            })
            .OrderByDescending(g => g.AverageRating)
            .ToList();

            return new UserGenrePreferenceDto
            {
                UserId = userId,
                UserName = user.UserName,
                GenrePreferences = preferences
            };
        }

        // ── GET PAGINATED RATINGS FOR MOVIE ──────────────────────
        // Used for "Latest ratings 1–10 per page" on movie detail page
        public async Task<PagedResultDto<MovieRatingResponseDto>> GetMovieRatingsPaginatedAsync(
            int movieId, int pageNumber, int pageSize)
        {
            var query = _context.MovieRatings
                .Include(r => r.User)
                .Include(r => r.Movie)
                .Where(r => r.MovieId == movieId && !r.IsRemoved)
                .OrderByDescending(r => r.RatedAt);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = items.Select(r => BuildResponse(r, r.Movie!, r.User!, removed: false))
                            .ToList();

            return new PagedResultDto<MovieRatingResponseDto>
            {
                Data = dtos,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNext = pageNumber < totalPages,
                HasPrevious = pageNumber > 1
            };
        }

        // ── HELPERS ───────────────────────────────────────────────
        private static MovieRatingResponseDto BuildResponse(
            MovieRating rating, Movie movie, User user, bool removed) => new()
            {
                Id = rating.Id,
                MovieId = rating.MovieId,
                MovieTitle = movie.Title,
                UserId = rating.UserId,
                UserName = user.UserName,
                RatingValue = removed ? 0 : rating.RatingValue,
                RatingLabel = removed ? "Removed" : GetLabel(rating.RatingValue),
                RatedAt = rating.RatedAt,
                IsRemoved = removed
            };

        private static string GetLabel(int value) => value switch
        {
            1 => "Not for me",
            2 => "I like this",
            3 => "Love this!",
            _ => "Unknown"
        };
    }
}