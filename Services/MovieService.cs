using Microsoft.EntityFrameworkCore;
using MovieRentalApp.Contexts;
using Microsoft.Extensions.Caching.Memory;
using MovieRentalApp.Models.DTOs;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;

namespace MovieRentalApp.Services
{
    public class MovieService : IMovieService
    {
        private readonly IRepository<int, Movie> _movieRepository;
        private readonly IRepository<int, MovieGenre> _movieGenreRepository;
        private readonly IRepository<int, Genre> _genreRepository;
        private readonly IMemoryCache _cache;
        private readonly AuditLogService _auditLog;
        private readonly MovieContext _context;

        private const string GenreCacheKey = "AllGenres";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public MovieService(
            IRepository<int, Movie> movieRepository,
            IRepository<int, MovieGenre> movieGenreRepository,
            IRepository<int, Genre> genreRepository,
            IMemoryCache cache,
            AuditLogService auditLog,
            MovieContext context)
        {
            _movieRepository = movieRepository;
            _movieGenreRepository = movieGenreRepository;
            _genreRepository = genreRepository;
            _cache = cache;
            _auditLog = auditLog;
            _context = context;
        }

        // ── GENRE DICT ────────────────────────────────────────────
        private async Task<Dictionary<int, string>> BuildGenreDict()
        {
            if (_cache.TryGetValue(GenreCacheKey, out Dictionary<int, string>? cached)
                && cached != null)
                return cached;

            var allGenres = await _genreRepository.GetAllAsync();
            var dict = allGenres.ToDictionary(g => g.Id, g => g.Name ?? string.Empty);
            _cache.Set(GenreCacheKey, dict,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheDuration
                });
            return dict;
        }

