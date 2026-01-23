using DeviceManagerMVC.Data;
using DeviceManagerMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

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
            const int pageSize = 15;

            var query = _context.Devices.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();

                query = query.Where(d =>
                    d.SerialNumber.ToLower().Contains(search) ||
                    d.Model.ToLower().Contains(search) ||
                    d.AssignedTo.ToLower().Contains(search) ||
                    d.DeviceType.ToLower().Contains(search) ||
                    d.Team.ToLower().Contains(search)
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

        [HttpGet]
        public async Task<IActionResult> ExportCsv()
        {
            var devices = await _context.Devices.ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("SerialNumber;DeviceType;Model;AssignedTo;Team");

            foreach (var d in devices)
            {
                csv.AppendLine($"{d.SerialNumber};{d.DeviceType};{d.Model};{d.AssignedTo};{d.Team}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "devices_export.csv");
        }


        // GET: Devices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var device = await _context.Devices.FirstOrDefaultAsync(m => m.Id == id);
            if (device == null)
                return NotFound();

            return View(device);
        }

        // GET: Devices/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Devices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SerialNumber,DeviceType,Model,AssignedTo,Team")] Device device)
        {
            if (ModelState.IsValid)
            {
                _context.Add(device);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(device);
        }

        // GET: Chart Data
        [HttpGet]
        public IActionResult GetChartData(string filter)
        {
            if (filter == "type")
            {
                var data = _context.Devices
                    .GroupBy(d => d.DeviceType)
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .ToList();

                return Json(new
                {
                    labels = data.Select(d => d.Label),
                    values = data.Select(d => d.Count)
                });
            }

            // Default: per Team
            var dataTeam = _context.Devices
                .GroupBy(d => d.Team)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .ToList();

            return Json(new
            {
                labels = dataTeam.Select(d => d.Label),
                values = dataTeam.Select(d => d.Count)
            });
        }

        // GET: Import CSV
        public IActionResult ImportCsv()
        {
            return View();
        }

        // POST: Import CSV
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

                string serial = values[0].Trim();
                string deviceType = values[1].Trim();
                string model = values[2].Trim();
                string assignedTo = values[3].Trim();
                string team = values[4].Trim();

                if (string.IsNullOrEmpty(serial))
                    errors.Add($"Riga {lineNumber}: SerialNumber mancante.");

                if (string.IsNullOrEmpty(deviceType))
                    errors.Add($"Riga {lineNumber}: DeviceType mancante.");

                if (string.IsNullOrEmpty(model))
                    errors.Add($"Riga {lineNumber}: Model mancante.");

                if (errors.Any(e => e.Contains($"Riga {lineNumber}")))
                    continue;

                var device = new Device
                {
                    SerialNumber = serial,
                    DeviceType = deviceType,
                    Model = model,
                    AssignedTo = assignedTo,
                    Team = team
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
                return NotFound();

            var device = await _context.Devices.FindAsync(id);
            if (device == null)
                return NotFound();

            return View(device);
        }

        // POST: Devices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SerialNumber,DeviceType,Model,AssignedTo,Team")] Device device)
        {
            if (id != device.Id)
                return NotFound();

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
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(device);
        }

        // GET: Devices/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var device = await _context.Devices.FirstOrDefaultAsync(m => m.Id == id);
            if (device == null)
                return NotFound();

            return View(device);
        }

        // POST: Devices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device != null)
                _context.Devices.Remove(device);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DeviceExists(int id)
        {
            return _context.Devices.Any(e => e.Id == id);
        }
    }
}

