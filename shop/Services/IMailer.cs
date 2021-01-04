using System.Threading.Tasks;

namespace shop.Services
{
    public interface IMailer
    {
        Task SendEmailAsync(string subject, string body);
    }
}
