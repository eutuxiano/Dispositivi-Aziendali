namespace DeviceManagerMVC.Models
{
    public class Device
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; }
        public string Model { get; set; }
        public string AssignedTo { get; set; }
        public DateTime PurchaseDate { get; set; }
        public bool IsActive { get; set; }
    }
}
