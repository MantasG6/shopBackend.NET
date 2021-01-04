namespace shop.Models
{
    public class Reservation
    {
        public int id { get; set; }
        public int productId { get; set; }
        public int quantity { get; set; }
        public int clientPersonalCode { get; set; }
        public string creationDate { get; set; }
    }
}
