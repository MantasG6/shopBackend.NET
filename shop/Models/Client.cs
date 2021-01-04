using System.ComponentModel.DataAnnotations;

namespace shop.Models
{
    public class Client
    {
        [Key]
        public int personalCode { get; set; }
        public string creationDate { get; set; }
        public string lastActivityDate { get; set; }
    }
}
