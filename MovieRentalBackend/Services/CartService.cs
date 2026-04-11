using MovieRentalApp.Contexts;
using MovieRentalApp.Exceptions;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Models;
using MovieRentalApp.Models.DTOs;
using System.Diagnostics;

namespace MovieRentalApp.Services
{
    [DebuggerNonUserCode]
    public class CartService : ICartService
    {
        private readonly IRepository<int, Cart> _cartRepository;
        private readonly IRepository<int, Movie> _movieRepository;
        private readonly IRepository<int, User> _userRepository;
        private readonly IRepository<int, Rental> _rentalRepository;
        private readonly IRepository<int, Payment> _paymentRepository;
        private readonly IRepository<int, MovieGenre> _movieGenreRepository;
        private readonly IRepository<int, Genre> _genreRepository;

        public CartService(
            IRepository<int, Cart> cartRepository,
            IRepository<int, Movie> movieRepository,
            IRepository<int, User> userRepository,
            IRepository<int, Rental> rentalRepository,
            IRepository<int, Payment> paymentRepository,
            IRepository<int, MovieGenre> movieGenreRepository,
            IRepository<int, Genre> genreRepository)
        {
            _cartRepository = cartRepository;
            _movieRepository = movieRepository;
            _userRepository = userRepository;
            _rentalRepository = rentalRepository;
            _paymentRepository = paymentRepository;
            _movieGenreRepository = movieGenreRepository;
            _genreRepository = genreRepository;
        }

        // ── Add to Cart ───────────────────────────────────────────
        public async Task<CartResponseDto> AddToCart(CartAddDto dto)
        {
            var user = await _userRepository.GetByIdAsync(dto.UserId);
            if (user == null)
                throw new EntityNotFoundException("User", dto.UserId);

            var movie = await _movieRepository.GetByIdAsync(dto.MovieId);
            if (movie == null)
                throw new EntityNotFoundException("Movie", dto.MovieId);

            if (!movie.IsActive)
                throw new BusinessRuleViolationException(
                    $"'{movie.Title}' is not available for rent.");

            // Duplicate cart check
            var existing = await _cartRepository.FindAsync(
                c => c.UserId == dto.UserId && c.MovieId == dto.MovieId);
            if (existing.Any())
                throw new DuplicateEntityException(
                    $"'{movie.Title}' is already in your cart.");

            // Already actively rented check — use StoredStatus + ExpiryDate
            var rented = await _rentalRepository.FindAsync(
                r => r.UserId == dto.UserId &&
                     r.MovieId == dto.MovieId &&
                     r.StoredStatus == "Active" &&
                     r.ExpiryDate > DateTime.UtcNow);
            if (rented.Any())
                throw new BusinessRuleViolationException(
                    $"You already have '{movie.Title}' rented.");

            var cart = new Cart
            {
                UserId = dto.UserId,
                MovieId = dto.MovieId,
                DurationDays = dto.DurationDays > 0 ? dto.DurationDays : 7,
                AddedAt = DateTime.UtcNow
            };
            await _cartRepository.AddAsync(cart);

            return await BuildDto(cart, movie);
        }

        // ── Get Cart By User ──────────────────────────────────────
        public async Task<IEnumerable<CartResponseDto>> GetCartByUser(
            int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException("User", userId);

            var items = await _cartRepository
                .GetAllWithIncludeAsync(c => c.Movie);

            var userItems = items
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.AddedAt)
                .ToList();

            var result = new List<CartResponseDto>();
            foreach (var item in userItems)
                result.Add(await BuildDto(item, item.Movie!));

            return result;
        }

        // ── Remove From Cart ──────────────────────────────────────
        public async Task RemoveFromCart(int cartId)
        {
            var item = await _cartRepository.GetByIdAsync(cartId);
            if (item == null)
                throw new EntityNotFoundException("Cart item", cartId);

            await _cartRepository.DeleteAsync(cartId);
        }

        // ── Update Duration ───────────────────────────────────────
        public async Task<CartResponseDto> UpdateDuration(
            int cartId, CartUpdateDto dto)
        {
            var item = await _cartRepository.GetByIdAsync(cartId);
            if (item == null)
                throw new EntityNotFoundException("Cart item", cartId);

            item.DurationDays = dto.DurationDays;
            await _cartRepository.UpdateAsync(cartId, item);

            var movie = await _movieRepository.GetByIdAsync(item.MovieId);
            if (movie == null)
                throw new EntityNotFoundException("Movie", item.MovieId);

            return await BuildDto(item, movie);
        }

