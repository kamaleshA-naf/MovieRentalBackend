using MovieRentalApp.Exceptions;

namespace MovieTesting
{
    /// <summary>
    /// Covers all 7 custom exception classes — constructors, messages, properties.
    /// </summary>
    public class ExceptionTests
    {
        // ── BusinessRuleViolationException ────────────────────────

        [Fact]
        public void BusinessRuleViolation_Message_IsPreserved()
        {
            var ex = new BusinessRuleViolationException("rule broken");
            Assert.Equal("rule broken", ex.Message);
        }

        [Fact]
        public void BusinessRuleViolation_IsException()
        {
            Assert.IsAssignableFrom<Exception>(new BusinessRuleViolationException("x"));
        }

        // ── DuplicateEntityException ──────────────────────────────

        [Fact]
        public void DuplicateEntity_Message_IsPreserved()
        {
            var ex = new DuplicateEntityException("already exists");
            Assert.Equal("already exists", ex.Message);
        }

        // ── EntityNotFoundException ───────────────────────────────

        [Fact]
        public void EntityNotFound_WithNameAndKey_FormatsMessage()
        {
            var ex = new EntityNotFoundException("Movie", 42);
            Assert.Contains("Movie", ex.Message);
            Assert.Contains("42", ex.Message);
        }

        [Fact]
        public void EntityNotFound_WithMessageOnly_IsPreserved()
        {
            var ex = new EntityNotFoundException("custom message");
            Assert.Equal("custom message", ex.Message);
        }

        // ── ForbiddenException ────────────────────────────────────

        [Fact]
        public void Forbidden_DefaultMessage_ContainsPermission()
        {
            var ex = new ForbiddenException();
            Assert.Contains("permission", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Forbidden_CustomMessage_IsPreserved()
        {
            var ex = new ForbiddenException("no access");
            Assert.Equal("no access", ex.Message);
        }

        // ── MovieCurrentlyRentedException ─────────────────────────

        [Fact]
        public void MovieCurrentlyRented_SingleCustomer_SingularMessage()
        {
            var ex = new MovieCurrentlyRentedException("Inception", 1);
            Assert.Equal(1, ex.ActiveRentalCount);
            Assert.Contains("1 customer", ex.Message);
            Assert.DoesNotContain("customers", ex.Message);
        }

        [Fact]
        public void MovieCurrentlyRented_MultipleCustomers_PluralMessage()
        {
            var ex = new MovieCurrentlyRentedException("Inception", 3);
            Assert.Equal(3, ex.ActiveRentalCount);
            Assert.Contains("3 customers", ex.Message);
        }

        // ── UnableToCreateEntityException ────────────────────────

        [Fact]
        public void UnableToCreate_EntityNameOnly_FormatsMessage()
        {
            var ex = new UnableToCreateEntityException("Rental");
            Assert.Contains("Rental", ex.Message);
        }

        [Fact]
        public void UnableToCreate_WithReason_IncludesReason()
        {
            var ex = new UnableToCreateEntityException("Payment", "DB error");
            Assert.Contains("Payment", ex.Message);
            Assert.Contains("DB error", ex.Message);
        }

        // ── UnauthorizedException ─────────────────────────────────

        [Fact]
        public void Unauthorized_DefaultMessage_ContainsAuthorized()
        {
            var ex = new UnauthorizedException();
            Assert.Contains("authorized", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Unauthorized_CustomMessage_IsPreserved()
        {
            var ex = new UnauthorizedException("bad token");
            Assert.Equal("bad token", ex.Message);
        }
    }
}
