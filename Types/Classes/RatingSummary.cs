namespace Types.Classes;

public class RatingSummary
{
    private int _rating = 0; 
    public int Rating {
        get {
            return _rating;
        }
        set {
            if (value < 0 || value > 5)
                throw new OverflowException("Рейтинг не может быть больше пяти и меньше нуля");
            _rating = value;
        }
    }
    public string Review { get; set; }

    public RatingSummary(int rating, string review)
    {
        Rating = rating;
        Review = review;
    }
}