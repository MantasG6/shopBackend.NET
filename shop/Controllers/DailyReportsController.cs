using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using shop.Models;
using Newtonsoft.Json;

namespace shop.Controllers
{
    [Route("shop/v1/[controller]")]
    [ApiController]
    public class DailyReportsController : ControllerBase
    {
        private readonly AppDBContext _context;
        private readonly ILogger<DailyReport> _logger;

        public DailyReportsController(AppDBContext context, ILogger<DailyReport> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: shop/v1/DailyReports
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DailyReport>>> GetdailyReports()
        {
            return await _context.DailyReports.ToListAsync();
        }

        // GET: shop/v1/DailyReports/2020-12-10
        [HttpGet("{date}")]
        public async Task<ActionResult<DailyReport>> GetDailyReport(string date)
        {
            var dailyReport = await _context.DailyReports.FindAsync(date);

            if (dailyReport == null)
            {
                return NotFound();
            }

            return dailyReport;
        }
        // PUT: shop/v1/DailyReports
        [HttpPut]
        public async Task<ActionResult<DailyReport>> PutClient()
        {
            // checking if report for today is not created yet
            if (!DailyReportExists(DateTime.Now.ToShortDateString()))
            {
                _logger.LogError("Todays report doesn't exist yet (create with POST request)");
                return BadRequest();
            }
            // updating report
            DailyReport dailyReport = await _context.DailyReports.FindAsync(DateTime.Now.ToShortDateString());
            List<Reservation> successfulReservations = await _context.Reservations.Where(r => r.creationDate == dailyReport.date).ToListAsync();
            if (successfulReservations.Count() == 0)
                dailyReport.successfulReservations = "No successful reservations";
            else
                dailyReport.successfulReservations = JsonConvert.SerializeObject(successfulReservations, Formatting.Indented);
            List<FailedReservation> failedReservations = await _context.FailedReservations.Where(r => r.creationDate == dailyReport.date).ToListAsync();
            if (failedReservations.Count == 0)
                dailyReport.failedReservations = "No failed reservations";
            else
                dailyReport.failedReservations = JsonConvert.SerializeObject(failedReservations, Formatting.Indented);
            List<Client> newCustomers = await _context.Clients.Where(c => c.creationDate == dailyReport.date).ToListAsync();
            if (newCustomers.Count == 0)
                dailyReport.newCustomers = "No new customers";
            else
                dailyReport.newCustomers = JsonConvert.SerializeObject(newCustomers, Formatting.Indented);
            List<Client> returningCustomers = await _context.Clients.Where(c => c.creationDate != dailyReport.date && c.lastActivityDate == dailyReport.date).ToListAsync();
            if (returningCustomers.Count() == 0)
                dailyReport.returningCustomers = "No returning customers";
            else
                dailyReport.returningCustomers = JsonConvert.SerializeObject(returningCustomers, Formatting.Indented);
            List<Product> products = await _context.Products.ToListAsync();
            if (products.Count() == 0)
                dailyReport.stockState = "No products found";
            else
                dailyReport.stockState = JsonConvert.SerializeObject(products, Formatting.Indented);

            // setting report state to modified
            _context.Entry(dailyReport).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            // returning updated report
            return dailyReport;
        }
        // POST: shop/v1/DailyReports
        [HttpPost]
        public async Task<ActionResult<DailyReport>> PostDailyReport()
        {
            // checking if report for today is already created
            if (DailyReportExists(DateTime.Now.ToShortDateString()))
            {
                _logger.LogError("Todays report already exists (to update use PUT request)");
                return BadRequest();
            }
            // creating the report
            DailyReport dailyReport = new DailyReport();
            dailyReport.date = DateTime.Now.ToShortDateString();
            List<Reservation> successfulReservations = await _context.Reservations.Where(r => r.creationDate == dailyReport.date).ToListAsync();
            if (successfulReservations.Count() == 0)
                dailyReport.successfulReservations = "No successful reservations";
            else
                dailyReport.successfulReservations = JsonConvert.SerializeObject(successfulReservations, Formatting.Indented);
            List<FailedReservation> failedReservations = await _context.FailedReservations.Where(r => r.creationDate == dailyReport.date).ToListAsync();
            if (failedReservations.Count == 0)
                dailyReport.failedReservations = "No failed reservations";
            else
                dailyReport.failedReservations = JsonConvert.SerializeObject(failedReservations, Formatting.Indented);
            List<Client> newCustomers = await _context.Clients.Where(c => c.creationDate == dailyReport.date).ToListAsync();
            if (newCustomers.Count == 0)
                dailyReport.newCustomers = "No new customers";
            else
                dailyReport.newCustomers = JsonConvert.SerializeObject(newCustomers, Formatting.Indented);
            List<Client> returningCustomers = await _context.Clients.Where(c => c.creationDate != dailyReport.date && c.lastActivityDate == dailyReport.date).ToListAsync();
            if (returningCustomers.Count() == 0)
                dailyReport.returningCustomers = "No returning customers";
            else
                dailyReport.returningCustomers = JsonConvert.SerializeObject(returningCustomers, Formatting.Indented);
            List<Product> products = await _context.Products.ToListAsync();
            if (products.Count() == 0)
                dailyReport.stockState = "No products found";
            else
                dailyReport.stockState = JsonConvert.SerializeObject(products, Formatting.Indented);

            // adding report to dbcontext
            _context.DailyReports.Add(dailyReport);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (DailyReportExists(dailyReport.date))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            // returning created report
            return CreatedAtAction(nameof(GetDailyReport), new { dailyReport.date }, dailyReport);
        }

        // DELETE: shop/v1/DailyReports/5
        [HttpDelete("{date}")]
        public async Task<ActionResult<DailyReport>> DeleteDailyReport(string date)
        {
            var dailyReport = await _context.DailyReports.FindAsync(date);
            if (dailyReport == null)
            {
                return NotFound();
            }

            _context.DailyReports.Remove(dailyReport);
            await _context.SaveChangesAsync();

            return dailyReport;
        }

        private bool DailyReportExists(string date)
        {
            return _context.DailyReports.Any(e => e.date == date);
        }
    }
}
