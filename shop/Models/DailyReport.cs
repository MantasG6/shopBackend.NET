using System.ComponentModel.DataAnnotations;

namespace shop.Models
{
    public class DailyReport
    {
        [Key]
        public string date { get; set; }
        public string successfulReservations { get; set; }
        public string failedReservations { get; set; }
        public string newCustomers { get; set; }
        public string returningCustomers { get; set; }
        public string stockState { get; set; }
    }
}
