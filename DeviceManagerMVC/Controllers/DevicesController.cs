using DeviceManagerMVC.Data;
using DeviceManagerMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceManagerMVC.Controllers
{
    public class DevicesController : Controller
    {
        private readonly AppDbContext _context;

        public DevicesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Devices
        public async Task<IActionResult> Index()
        {
            return View(await _context.Devices.ToListAsync());
        }

        public async Task<IActionResult> SearchAjax(string search, int page = 1)
        {
            Console.WriteLine("SEARCH = [" + search + "]");

            const int pageSize = 15;

            var query = _context.Devices.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();

                query = query.Where(d =>
                    d.SerialNumber.ToLower().Contains(search) ||
                    d.Model.ToLower().Contains(search) ||
                    d.AssignedTo.ToLower().Contains(search) ||
                    d.IsActive.ToString().ToLower().Contains(search)
                );
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await query
                .OrderBy(d => d.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<Device>
            {
                Items = items,
                Page = page,
                TotalPages = totalPages
            };

            return PartialView("_DeviceTable", result);
        }


        // GET: Devices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var device = await _context.Devices
                .FirstOrDefaultAsync(m => m.Id == id);
            if (device == null)
            {
                return NotFound();
            }

            return View(device);
        }

        // GET: Devices/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ImportCsv()
        {
            return View();
        }


        // POST: Devices/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SerialNumber,Model,AssignedTo,PurchaseDate,IsActive")] Device device)
        {
            if (ModelState.IsValid)
            {
                _context.Add(device);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(device);
        }

        [HttpPost]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ImportErrors"] = "Nessun file selezionato.";
                return RedirectToAction("Index");
            }

            var errors = new List<string>();
            int lineNumber = 0;
            int imported = 0;

            using var reader = new StreamReader(file.OpenReadStream());

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {

                lineNumber++;

                // Salta l'header
                if (lineNumber == 1)
                    continue;

                if (string.IsNullOrWhiteSpace(line))
                {
                    errors.Add($"Riga {lineNumber}: vuota.");
                    continue;
                }

                var values = line.Split(';');

                if (values.Length < 5)
                {
                    errors.Add($"Riga {lineNumber}: numero di colonne errato ({values.Length}/5).");
                    continue;
                }

                // Estrazione campi
                string serial = values[0].Trim();
                string model = values[1].Trim();
                string assignedTo = values[2].Trim();
                string purchaseDateString = values[3].Trim();
                string isActiveString = values[4].Trim();

                // Validazione
                if (string.IsNullOrEmpty(serial))
                    errors.Add($"Riga {lineNumber}: SerialNumber mancante.");

                if (string.IsNullOrEmpty(model))
                    errors.Add($"Riga {lineNumber}: Model mancante.");

                if (!DateTime.TryParse(purchaseDateString, out DateTime purchaseDate))
                    errors.Add($"Riga {lineNumber}: PurchaseDate non valida ({purchaseDateString}).");

                if (!bool.TryParse(isActiveString, out bool isActive))
                    errors.Add($"Riga {lineNumber}: IsActive deve essere true/false ({isActiveString}).");

                // Se ci sono errori per questa riga → salta inserimento
                if (errors.Any(e => e.Contains($"Riga {lineNumber}")))
                    continue;

                var device = new Device
                {
                    SerialNumber = serial,
                    Model = model,
                    AssignedTo = assignedTo,
                    PurchaseDate = purchaseDate,
                    IsActive = isActive
                };

                _context.Devices.Add(device);
                imported++;
            }

            await _context.SaveChangesAsync();

            if (errors.Count > 0)
                TempData["ImportErrors"] = string.Join("<br/>", errors);

            TempData["ImportSuccess"] = $"Importazione completata. Inseriti {imported} dispositivi.";

            return RedirectToAction("Index");
        }


        // GET: Devices/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                return NotFound();
            }
            return View(device);
        }

        public async Task<IActionResult> ExportCsv()
        {
            var devices = await _context.Devices.ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id;SerialNumber;Model;AssignedTo;PurchaseDate;IsActive");

            foreach (var d in devices)
            {
                csv.AppendLine($"{d.Id};{d.SerialNumber};{d.Model};{d.AssignedTo};{d.PurchaseDate:yyyy-MM-dd};{d.IsActive}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "devices_export.csv");
        }



        // POST: Devices/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SerialNumber,Model,AssignedTo,PurchaseDate,IsActive")] Device device)
        {
            if (id != device.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(device);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DeviceExists(device.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(device);
        }

        // GET: Devices/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var device = await _context.Devices
                .FirstOrDefaultAsync(m => m.Id == id);
            if (device == null)
            {
                return NotFound();
            }

            return View(device);
        }

        // POST: Devices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device != null)
            {
                _context.Devices.Remove(device);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DeviceExists(int id)
        {
            return _context.Devices.Any(e => e.Id == id);
        }
    }
}
