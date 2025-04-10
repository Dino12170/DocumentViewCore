using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using DocumentViewCore.Models;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace DocumentViewCore.Controllers
{
    public class FilesController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public FilesController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost]
        public IActionResult UploadFile(IFormFile file, string folderName, string newFolderName)
        {
            string finalFolderName = !string.IsNullOrEmpty(newFolderName) ? newFolderName : folderName;

            if (string.IsNullOrEmpty(finalFolderName))
            {
                return Content("Folder name is required.");
            }

            if (file != null && file.Length > 0)
            {
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", finalFolderName);

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var filePath = Path.Combine(uploadPath, file.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                return RedirectToAction("AddNew", "Home");
            }

            return Content("No file selected or file is empty.");
        }


        public IActionResult ViewFile(string folderName, string fileName)
        {
            var filePath = Path.Combine(_environment.WebRootPath, "uploads",folderName, fileName);
            var fileType = Path.GetExtension(filePath).ToLower();

            if (!System.IO.File.Exists(filePath))
            {
                return Content("File không tồn tại.");
            }

            if (fileType == ".pdf")
            {
                return PhysicalFile(filePath, "application/pdf");
            }
            else if (fileType == ".jpg" || fileType == ".jpeg")
            {
                return PhysicalFile(filePath, "image/jpeg");
            }
            else if (fileType == ".png")
            {
                return PhysicalFile(filePath, "image/png");
            }
            else if (fileType == ".mp4")
            {
                return PhysicalFile(filePath, "video/mp4");
            }
            else if (fileType == ".pptx" || fileType == ".ppt")
            {
                var pdfFileName = Path.ChangeExtension(fileName, ".pdf");
                var pdfFilePath = Path.Combine(_environment.WebRootPath, "uploads", pdfFileName);

                if (!System.IO.File.Exists(pdfFilePath))
                {
                    // Chuyển đổi PPTX sang PDF
                    ConvertPptxToPdf(filePath, pdfFilePath);
                }

                return PhysicalFile(pdfFilePath, "application/pdf");
            }

            return Content("File format not supported for preview.");
        }
        private void ConvertPptxToPdf(string pptxPath, string pdfPath)
        {
            using (var presentation = new Aspose.Slides.Presentation(pptxPath))
            {
                presentation.Save(pdfPath, Aspose.Slides.Export.SaveFormat.Pdf);
            }
        }
    }
}
