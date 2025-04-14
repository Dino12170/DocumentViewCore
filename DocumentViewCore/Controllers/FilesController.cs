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
using System.Collections.Generic;

namespace DocumentViewCore.Controllers
{
    public class FilesController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public FilesController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public JsonResult GetSectionFolders(string department)
        {
            try
            {
                var deptPath = Path.Combine(_environment.WebRootPath, "uploads", department);
                if (Directory.Exists(deptPath))
                {
                    var folders = Directory.GetDirectories(deptPath)
                                           .Select(Path.GetFileName)
                                           .ToList();
                    return Json(folders);
                }
                return Json(new List<string>());
            }
            catch
            {
                return Json(new List<string>());
            }
        }


        [HttpPost]
        public IActionResult UploadFile(IFormFile file, string foldeDeprName, string folderSecName)
        {
            if (file == null || file.Length == 0)
                return Content("No file selected.");

            string targetFolder = !string.IsNullOrEmpty(folderSecName) ? folderSecName : foldeDeprName;
            if (string.IsNullOrEmpty(targetFolder))
                return Content("No target folder specified.");

            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", foldeDeprName);

            // Nếu có Section Folder thì tạo thêm cấp con
            if (!string.IsNullOrEmpty(folderSecName))
                uploadPath = Path.Combine(uploadPath, folderSecName);

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return RedirectToAction("AddNew", "Home");
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
            else if (fileType == ".doc" || fileType == ".docx")
            {
                var pdfFileName = Path.ChangeExtension(fileName, ".pdf");
                var pdfFilePath = Path.Combine(_environment.WebRootPath, "uploads", folderName, pdfFileName);

                if (!System.IO.File.Exists(pdfFilePath))
                {
                    ConvertWordToPdf(filePath, pdfFilePath);
                }

                return PhysicalFile(pdfFilePath, "application/pdf");
            }
            else if (fileType == ".xls" || fileType == ".xlsx")
            {
                var pdfFileName = Path.ChangeExtension(fileName, ".pdf");
                var pdfFilePath = Path.Combine(_environment.WebRootPath, "uploads", folderName, pdfFileName);

                if (!System.IO.File.Exists(pdfFilePath))
                {
                    ConvertExcelToPdf(filePath, pdfFilePath);
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

        private void ConvertWordToPdf(string wordPath, string pdfPath)
        {
            var doc = new Aspose.Words.Document(wordPath);
            doc.Save(pdfPath, Aspose.Words.SaveFormat.Pdf);
        }

        private void ConvertExcelToPdf(string excelPath, string pdfPath)
        {
            var workbook = new Aspose.Cells.Workbook(excelPath);
            workbook.Save(pdfPath, Aspose.Cells.SaveFormat.Pdf);
        }

    }
}
