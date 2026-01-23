namespace DeviceManagerMVC.Models
{
    public class Device
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; }
        public string Model { get; set; }
        public string AssignedTo { get; set; }
        public string DeviceType { get; set; } // PC, Phone, Tablet, etc.
        public string Team { get; set; } // IT, HR, Finance, etc.


    }
}
