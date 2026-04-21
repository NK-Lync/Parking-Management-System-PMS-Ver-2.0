using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingManagementSystem.Data;
using ParkingManagementSystem.Models;
using ParkingManagementSystem.ViewModels;

namespace ParkingManagementSystem.Controllers
{
    public class ParkingZoneController : Controller
    {
        private readonly ParkingDbContext _context;

        public ParkingZoneController(ParkingDbContext context)
        {
            _context = context;
        }

        private static string NormalizeZone(string? zone)
        {
            if (string.IsNullOrWhiteSpace(zone)) return string.Empty;
            return zone.Trim().ToUpperInvariant();
        }

        public async Task<IActionResult> Index()
        {
            var zones = await _context.ParkingPositions
                .AsNoTracking()
                .GroupBy(p => p.Zone)
                .Select(g => new ParkingZoneSummaryViewModel
                {
                    ZoneName = g.Key,
                    TotalSlots = g.Count(),
                    OccupiedSlots = g.Count(x => x.IsOccupied),
                    MaintenanceSlots = g.Count(x => x.Status != null && (x.Status.Contains("Bao tri") || x.Status.Contains("Maintenance")))
                })
                .OrderBy(z => z.ZoneName)
                .ToListAsync();

            return View(zones);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateZone(string zoneName, int slotCount)
        {
            var normalizedZone = NormalizeZone(zoneName);
            if (string.IsNullOrWhiteSpace(normalizedZone))
            {
                TempData["Error"] = "Ten zone khong hop le.";
                return RedirectToAction(nameof(Index));
            }

            if (slotCount <= 0)
            {
                TempData["Error"] = "So luong vi tri phai lon hon 0.";
                return RedirectToAction(nameof(Index));
            }

            var exists = await _context.ParkingPositions.AnyAsync(p => p.Zone.ToUpper() == normalizedZone);
            if (exists)
            {
                TempData["Error"] = $"Zone {normalizedZone} da ton tai.";
                return RedirectToAction(nameof(Index));
            }

            for (int i = 0; i < slotCount; i++)
            {
                _context.ParkingPositions.Add(new ParkingPosition
                {
                    Zone = normalizedZone,
                    IsOccupied = false,
                    Status = "Ready"
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Da tao zone {normalizedZone} voi {slotCount} vi tri.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCapacity(string zoneName, int newCapacity)
        {
            var normalizedZone = NormalizeZone(zoneName);
            if (string.IsNullOrWhiteSpace(normalizedZone))
            {
                TempData["Error"] = "Zone khong hop le.";
                return RedirectToAction(nameof(Index));
            }

            if (newCapacity <= 0)
            {
                TempData["Error"] = "Suc chua moi phai lon hon 0.";
                return RedirectToAction(nameof(Index));
            }

            var zonePositions = await _context.ParkingPositions
                .Where(p => p.Zone.ToUpper() == normalizedZone)
                .OrderByDescending(p => p.PositionId)
                .ToListAsync();

            if (zonePositions.Count == 0)
            {
                TempData["Error"] = $"Khong tim thay zone {normalizedZone}.";
                return RedirectToAction(nameof(Index));
            }

            var currentCapacity = zonePositions.Count;
            if (newCapacity == currentCapacity)
            {
                TempData["Success"] = $"Zone {normalizedZone} da o muc suc chua {newCapacity}.";
                return RedirectToAction(nameof(Index));
            }

            if (newCapacity > currentCapacity)
            {
                var addCount = newCapacity - currentCapacity;
                for (int i = 0; i < addCount; i++)
                {
                    _context.ParkingPositions.Add(new ParkingPosition
                    {
                        Zone = normalizedZone,
                        IsOccupied = false,
                        Status = "Ready"
                    });
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Da tang suc chua zone {normalizedZone} tu {currentCapacity} len {newCapacity}.";
                return RedirectToAction(nameof(Index));
            }

            var occupiedCount = zonePositions.Count(p => p.IsOccupied);
            if (newCapacity < occupiedCount)
            {
                TempData["Error"] = $"Khong the giam zone {normalizedZone} xuong {newCapacity} vi hien dang co {occupiedCount} xe trong bai.";
                return RedirectToAction(nameof(Index));
            }

            var removeCount = currentCapacity - newCapacity;
            var removableSlots = zonePositions.Where(p => !p.IsOccupied).Take(removeCount).ToList();
            if (removableSlots.Count < removeCount)
            {
                TempData["Error"] = $"Khong du vi tri trong de giam zone {normalizedZone}.";
                return RedirectToAction(nameof(Index));
            }

            _context.ParkingPositions.RemoveRange(removableSlots);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Da giam suc chua zone {normalizedZone} tu {currentCapacity} xuong {newCapacity}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
