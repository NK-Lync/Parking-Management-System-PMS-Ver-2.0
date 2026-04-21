namespace ParkingManagementSystem.ViewModels
{
    public class ParkingZoneSummaryViewModel
    {
        public string ZoneName { get; set; } = string.Empty;
        public int TotalSlots { get; set; }
        public int OccupiedSlots { get; set; }
        public int MaintenanceSlots { get; set; }
        public int AvailableSlots => TotalSlots - OccupiedSlots;
    }
}
