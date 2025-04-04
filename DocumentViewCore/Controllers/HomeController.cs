using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Linq;

namespace DocumentViewCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public HomeController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads");

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var folders = Directory.GetDirectories(uploadPath).Select(Path.GetFileName).ToList();
            ViewBag.Folders = folders;

            return View();
        }

        public IActionResult ListFiles(string folderName)
        {
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", folderName);

            if (!Directory.Exists(uploadPath))
            {
                return NotFound("Thư mục không tồn tại.");
            }

            var files = Directory.GetFiles(uploadPath).Select(Path.GetFileName).ToList();
            ViewBag.Files = files;
            ViewBag.CurrentFolder = folderName;

            return View();
        }
    }
}