        private static MovieResponseDto MapToDto(
            Movie movie,
            IEnumerable<MovieGenre> movieGenres,
            Dictionary<int, string> genreDict)
        {
            var genres = movieGenres
                .Where(mg => mg.MovieId == movie.Id)
                .Select(mg => new GenreResponseDto
                {
                    Id = mg.GenreId,
                    Name = genreDict.ContainsKey(mg.GenreId)
                           ? genreDict[mg.GenreId]
                           : string.Empty
                }).ToList();

            return new MovieResponseDto
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                RentalPrice = movie.RentalPrice,
                Director = movie.Director ?? string.Empty,
                ReleaseYear = movie.ReleaseYear,
                Rating = movie.Rating,
                IsActive = movie.IsActive,
                Language = movie.Language,
                VideoUrl = movie.VideoUrl,
                ThumbnailUrl = movie.ThumbnailUrl,
                ViewCount = movie.ViewCount,
                CreatedAt = movie.CreatedAt,
                Genres = genres
            };
        }

        // ── ADD MOVIE ─────────────────────────────────────────────
        public async Task<MovieResponseDto> AddMovie(MovieCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new BusinessRuleViolationException("Movie title cannot be empty.");
            if (dto.RentalPrice <= 0)
                throw new BusinessRuleViolationException("Rental price must be greater than zero.");
            if (string.IsNullOrWhiteSpace(dto.Language))
                throw new BusinessRuleViolationException("Language is required.");

            await using var tx = await _context.Database.BeginTransactionAsync();

            var movie = new Movie
            {
                Title = dto.Title,
                Description = dto.Description ?? string.Empty,
                RentalPrice = dto.RentalPrice,
                Director = dto.Director ?? string.Empty,
                ReleaseYear = dto.ReleaseYear,
                Rating = dto.Rating,
                Language = dto.Language.Trim(),
                VideoUrl = dto.VideoUrl,
                ThumbnailUrl = dto.ThumbnailUrl,
                ViewCount = 0,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var created = await _movieRepository.AddAsync(movie);

            if (dto.GenreIds != null && dto.GenreIds.Any())
            {
                var uniqueIds = dto.GenreIds.Distinct().ToList();
                foreach (var genreId in uniqueIds)
                {
                    var genre = await _genreRepository.GetByIdAsync(genreId);
                    if (genre == null)
                        throw new EntityNotFoundException("Genre", genreId);

                    await _movieGenreRepository.AddAsync(
                        new MovieGenre { MovieId = created.Id, GenreId = genreId });
                }
            }

            await tx.CommitAsync();

            var movieGenres = await _movieGenreRepository.FindAsync(mg => mg.MovieId == created.Id);
            var genreDict = await BuildGenreDict();
            return MapToDto(created, movieGenres, genreDict);
        }

        // ── GET MOVIE ─────────────────────────────────────────────
        public async Task<MovieResponseDto> GetMovie(int id)
        {
            if (id <= 0)
                throw new BusinessRuleViolationException("Invalid movie ID.");

            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null)
                throw new EntityNotFoundException("Movie", id);

            var movieGenres = await _movieGenreRepository
                .FindAsync(mg => mg.MovieId == id);
            var genreDict = await BuildGenreDict();
            return MapToDto(movie, movieGenres, genreDict);
        }

        // ── GET ALL (active only, with filters + sorting) ─────────
        public async Task<PagedResultDto<MovieResponseDto>> GetAllMovies(
            PaginationDto pagination,
            int? genreId = null,
            string? language = null,
            double? minRating = null,
            string sortBy = "Id",
            string sortDirection = "desc")
        {
            // Start with active movies — join genres if filtering by genre
            IQueryable<Movie> query;

            if (genreId.HasValue)
            {
                query = _context.Movies
                    .Where(m => m.IsActive &&
                                m.MovieGenres.Any(mg => mg.GenreId == genreId.Value));
            }
            else
            {
                query = _context.Movies.Where(m => m.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(language))
                query = query.Where(m => m.Language.ToLower() == language.ToLower());

            if (minRating.HasValue)
                query = query.Where(m => m.Rating >= minRating.Value);

            // Sorting
            query = (sortBy?.ToLower(), sortDirection?.ToLower() == "desc") switch
            {
                ("title",  true)  => query.OrderByDescending(m => m.Title),
                ("title",  false) => query.OrderBy(m => m.Title),
                ("price",  true)  => query.OrderByDescending(m => m.RentalPrice),
                ("price",  false) => query.OrderBy(m => m.RentalPrice),
                ("rating", true)  => query.OrderByDescending(m => m.Rating),
                ("rating", false) => query.OrderBy(m => m.Rating),
                (_,        false) => query.OrderBy(m => m.Id),
                _                 => query.OrderByDescending(m => m.Id)
            };

            var totalCount = await query.CountAsync();

            if (totalCount == 0)
                return new PagedResultDto<MovieResponseDto>
                {
                    Data = Enumerable.Empty<MovieResponseDto>(),
                    TotalCount = 0,
                    PageNumber = pagination.PageNumber,
                    PageSize = pagination.PageSize,
                    TotalPages = 0,
                    HasNext = false,
                    HasPrevious = false
                };

            var movies = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            var movieIds = movies.Select(m => m.Id).ToList();
            var movieGenres = await _movieGenreRepository
                .FindAsync(mg => movieIds.Contains(mg.MovieId));
            var genreDict = await BuildGenreDict();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize);

            return new PagedResultDto<MovieResponseDto>
            {
                Data = movies.Select(m => MapToDto(m, movieGenres, genreDict)),
                TotalCount = totalCount,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize,
                TotalPages = totalPages,
                HasNext = pagination.PageNumber < totalPages,
                HasPrevious = pagination.PageNumber > 1
            };
        }

        public async Task<MovieResponseDto> UpdateMovie(int id, MovieUpdateDto dto)
        {
            if (id <= 0)
                throw new BusinessRuleViolationException("Invalid movie ID.");

            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null)
                throw new EntityNotFoundException("Movie", id);

            await using var tx = await _context.Database.BeginTransactionAsync();

            if (!string.IsNullOrWhiteSpace(dto.Title)) movie.Title = dto.Title;
            if (!string.IsNullOrWhiteSpace(dto.Description)) movie.Description = dto.Description;
            if (dto.RentalPrice > 0) movie.RentalPrice = dto.RentalPrice;
            if (!string.IsNullOrWhiteSpace(dto.Director)) movie.Director = dto.Director;
            if (dto.ReleaseYear > 0) movie.ReleaseYear = dto.ReleaseYear;
            if (dto.Rating > 0) movie.Rating = dto.Rating;
            if (dto.VideoUrl != null) movie.VideoUrl = dto.VideoUrl;
            if (dto.ThumbnailUrl != null) movie.ThumbnailUrl = dto.ThumbnailUrl;
            if (dto.IsActive.HasValue) movie.IsActive = dto.IsActive.Value;
            if (!string.IsNullOrWhiteSpace(dto.Language)) movie.Language = dto.Language.Trim();

            await _movieRepository.UpdateAsync(id, movie);

            // ── Transactional genre replace ───────────────────────
            if (dto.GenreIds != null)
            {
                // Delete all existing genre links for this movie
                await _context.MovieGenres
                    .Where(mg => mg.MovieId == id)
                    .ExecuteDeleteAsync();

                // Insert new unique genre links, validate each
                var uniqueIds = dto.GenreIds.Distinct().ToList();
                foreach (var genreId in uniqueIds)
                {
                    var genre = await _genreRepository.GetByIdAsync(genreId);
                    if (genre == null)
                        throw new EntityNotFoundException("Genre", genreId);

                    _context.MovieGenres.Add(new MovieGenre { MovieId = id, GenreId = genreId });
                }
                await _context.SaveChangesAsync();
            }

            await tx.CommitAsync();
            _cache.Remove(GenreCacheKey);

            var movieGenres = await _movieGenreRepository.FindAsync(mg => mg.MovieId == id);
            var genreDict = await BuildGenreDict();
            return MapToDto(movie, movieGenres, genreDict);
        }

        // ── SOFT DELETE (sets IsActive=false) ─────────────────────
        public async Task<MovieResponseDto> DeleteMovie(int id)
        {
            if (id <= 0)
                throw new BusinessRuleViolationException("Invalid movie ID.");

            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null)
                throw new EntityNotFoundException("Movie", id);

            movie.IsActive = false;
            await _movieRepository.UpdateAsync(id, movie);

            var movieGenres = await _movieGenreRepository
                .FindAsync(mg => mg.MovieId == id);
            var genreDict = await BuildGenreDict();
            return MapToDto(movie, movieGenres, genreDict);
        }

        // ── TRENDING ─────────────────────────────────────────────
        // Fetches all in one query ordered by ViewCount descending.
        // movieIds list comes from the controller (rental-based or fallback).
        public async Task<IEnumerable<MovieResponseDto>> GetTrendingMovies(
            List<int> movieIds)
        {
            if (!movieIds.Any())
                return Enumerable.Empty<MovieResponseDto>();

            // Single query — no N+1
            var movies = await _context.Movies
                .Where(m => movieIds.Contains(m.Id) && m.IsActive)
                .OrderByDescending(m => m.ViewCount)
                .ToListAsync();

            if (!movies.Any())
                return Enumerable.Empty<MovieResponseDto>();

            var ids = movies.Select(m => m.Id).ToList();
            var movieGenres = await _movieGenreRepository.FindAsync(mg => ids.Contains(mg.MovieId));
            var genreDict = await BuildGenreDict();

            return movies.Select(m => MapToDto(m, movieGenres, genreDict));
        }
        // ── INCREMENT VIEW COUNT ──────────────────────────────────
        // Uses ExecuteUpdateAsync — single SQL UPDATE, no entity load.
        // Only increments when IsActive = true (soft-deleted movies have IsActive = false).
        public async Task<bool> IncrementViewCountAsync(int id)
        {
            var updated = await _context.Movies
                .Where(m => m.Id == id && m.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.ViewCount, m => m.ViewCount + 1));

            return updated > 0;
        }
    }
}