        // ── Checkout ──────────────────────────────────────────────
        public async Task<CartCheckoutResultDto> Checkout(CartCheckoutDto dto)
        {
            var items = await _cartRepository
                .GetAllWithIncludeAsync(c => c.Movie);

            var userCart = items
                .Where(c => c.UserId == dto.UserId)
                .ToList();

            if (!userCart.Any())
                throw new BusinessRuleViolationException("Your cart is empty.");

            var user = await _userRepository.GetByIdAsync(dto.UserId);
            if (user == null)
                throw new EntityNotFoundException("User", dto.UserId);

            var rentals = new List<RentalResponseDto>();
            var skipped = new List<string>();
            decimal total = 0;

            foreach (var cartItem in userCart)
            {
                var movie = cartItem.Movie!;

                // Skip already rented — use StoredStatus + ExpiryDate
                var alreadyRented = await _rentalRepository.FindAsync(
                    r => r.UserId == dto.UserId &&
                         r.MovieId == movie.Id &&
                         r.StoredStatus == "Active" &&
                         r.ExpiryDate > DateTime.UtcNow);

                if (alreadyRented.Any())
                {
                    skipped.Add(movie.Title);
                    continue;
                }

                // ✅ Use StoredStatus — NOT Status (computed, read-only)
                var rental = new Rental
                {
                    UserId = dto.UserId,
                    MovieId = movie.Id,
                    RentalDate = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddDays(cartItem.DurationDays),
                    StoredStatus = "Active"   // ← correct
                };
                await _rentalRepository.AddAsync(rental);

                var amount = movie.RentalPrice * cartItem.DurationDays;

                var validMethods = new[] { "UPI", "Card", "NetBanking" };
                var method = validMethods.Contains(dto.PaymentMethod) ? dto.PaymentMethod : "UPI";

                await _paymentRepository.AddAsync(new Payment
                {
                    UserId = dto.UserId,
                    RentalId = rental.Id,
                    MovieId = movie.Id,
                    Amount = amount,
                    Method = method,
                    Status = "Completed",
                    PaymentDate = DateTime.UtcNow
                });

                total += amount;

                rentals.Add(new RentalResponseDto
                {
                    Id = rental.Id,
                    UserId = rental.UserId,
                    UserName = user.UserName,
                    MovieId = rental.MovieId,
                    MovieTitle = movie.Title,
                    RentalDate = rental.RentalDate,
                    ExpiryDate = rental.ExpiryDate,
                    Status = rental.Status   // ← computed — safe for reading
                });
            }

            await ClearCart(dto.UserId);

            return new CartCheckoutResultDto
            {
                TotalMovies = rentals.Count,
                TotalAmount = total,
                SkippedMovies = skipped,
                Rentals = rentals
            };
        }

        // ── Clear Cart ────────────────────────────────────────────
        public async Task ClearCart(int userId)
        {
            var items = await _cartRepository
                .FindAsync(c => c.UserId == userId);
            foreach (var item in items)
                await _cartRepository.DeleteAsync(item.Id);
        }

       

        // ── Mapper ────────────────────────────────────────────────
        private async Task<CartResponseDto> BuildDto(Cart cart, Movie movie)
        {
            var movieGenres = await _movieGenreRepository
                .FindAsync(mg => mg.MovieId == movie.Id);
            var allGenres = await _genreRepository.GetAllAsync();
            var genreDict = allGenres.ToDictionary(
                g => g.Id, g => g.Name ?? string.Empty);

            var genres = movieGenres.Select(mg => new GenreResponseDto
            {
                Id = mg.GenreId,
                Name = genreDict.ContainsKey(mg.GenreId)
                       ? genreDict[mg.GenreId]
                       : string.Empty
            }).ToList();

            return new CartResponseDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                MovieId = cart.MovieId,
                MovieTitle = movie.Title,
                ThumbnailUrl = movie.ThumbnailUrl,
                RentalPrice = movie.RentalPrice,
                DurationDays = cart.DurationDays,
                TotalCost = movie.RentalPrice * cart.DurationDays,
                AddedAt = cart.AddedAt,
                Genres = genres
            };
        }
    }
}