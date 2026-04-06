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

        private static MovieResponseDto MapToDto(Movie movie,IEnumerable<MovieGenre> movieGenres,Dictionary<int, string> genreDict)
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

            // Duplicate check: same title + release year (case-insensitive, trimmed)
            var titleNorm = dto.Title.Trim().ToLower();

            var isDuplicate = await _context.Movies.AnyAsync(m =>
                m.Title.ToLower().Trim() == titleNorm &&
                m.ReleaseYear            == dto.ReleaseYear);

            if (isDuplicate)
                throw new BusinessRuleViolationException(
                    "Movie with the same name and release year already exists.");

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
        public async Task<PagedResultDto<MovieResponseDto>> GetMovies(GetMoviesRequestDto request)
        {
            IQueryable<Movie> query;

            if (request.GenreId.HasValue)
            {
                query = _context.Movies
                    .Where(m => m.IsActive &&
                                m.MovieGenres.Any(mg => mg.GenreId == request.GenreId.Value));
            }
            else
            {
                query = _context.Movies.Where(m => m.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(request.Language))
                query = query.Where(m => m.Language.ToLower() == request.Language.ToLower());

            if (request.MinRating.HasValue)
                query = query.Where(m => m.Rating >= request.MinRating.Value);

            query = (request.SortBy?.ToLower(), request.SortDirection?.ToLower() == "desc") switch
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
                    Data        = Enumerable.Empty<MovieResponseDto>(),
                    TotalCount  = 0,
                    PageNumber  = request.PageNumber,
                    PageSize    = request.PageSize,
                    TotalPages  = 0,
                    HasNext     = false,
                    HasPrevious = false
                };

            var movies = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var movieIds    = movies.Select(m => m.Id).ToList();
            var movieGenres = await _movieGenreRepository.FindAsync(mg => movieIds.Contains(mg.MovieId));
            var genreDict   = await BuildGenreDict();
            var totalPages  = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            return new PagedResultDto<MovieResponseDto>
            {
                Data        = movies.Select(m => MapToDto(m, movieGenres, genreDict)),
                TotalCount  = totalCount,
                PageNumber  = request.PageNumber,
                PageSize    = request.PageSize,
                TotalPages  = totalPages,
                HasNext     = request.PageNumber < totalPages,
                HasPrevious = request.PageNumber > 1
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

        // ── DELETE (sets IsActive=false) ──────────────────────────
        public async Task<MovieResponseDto> DeleteMovie(int id)
        {
            if (id <= 0)
                throw new BusinessRuleViolationException("Invalid movie ID.");

            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null)
                throw new EntityNotFoundException("Movie", id);

            // Block delete if the movie has ANY rental history (active or past)
            var hasRentals = await _context.Rentals
                .AnyAsync(r => r.MovieId == id);

            if (hasRentals)
                throw new BusinessRuleViolationException(
                    "Movie is currently rented and cannot be deleted.");

            movie.IsActive = false;
            await _movieRepository.UpdateAsync(id, movie);

            var movieGenres = await _movieGenreRepository
                .FindAsync(mg => mg.MovieId == id);
            var genreDict = await BuildGenreDict();
            return MapToDto(movie, movieGenres, genreDict);
        }

        // ── TRENDING ─────────────────────────────────────────────
        // Controller no longer queries DB — service owns the logic entirely
        public async Task<IEnumerable<MovieResponseDto>> GetTrendingMovies(int top = 10)
        {
            if (top < 1) top = 10;
            if (top > 50) top = 50;

            var movies = await _context.Movies
                .Where(m => m.IsActive)
                .OrderByDescending(m => m.ViewCount)
                .ThenByDescending(m => m.CreatedAt)
                .Take(top)
                .ToListAsync();

            if (!movies.Any())
                return Enumerable.Empty<MovieResponseDto>();

            var ids        = movies.Select(m => m.Id).ToList();
            var movieGenres = await _movieGenreRepository.FindAsync(mg => ids.Contains(mg.MovieId));
            var genreDict  = await BuildGenreDict();

            return movies.Select(m => MapToDto(m, movieGenres, genreDict));
        }
        // ── INCREMENT VIEW COUNT ──────────────────────────────────
        public async Task<bool> IncrementViewCountAsync(int id)
        {
            // Validate existence first — gives controller a clear result
            var exists = await _context.Movies.AnyAsync(m => m.Id == id && m.IsActive);
            if (!exists) return false;

            await _context.Movies
                .Where(m => m.Id == id && m.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.ViewCount, m => m.ViewCount + 1));

            return true;
        }

        // ── UPLOAD VIDEO ──────────────────────────────────────────
        public async Task<string> UploadVideoAsync(int movieId, IFormFile file, string webRootPath)
        {
            var movie = await _movieRepository.GetByIdAsync(movieId)
                ?? throw new EntityNotFoundException("Movie", movieId);

            var folder   = Path.Combine(webRootPath, "uploads", "movies");
            Directory.CreateDirectory(folder);

            var ext      = Path.GetExtension(file.FileName).ToLower();
            var fileName = $"movie_{movieId}_{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(folder, fileName);

            try
            {
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
            }
            catch (IOException ex)
            {
                if (File.Exists(filePath)) File.Delete(filePath);
                throw new BusinessRuleViolationException($"Failed to save video file: {ex.Message}");
            }

            var videoUrl = $"/uploads/movies/{fileName}";
            movie.VideoUrl = videoUrl;
            await _movieRepository.UpdateAsync(movieId, movie);
            return videoUrl;
        }

        // ── UPLOAD THUMBNAIL ──────────────────────────────────────
        public async Task<string> UploadThumbnailAsync(int movieId, IFormFile file, string webRootPath)
        {
            var movie = await _movieRepository.GetByIdAsync(movieId)
                ?? throw new EntityNotFoundException("Movie", movieId);

            var folder   = Path.Combine(webRootPath, "uploads", "thumbnails");
            Directory.CreateDirectory(folder);

            var ext          = Path.GetExtension(file.FileName).ToLower();
            var fileName     = $"thumb_{movieId}_{Guid.NewGuid()}{ext}";
            var filePath     = Path.Combine(folder, fileName);

            try
            {
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
            }
            catch (IOException ex)
            {
                if (File.Exists(filePath)) File.Delete(filePath);
                throw new BusinessRuleViolationException($"Failed to save thumbnail file: {ex.Message}");
            }

            var thumbnailUrl = $"/uploads/thumbnails/{fileName}";
            movie.ThumbnailUrl = thumbnailUrl;
            await _movieRepository.UpdateAsync(movieId, movie);
            return thumbnailUrl;
        }
    }
}