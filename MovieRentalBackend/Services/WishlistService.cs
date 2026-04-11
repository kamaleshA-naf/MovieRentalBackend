using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;
using System.Diagnostics;

namespace MovieRentalApp.Services
{
    [DebuggerNonUserCode]
    public class WishlistService : IWishlistService
    {
        private readonly IRepository<int, Wishlist> _wishlistRepository;
        private readonly IRepository<int, Movie> _movieRepository;
        private readonly IRepository<int, User> _userRepository;

        public WishlistService(
            IRepository<int, Wishlist> wishlistRepository,
            IRepository<int, Movie> movieRepository,
            IRepository<int, User> userRepository)
        {
            _wishlistRepository = wishlistRepository;
            _movieRepository = movieRepository;
            _userRepository = userRepository;
        }

        public async Task<WishlistResponseDto> AddToWishlist(WishlistCreateDto dto)
        {
            var user = await _userRepository.GetByIdAsync(dto.UserId);
            if (user == null)
                throw new EntityNotFoundException("User", dto.UserId);

            var movie = await _movieRepository.GetByIdAsync(dto.MovieId);
            if (movie == null)
                throw new EntityNotFoundException("Movie", dto.MovieId);

            var existing = await _wishlistRepository.FindAsync(
                w => w.UserId == dto.UserId && w.MovieId == dto.MovieId);
            if (existing.Any())
                throw new DuplicateEntityException(
                    $"'{movie.Title}' is already in your wishlist.");

            var wishlist = new Wishlist
            {
                UserId = dto.UserId,
                MovieId = dto.MovieId,
                AddedDate = DateTime.UtcNow
            };
            await _wishlistRepository.AddAsync(wishlist);
            return MapToDto(wishlist, movie);
        }

        public async Task<IEnumerable<WishlistResponseDto>> GetWishlistByUser(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException("User", userId);

            // Include Movie so ThumbnailUrl is available
            var wishlists = await _wishlistRepository
                .GetAllWithIncludeAsync(w => w.Movie);

            return wishlists
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedDate)
                .Select(w => MapToDto(w, w.Movie!));
        }

        public async Task RemoveFromWishlist(int id)
        {
            var item = await _wishlistRepository.GetByIdAsync(id);
            if (item == null)
                throw new EntityNotFoundException("Wishlist item", id);

            await _wishlistRepository.DeleteAsync(id);
        }

        // Safe removal by userId+movieId — used when renting, no exception if not found
        public async Task RemoveByUserAndMovieAsync(int userId, int movieId)
        {
            var items = await _wishlistRepository.FindAsync(
                w => w.UserId == userId && w.MovieId == movieId);
            var item = items.FirstOrDefault();
            if (item != null)
                await _wishlistRepository.DeleteAsync(item.Id);
        }

        // ── KEY FIX: now reads movie.ThumbnailUrl ────────────────
        private static WishlistResponseDto MapToDto(Wishlist w, Movie movie) => new()
        {
            Id = w.Id,
            UserId = w.UserId,
            MovieId = w.MovieId,
            MovieTitle = movie.Title,
            RentalPrice = movie.RentalPrice,
            AddedDate = w.AddedDate,
            ThumbnailUrl = movie.ThumbnailUrl   
        };
    }
}