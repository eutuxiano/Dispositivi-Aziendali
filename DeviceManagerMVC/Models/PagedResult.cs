namespace DeviceManagerMVC.Models
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }
}

