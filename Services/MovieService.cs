using Microsoft.EntityFrameworkCore;
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

        private const string GenreCacheKey = "AllGenres";
        private static readonly TimeSpan CacheDuration =
            TimeSpan.FromMinutes(10);

        public MovieService(
            IRepository<int, Movie> movieRepository,
            IRepository<int, MovieGenre> movieGenreRepository,
            IRepository<int, Genre> genreRepository,
            IMemoryCache cache)
        {
            _movieRepository = movieRepository;
            _movieGenreRepository = movieGenreRepository;
            _genreRepository = genreRepository;
            _cache = cache;
        }

        private async Task<Dictionary<int, string>> BuildGenreDict()
        {
            if (_cache.TryGetValue(
                    GenreCacheKey,
                    out Dictionary<int, string>? cached)
                && cached != null)
                return cached;

            var allGenres = await _genreRepository.GetAllAsync();
            var dict = allGenres.ToDictionary(
                g => g.Id,
                g => g.Name ?? string.Empty);

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
                VideoUrl = movie.VideoUrl,
                ThumbnailUrl = movie.ThumbnailUrl,
                ViewCount = movie.ViewCount,
                CreatedAt = movie.CreatedAt,
                Genres = genres
            };
        }

        public async Task<MovieResponseDto> AddMovie(MovieCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new BusinessRuleViolationException(
                    "Movie title cannot be empty.");

            if (dto.RentalPrice <= 0)
                throw new BusinessRuleViolationException(
                    "Rental price must be greater than zero.");

            var movie = new Movie
            {
                Title = dto.Title,
                Description = dto.Description ?? string.Empty,
                RentalPrice = dto.RentalPrice,
                Director = dto.Director ?? string.Empty,
                ReleaseYear = dto.ReleaseYear,
                Rating = dto.Rating,
                VideoUrl = dto.VideoUrl,
                ThumbnailUrl = dto.ThumbnailUrl,
                ViewCount = 0,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var created = await _movieRepository.AddAsync(movie);

            if (dto.GenreIds != null && dto.GenreIds.Any())
            {
                foreach (var genreId in dto.GenreIds)
                {
                    var genre = await _genreRepository
                        .GetByIdAsync(genreId);
                    if (genre != null)
                        await _movieGenreRepository.AddAsync(
                            new MovieGenre
                            {
                                MovieId = created.Id,
                                GenreId = genreId
                            });
                }
            }

            var movieGenres = await _movieGenreRepository
                .FindAsync(mg => mg.MovieId == created.Id);
            var genreDict = await BuildGenreDict();
            return MapToDto(created, movieGenres, genreDict);
        }

        public async Task<MovieResponseDto> GetMovie(int id)
        {
            if (id <= 0)
                throw new BusinessRuleViolationException(
                    "Invalid movie ID.");

            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null)
                throw new EntityNotFoundException("Movie", id);

            var movieGenres = await _movieGenreRepository
                .FindAsync(mg => mg.MovieId == id);
            var genreDict = await BuildGenreDict();
            return MapToDto(movie, movieGenres, genreDict);
        }

        public async Task<PagedResultDto<MovieResponseDto>> GetAllMovies(
            PaginationDto pagination)
        {
            var query = _movieRepository
                .GetQueryable()
                .Where(m => m.IsActive)
                .OrderByDescending(m => m.Id);

            var totalCount = await query.CountAsync();
            if (totalCount == 0)
                throw new EntityNotFoundException("No movies found.");

            var movies = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            var movieIds = movies.Select(m => m.Id).ToList();
            var movieGenres = await _movieGenreRepository
                .FindAsync(mg => movieIds.Contains(mg.MovieId));
            var genreDict = await BuildGenreDict();
            var totalPages = (int)Math.Ceiling(
                totalCount / (double)pagination.PageSize);

            return new PagedResultDto<MovieResponseDto>
            {
                Data = movies.Select(
                    m => MapToDto(m, movieGenres, genreDict)),
                TotalCount = totalCount,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize,
                TotalPages = totalPages,
                HasNext = pagination.PageNumber < totalPages,
                HasPrevious = pagination.PageNumber > 1
            };
        }

        public async Task<PagedResultDto<MovieResponseDto>> SearchMovies(
            string keyword, PaginationDto pagination)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                throw new BusinessRuleViolationException(
                    "Search keyword cannot be empty.");

            var kw = keyword.ToLower();
            var query = _movieRepository
                .GetQueryable()
                .Where(m => m.IsActive && (
                    m.Title.ToLower().Contains(kw) ||
                    (m.Director ?? "").ToLower().Contains(kw) ||
                    m.Description.ToLower().Contains(kw)))
                .OrderByDescending(m => m.Id);

            var totalCount = await query.CountAsync();
            if (totalCount == 0)
                throw new EntityNotFoundException(
                    $"No movies found for '{keyword}'.");

            var movies = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            var movieIds = movies.Select(m => m.Id).ToList();
            var movieGenres = await _movieGenreRepository
                .FindAsync(mg => movieIds.Contains(mg.MovieId));
            var genreDict = await BuildGenreDict();
            var totalPages = (int)Math.Ceiling(
                totalCount / (double)pagination.PageSize);

            return new PagedResultDto<MovieResponseDto>
            {
                Data = movies.Select(
                    m => MapToDto(m, movieGenres, genreDict)),
                TotalCount = totalCount,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize,
                TotalPages = totalPages,
                HasNext = pagination.PageNumber < totalPages,
                HasPrevious = pagination.PageNumber > 1
            };
        }

        public async Task<PagedResultDto<MovieResponseDto>> GetMoviesByGenre(
            int genreId, PaginationDto pagination)
        {
            if (genreId <= 0)
                throw new BusinessRuleViolationException(
                    "Invalid genre ID.");

            var genreDict = await BuildGenreDict();
            if (!genreDict.ContainsKey(genreId))
                throw new EntityNotFoundException("Genre", genreId);

            var genreMovies = await _movieGenreRepository
                .FindAsync(mg => mg.GenreId == genreId);
            if (!genreMovies.Any())
                throw new EntityNotFoundException(
                    $"No movies found for genre ID {genreId}.");

            var movieIds = genreMovies
                .Select(mg => mg.MovieId).ToList();

            var query = _movieRepository
                .GetQueryable()
                .Where(m => movieIds.Contains(m.Id) && m.IsActive)
                .OrderByDescending(m => m.Id);

            var totalCount = await query.CountAsync();
            var movies = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            var pagedIds = movies.Select(m => m.Id).ToList();
            var movieGenres = await _movieGenreRepository
                .FindAsync(mg => pagedIds.Contains(mg.MovieId));
            var totalPages = (int)Math.Ceiling(
                totalCount / (double)pagination.PageSize);

            return new PagedResultDto<MovieResponseDto>
            {
                Data = movies.Select(
                    m => MapToDto(m, movieGenres, genreDict)),
                TotalCount = totalCount,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize,
                TotalPages = totalPages,
                HasNext = pagination.PageNumber < totalPages,
                HasPrevious = pagination.PageNumber > 1
            };
        }

        public async Task<MovieResponseDto> UpdateMovie(
            int id, MovieUpdateDto dto)
        {
            if (id <= 0)
                throw new BusinessRuleViolationException(
                    "Invalid movie ID.");

            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null)
                throw new EntityNotFoundException("Movie", id);

            if (!string.IsNullOrWhiteSpace(dto.Title))
                movie.Title = dto.Title;
            if (!string.IsNullOrWhiteSpace(dto.Description))
                movie.Description = dto.Description;
            if (dto.RentalPrice > 0)
                movie.RentalPrice = dto.RentalPrice;
            if (!string.IsNullOrWhiteSpace(dto.Director))
                movie.Director = dto.Director;
            if (dto.ReleaseYear > 0)
                movie.ReleaseYear = dto.ReleaseYear;
            if (dto.Rating > 0)
                movie.Rating = dto.Rating;
            if (dto.VideoUrl != null)
                movie.VideoUrl = dto.VideoUrl;
            if (dto.ThumbnailUrl != null)
                movie.ThumbnailUrl = dto.ThumbnailUrl;
            if (dto.IsActive.HasValue)
                movie.IsActive = dto.IsActive.Value;

            await _movieRepository.UpdateAsync(id, movie);

            var movieGenres = await _movieGenreRepository
                .FindAsync(mg => mg.MovieId == id);
            var genreDict = await BuildGenreDict();
            return MapToDto(movie, movieGenres, genreDict);
        }

        public async Task<MovieResponseDto> DeleteMovie(int id)
        {
            if (id <= 0)
                throw new BusinessRuleViolationException("Invalid movie ID.");

            var movie = await _movieRepository.GetByIdAsync(id);
            if (movie == null)
                throw new EntityNotFoundException("Movie", id);

            // Soft delete — keeps foreign key data intact
            movie.IsActive = false;
            await _movieRepository.UpdateAsync(id, movie);

            // Need all 3 params for MapToDto
            var movieGenres = await _movieGenreRepository
                .FindAsync(mg => mg.MovieId == id);
            var genreDict = await BuildGenreDict();

            return MapToDto(movie, movieGenres, genreDict);
        }


        public async Task<IEnumerable<MovieResponseDto>> GetTrendingMovies(
            List<int> movieIds)
        {
            var genreDict = await BuildGenreDict();
            var result = new List<MovieResponseDto>();

            foreach (var id in movieIds)
            {
                var movie = await _movieRepository.GetByIdAsync(id);
                if (movie == null || !movie.IsActive) continue;

                var genres = await _movieGenreRepository
                    .FindAsync(mg => mg.MovieId == id);

                result.Add(MapToDto(movie, genres, genreDict));
            }

            return result;
        }
    }
}