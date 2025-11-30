namespace Application.DTOs;
public class ReviewDto
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty; // Could be customer's name
}

/// <summary>
/// Data required to create a new review.
/// </summary>
public class CreateReviewDto
{
    public int ProductId { get; set; }
    public Guid CustomerId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}
