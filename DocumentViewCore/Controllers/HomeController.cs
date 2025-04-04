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
            var uploadPathFolder = Path.Combine(_environment.WebRootPath, "uploads");

            if (!Directory.Exists(uploadPathFolder))
            {
                Directory.CreateDirectory(uploadPathFolder);
            }

            var folders = Directory.GetDirectories(uploadPathFolder).Select(Path.GetFileName).ToList();
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

            var uploadPathFolder = Path.Combine(_environment.WebRootPath, "uploads");
            var folders = Directory.GetDirectories(uploadPathFolder).Select(Path.GetFileName).ToList();
            ViewBag.Folders = folders;
            return View();
        }
    }
}
