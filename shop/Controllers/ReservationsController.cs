using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using shop.Models;
using shop.Services;

namespace shop.Controllers
{
    [Route("shop/v1/reserve")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDBContext _context;
        private readonly ILogger<Reservation> _logger;
        private readonly IMailer _mailer;

        public ReservationsController(AppDBContext context, ILogger<Reservation> logger, IMailer mailer)
        {
            _context = context;
            _logger = logger;
            _mailer = mailer;
        }

        // GET: shop/v1/reserve/1/1
        [HttpGet("{id}/{clientPersonalCode}")]
        public async Task<ActionResult<Reservation>> GetReservation(int id, int clientPersonalCode)
        {
            var reservation = await _context.Reservations.FindAsync(id, clientPersonalCode);

            if (reservation == null)
            {
                return NotFound();
            }

            return reservation;
        }

        // GET: shop/v1/reserve/client/1
        [HttpGet("client/{clientPersonalCode}")]
        public async Task<ActionResult<IEnumerable<Reservation>>> GetReservationsC(int clientPersonalCode)
        {
            var reservations = _context.Reservations.Where(r => r.clientPersonalCode == clientPersonalCode);
            return await reservations.ToListAsync();
        }

        // GET: shop/v1/reserve/reservation/1
        [HttpGet("reservation/{id}")]
        public async Task<ActionResult<IEnumerable<Reservation>>> GetReservationsR(int id)
        {
            var reservations = _context.Reservations.Where(r => r.id == id);
            return await reservations.ToListAsync();
        }

        // PUT: shop/v1/reserve/3/1/50
        [HttpPut("{productId}/{clientPersonalCode}/{quantity}")]
        public async Task<ActionResult<Reservation>> PutReservation(int productId, int clientPersonalCode, int quantity)
        {
            Client client = await _context.Clients.FindAsync(clientPersonalCode);
            if (client == null)
            {
                _logger.LogError("No client with given client personal code exists");
                return NotFound();
            }
            Reservation reservation = await _context.Reservations
                .Where(r => r.clientPersonalCode == clientPersonalCode && r.productId == productId)
                .FirstOrDefaultAsync();
            if (reservation == null)
            {
                _logger.LogError("No reservation with given data found");
                return NotFound();
            }

            int resultQuantity = quantity - reservation.quantity;

            client.lastActivityDate = DateTime.Now.ToShortDateString();
            _context.Entry(client).State = EntityState.Modified;

            if (resultQuantity == 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogWarning("Given quantity is the same, reservation not changed");
                return reservation;
            }

            reservation.quantity = quantity;

            Product product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                _logger.LogError("Wrong productId");
                return NotFound();
            }
            // calculating new product stock
            int newStock = product.stock - resultQuantity;
            if (newStock < 0)
            {
                _logger.LogError("Not enough products in stock");
                return BadRequest();
            }
            product.stock = newStock;
            // updating product stock
            _context.Entry(product).State = EntityState.Modified;
            _context.Entry(reservation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                string emailBody = "Reservation:\n";
                emailBody += String.Format("ID: {0}\n", reservation.id);
                emailBody += String.Format("Product ID: {0}\n", reservation.productId);
                emailBody += String.Format("New quantity: {0}\n", reservation.quantity);
                emailBody += String.Format("Client personal code: {0}\n", reservation.clientPersonalCode);
                await _mailer.SendEmailAsync("Reservation quantity changed", emailBody);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservationExists(reservation.id, reservation.clientPersonalCode))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return reservation;
        }

        // POST: shop/v1/reserve/1/1/3
        [HttpPost("{productId}/{clientPersonalCode}/{quantity}")]
        public async Task<ActionResult<Reservation>> PostReservation(int productId, int clientPersonalCode, int quantity)
        {
            // checking if client with given personal code exists
            Client client = await _context.Clients.FindAsync(clientPersonalCode);
            if (client == null)
            {
                _logger.LogWarning("No client with given personal code exists, creating new client");
                client = new Client();
                client.personalCode = clientPersonalCode;
                client.creationDate = DateTime.Now.ToShortDateString();
                client.lastActivityDate = DateTime.Now.ToShortDateString();
                _context.Clients.Add(client);
            }
            // finding the product to reserve
            Product product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                _logger.LogError("Wrong productId");
                return NotFound();
            }
            if (_context.Reservations.Where(r => r.clientPersonalCode == clientPersonalCode && r.productId == productId).Any())
            {
                _logger.LogError("Reservation already exists, edit (PUT request) the reservation if you want to change quantity");
                return BadRequest();
            }
            // calculating new product stock
            int newStock = product.stock - quantity;
            if (newStock < 0)
            {
                FailedReservation failedReservation = new FailedReservation();
                failedReservation.clientPersonalCode = clientPersonalCode;
                failedReservation.productId = productId;
                failedReservation.quantity = quantity;
                failedReservation.creationDate = DateTime.Now.ToShortDateString();
                _context.FailedReservations.Add(failedReservation);
                await _context.SaveChangesAsync();
                _logger.LogError("Not enough products in stock, reservation failed");
                return BadRequest();
            }
            product.stock = newStock;
            // updating product stock and client last activity time
            _context.Entry(product).State = EntityState.Modified;
            if (client.lastActivityDate != DateTime.Now.ToShortDateString())
            {
                client.lastActivityDate = DateTime.Now.ToShortDateString();
                _context.Entry(client).State = EntityState.Modified;
            }
            
            Reservation reservation = new Reservation();
            reservation.creationDate = DateTime.Now.ToShortDateString();
            // find reservations for given client
            List<Reservation> clientReservations = await _context.Reservations.Where(r => r.clientPersonalCode == clientPersonalCode).ToListAsync();
            // if none reservations for client found set reservation id to 0
            if (clientReservations.Count() == 0)
            {
                reservation.id = 0;
            }
            // else set reservation id to last found given client reservation id + 1
            else
            {
                reservation.id = clientReservations.Last().id + 1;
            }
            // setting reservation data
            reservation.productId = productId;
            reservation.clientPersonalCode = clientPersonalCode;
            reservation.quantity = quantity;
            // add created reservation 
            _context.Reservations.Add(reservation);
            // save changes to database
            await _context.SaveChangesAsync();
            // send emails
            string emailBody = "Reservation:\n";
            emailBody += String.Format("ID: {0}\n", reservation.id);
            emailBody += String.Format("Product ID: {0}\n", reservation.productId);
            emailBody += String.Format("Quantity: {0}\n", reservation.quantity);
            emailBody += String.Format("Client personal code: {0}\n", reservation.clientPersonalCode);
            await _mailer.SendEmailAsync("New reservation made", emailBody);
            // return created reservation with request status
            return CreatedAtAction(nameof(GetReservation), new { reservation.id, reservation.clientPersonalCode }, reservation);
        }

        // DELETE: shop/v1/reserve/5/3
        [HttpDelete("{id}/{clientPersonalCode}")]
        public async Task<ActionResult<Reservation>> DeleteReservation(int id, int clientPersonalCode)
        {
            var reservation = await _context.Reservations.FindAsync(id, clientPersonalCode);
            if (reservation == null)
            {
                return NotFound();
            }

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            return reservation;
        }

        private bool ReservationExists(int id, int clientPersonalCode)
        {
            return _context.Reservations.Any(e => e.id == id && e.clientPersonalCode == clientPersonalCode);
        }
    }
}
