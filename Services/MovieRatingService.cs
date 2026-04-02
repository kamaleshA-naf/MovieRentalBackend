﻿using Microsoft.EntityFrameworkCore;
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
        private readonly AuditLogService _auditLog;
        private readonly MovieContext _context;

        public MovieRatingService(
            IRepository<int, MovieRating> ratingRepository,
            IRepository<int, Movie> movieRepository,
            IRepository<int, User> userRepository,
            AuditLogService auditLog,
            MovieContext context)
        {
            _ratingRepository = ratingRepository;
            _movieRepository = movieRepository;
            _userRepository = userRepository;
            _auditLog = auditLog;
            _context = context;
        }

        // ── RATE MOVIE ────────────────────────────────────────────
        public async Task<MovieRatingResponseDto> RateMovie(
            int movieId, int userId, MovieRatingCreateDto dto)
        {
            var movie = await _movieRepository.GetByIdAsync(movieId);
            if (movie == null) throw new EntityNotFoundException("Movie", movieId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new EntityNotFoundException("User", userId);

            var existing = await _ratingRepository.FindAsync(
                r => r.MovieId == movieId && r.UserId == userId);

            MovieRating rating;

            if (existing.Any())
            {
                rating = existing.First();

                if (!rating.IsRemoved && rating.RatingValue == dto.RatingValue)
                {
                    rating.IsRemoved = true;
                    rating.UpdatedAt = DateTime.UtcNow;
                    await _ratingRepository.UpdateAsync(rating.Id, rating);
                    await _auditLog.LogAsync(user.UserId, user.UserName, user.Role.ToString(),
                        $"Removed rating for '{movie.Title}'.", "");
                    await UpdateMovieAverageRating(movieId, movie);
                    return BuildResponse(rating, movie, user, removed: true);
                }

                var oldLabel = GetLabel(rating.RatingValue);
                rating.RatingValue = dto.RatingValue;
                rating.IsRemoved = false;
                rating.UpdatedAt = DateTime.UtcNow;
                await _ratingRepository.UpdateAsync(rating.Id, rating);
                await _auditLog.LogAsync(user.UserId, user.UserName, user.Role.ToString(),
                    $"Updated rating for '{movie.Title}' from '{oldLabel}' to '{GetLabel(dto.RatingValue)}'.", "");
            }
            else
            {
                rating = new MovieRating
                {
                    MovieId = movieId, UserId = userId,
                    RatingValue = dto.RatingValue,
                    RatedAt = DateTime.UtcNow, IsRemoved = false
                };
                await _ratingRepository.AddAsync(rating);
                await _auditLog.LogAsync(user.UserId, user.UserName, user.Role.ToString(),
                    $"Rated '{movie.Title}' as '{GetLabel(dto.RatingValue)}'.", "");
            }

            await UpdateMovieAverageRating(movieId, movie);
            return BuildResponse(rating, movie, user, removed: false);
        }

        private async Task UpdateMovieAverageRating(int movieId, Movie movie)
        {
            var all = (await _ratingRepository.FindAsync(r => r.MovieId == movieId && !r.IsRemoved)).ToList();
            movie.Rating = all.Any() ? Math.Round(all.Average(r => r.RatingValue), 1) : 0;
            await _movieRepository.UpdateAsync(movieId, movie);
        }

        // ── GET SUMMARY ───────────────────────────────────────────
        public async Task<MovieRatingSummaryDto> GetMovieRatingSummary(int movieId)
        {
            var movie = await _movieRepository.GetByIdAsync(movieId);
            if (movie == null) throw new EntityNotFoundException("Movie", movieId);

            var ratings = (await _ratingRepository
                .FindAsync(r => r.MovieId == movieId && !r.IsRemoved)).ToList();

            return new MovieRatingSummaryDto
            {
                MovieId       = movieId,
                MovieTitle    = movie.Title,
                TotalRatings  = ratings.Count,
                AverageRating = ratings.Any() ? Math.Round(ratings.Average(r => r.RatingValue), 1) : 0,
                NotForMeCount = ratings.Count(r => r.RatingValue == 1),
                LikeCount     = ratings.Count(r => r.RatingValue == 2),
                LoveCount     = ratings.Count(r => r.RatingValue == 3)
            };
        }

        // ── GET USER RATING FOR MOVIE ─────────────────────────────
        public async Task<MovieRatingResponseDto?> GetUserRatingForMovie(int movieId, int userId)
        {
            var ratings = await _ratingRepository.FindAsync(
                r => r.MovieId == movieId && r.UserId == userId && !r.IsRemoved);
            var rating = ratings.FirstOrDefault();
            if (rating == null) return null;

            var movie = await _movieRepository.GetByIdAsync(movieId);
            var user  = await _userRepository.GetByIdAsync(userId);
            return BuildResponse(rating, movie!, user!, removed: false);
        }

        // ── GET PAGINATED RATINGS FOR MOVIE ───────────────────────
        public async Task<PagedResultDto<MovieRatingResponseDto>> GetMovieRatingsPaginatedAsync(
            int movieId, int pageNumber, int pageSize)
        {
            var query = _context.MovieRatings
                .Include(r => r.User)
                .Include(r => r.Movie)
                .Where(r => r.MovieId == movieId && !r.IsRemoved)
                .OrderByDescending(r => r.RatedAt);

            var total      = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            var items      = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResultDto<MovieRatingResponseDto>
            {
                Data        = items.Select(r => BuildResponse(r, r.Movie!, r.User!, removed: false)).ToList(),
                TotalCount  = total,
                PageNumber  = pageNumber,
                PageSize    = pageSize,
                TotalPages  = totalPages,
                HasNext     = pageNumber < totalPages,
                HasPrevious = pageNumber > 1
            };
        }

        // ── HELPERS ───────────────────────────────────────────────
        private static MovieRatingResponseDto BuildResponse(
            MovieRating rating, Movie movie, User user, bool removed) => new()
        {
            Id          = rating.Id,
            MovieId     = rating.MovieId,
            MovieTitle  = movie.Title,
            UserId      = rating.UserId,
            UserName    = user.UserName,
            RatingValue = removed ? 0 : rating.RatingValue,
            RatingLabel = removed ? "Removed" : GetLabel(rating.RatingValue),
            RatedAt     = rating.RatedAt,
            IsRemoved   = removed
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
