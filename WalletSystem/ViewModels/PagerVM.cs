namespace WalletSystem.ViewModels;

public class PagerVM
{
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public string BaseUrl { get; set; } = "";
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
