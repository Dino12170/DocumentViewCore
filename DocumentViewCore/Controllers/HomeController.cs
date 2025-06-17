using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Data.SqlClient;
using System.Web;
using System;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

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
            if (HttpContext.Session.GetString("UserId") == null)
            {
                //ViewBag.error = TempData["Please login first!!!"];
                TempData["Error"] = "Plese Login!!!";
                return RedirectToAction("Login", "Home");
            }
            else
            {
                var uploadPathFolder = Path.Combine(_environment.WebRootPath, "uploads");

                if (!Directory.Exists(uploadPathFolder))
                {
                    Directory.CreateDirectory(uploadPathFolder);
                }
                ViewBag.username = HttpContext.Session.GetString("UserName");
                ViewBag.userID = HttpContext.Session.GetString("UserId");
                var folders = Directory.GetDirectories(uploadPathFolder).Select(Path.GetFileName).ToList();
                ViewBag.Folders = folders;
                // Tạo dictionary chứa các thư mục con tương ứng từng top folder
                var subfoldersMap = new Dictionary<string, List<string>>();
                foreach (var folder in folders)
                {
                    var subPath = Path.Combine(uploadPathFolder, folder);
                    var children = Directory.GetDirectories(subPath)
                        .Select(Path.GetFileName)
                        .ToList();
                    subfoldersMap[folder] = children;
                }
                ViewBag.SubfoldersMap = subfoldersMap;

                return View();
            }
        }

        public IActionResult AddNew()
        {
            var uploadPathFolder = Path.Combine(_environment.WebRootPath, "uploads");

            if (!Directory.Exists(uploadPathFolder))
            {
                Directory.CreateDirectory(uploadPathFolder);
            }
            ViewBag.username = HttpContext.Session.GetString("UserName");
            ViewBag.userID = HttpContext.Session.GetString("UserId");
            var folders = Directory.GetDirectories(uploadPathFolder).Select(Path.GetFileName).ToList();
            ViewBag.Folders = folders;

            return View();

            //if (HttpContext.Session.GetString("UserId") == null)
            //{
            //    //ViewBag.error = TempData["Please login first!!!"];
            //    TempData["Error"] = "Plese Login!!!";
            //    return RedirectToAction("Login", "Home");
            //}
            //else
            //{
            //    var uploadPathFolder = Path.Combine(_environment.WebRootPath, "uploads");

            //    if (!Directory.Exists(uploadPathFolder))
            //    {
            //        Directory.CreateDirectory(uploadPathFolder);
            //    }
            //    ViewBag.username = HttpContext.Session.GetString("UserName");
            //    ViewBag.userID = HttpContext.Session.GetString("UserId");
            //    var folders = Directory.GetDirectories(uploadPathFolder).Select(Path.GetFileName).ToList();
            //    ViewBag.Folders = folders;

            //    return View();
            //}
        }

        [HttpPost]
        public JsonResult CreateFolder(string departmentFolder, string sectionFolder)
        {
            try
            {
                var rootPath = Path.Combine(_environment.WebRootPath, "uploads");
                var deptPath = Path.Combine(rootPath, departmentFolder);
                var sectionPath = Path.Combine(deptPath, sectionFolder);

                if (!Directory.Exists(deptPath))
                {
                    Directory.CreateDirectory(deptPath);
                }

                if (!Directory.Exists(sectionPath))
                {
                    Directory.CreateDirectory(sectionPath);
                }

                return Json(new { success = true, folder = departmentFolder });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        public IActionResult RefreshSidebar()
        {
            var uploadPathFolder = Path.Combine(_environment.WebRootPath, "uploads");

            if (!Directory.Exists(uploadPathFolder))
            {
                Directory.CreateDirectory(uploadPathFolder);
            }
            var folders = Directory.GetDirectories(uploadPathFolder).Select(Path.GetFileName).ToList();
            ViewBag.Folders = folders;

            return PartialView("_FolderSidebar");
        }


        #region Login

        [HttpGet]
        public ActionResult Login()
        {
            if (HttpContext.Session.GetString("UserId") != null)
            {
                HttpContext.Session.Clear();
            }
            return View();
        }

        [HttpPost]
        public ActionResult Login(string userid, string password)
        {
            string pwd = EC(password);
            string dept = "";
            using (SqlConnection con1 = new SqlConnection(@"Data Source=10.5.1.126;Initial Catalog=ACCOUNT; User ID=sa; Password=gmportalsa; TrustServerCertificate=True"))
            {
                con1.Open();
                string SelSQL = "SELECT * FROM UserAccount WHERE username = @userid AND password = @password AND FLAG = 'Y'";

                using (SqlCommand MScommand = new SqlCommand(SelSQL, con1))
                {
                    // Sử dụng tham số để tránh SQL Injection
                    MScommand.Parameters.AddWithValue("@userid", userid);
                    MScommand.Parameters.AddWithValue("@password", pwd);

                    using (SqlDataReader MSReader = MScommand.ExecuteReader())
                    {
                        if (MSReader.Read())
                        {
                            string connectionString = @"Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)(HOST = 10.5.1.53)(PORT = 1621))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME =ERPDW)));Password= vnhr$2819Hp;User ID=hcp;";
                            string sqlinfo = string.Format(@"select emp_no,emp_name,dept_name, dept_no from VN_EAS_EMP_V where emp_no='{0}'", userid);
                            using (OracleConnection conn = new OracleConnection(connectionString))
                            {
                                conn.Open();
                                using (OracleCommand cmd = new OracleCommand(sqlinfo, conn))
                                {
                                    OracleDataReader reader = cmd.ExecuteReader();
                                    if (reader.HasRows)
                                    {
                                        while (reader.Read())
                                        {
                                            HttpContext.Session.SetString("UserId", userid.ToString());
                                            HttpContext.Session.SetString("UserName", reader["emp_name"].ToString());
                                            HttpContext.Session.SetString("DepName", reader["dept_name"].ToString());
                                            HttpContext.Session.SetString("DepNo", reader["dept_no"].ToString());
                                            dept = reader["dept_name"].ToString();
                                        }
                                    }
                                }
                            }
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            ViewBag.error = "Password is wrong!!!";
                            return View();
                        }
                    }
                }
            }
        }
        public ActionResult Logout()
        {
            //var username = Session["UserId"] as string;
            var username = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(username))
            {
                //_dbHelper.LogLogout(username);
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home");
        }

        public string EC(string str)
        {
            string Result = "";
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            byte[] dBytes = encoding.GetBytes(str);
            for (int i = 0; i < dBytes.Length; i++)
            {
                int MyInterger = Convert.ToInt16(dBytes[i]);
                MyInterger = MyInterger + 8 + i;
                string strSEQ = string.Format("{0:X3}", MyInterger);
                Result = Result + strSEQ;
            }
            return Result;
        }
        #endregion

        //public IActionResult ListFiles(string folderName)
        //{
        //    if (HttpContext.Session.GetString("UserId") == null)
        //    {
        //        //ViewBag.error = TempData["Please login first!!!"];
        //        TempData["Error"] = "Plese Login!!!";
        //        return RedirectToAction("Login", "Home");
        //    }
        //    else
        //    {
        //        var uploadPathFolder = Path.Combine(_environment.WebRootPath, "uploads");
        //        var combinepath = Path.Combine(uploadPathFolder, folderName);
        //        var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", folderName);

        //        if (!Directory.Exists(uploadPath))
        //        {
        //            return NotFound("Thư mục không tồn tại.");
        //        }

        //        var files = Directory.GetFiles(uploadPath).Select(Path.GetFileName).ToList();
        //        ViewBag.Files = files;
        //        ViewBag.CurrentFolder = folderName;

        //        var folders = Directory.GetDirectories(uploadPathFolder).Select(Path.GetFileName).ToList();
        //        ViewBag.Folders = folders;
        //        return View();
        //    }
        //}

        public IActionResult ListFiles(string folderName)
        {
            // Kiểm tra đăng nhập
            if (HttpContext.Session.GetString("UserId") == null)
            {
                TempData["Error"] = "Please Login!!!";
                return RedirectToAction("Login", "Home");
            }

            // Đường dẫn gốc: wwwroot/uploads
            var uploadRootPath = Path.Combine(_environment.WebRootPath, "uploads");

            // Đường dẫn thư mục cần xem
            var targetPath = string.IsNullOrEmpty(folderName)
                ? uploadRootPath
                : Path.Combine(uploadRootPath, folderName);

            if (!Directory.Exists(targetPath))
            {
                return NotFound("Thư mục không tồn tại.");
            }

            // Lấy danh sách file trong thư mục hiện tại
            var files = Directory.GetFiles(targetPath).Select(Path.GetFileName).ToList();

            // Lấy danh sách thư mục con trong thư mục hiện tại
            var subfolders = Directory.GetDirectories(targetPath)
                .Select(dir => Path.GetFileName(dir))
                .ToList();

            // Gửi dữ liệu về View
            ViewBag.Files = files;
            ViewBag.Subfolders = subfolders;
            ViewBag.CurrentFolder = folderName;

            // Gửi danh sách thư mục cấp 1 (trong wwwroot/uploads) nếu cần hiển thị sidebar
            var topFolders = Directory.GetDirectories(uploadRootPath)
                .Select(Path.GetFileName)
                .ToList();
            ViewBag.Folders = topFolders;
            // Tạo dictionary chứa các thư mục con tương ứng từng top folder
            var subfoldersMap = new Dictionary<string, List<string>>();
            foreach (var folder in topFolders)
            {
                var subPath = Path.Combine(uploadRootPath, folder);
                var children = Directory.GetDirectories(subPath)
                    .Select(Path.GetFileName)
                    .ToList();
                subfoldersMap[folder] = children;
            }
            ViewBag.SubfoldersMap = subfoldersMap;
            return View();
        }

        [HttpGet]
        public IActionResult Search(string query)
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                //ViewBag.error = TempData["Please login first!!!"];
                TempData["Error"] = "Plese Login!!!";
                return RedirectToAction("Login", "Home");
            }
            else
            {
                var uploadRoot = Path.Combine(_environment.WebRootPath, "uploads");
                var matchedFiles = new List<(string Folder, string File)>();

                if (Directory.Exists(uploadRoot))
                {
                    var allFiles = Directory
                        .GetFiles(uploadRoot, "*", SearchOption.AllDirectories)
                        .Where(f => Path.GetDirectoryName(f) != uploadRoot) // chỉ giữ file KHÔNG ở thư mục gốc
                        .ToList();

                    foreach (var filePath in allFiles)
                    {
                        var fileName = Path.GetFileName(filePath);
                        if (fileName.Contains(query, StringComparison.OrdinalIgnoreCase))
                        {
                            var folder = Path.GetRelativePath(uploadRoot, Path.GetDirectoryName(filePath)!);
                            matchedFiles.Add((folder, fileName));
                        }
                    }
                }
                var folders = Directory.GetDirectories(uploadRoot).Select(Path.GetFileName).ToList();
                ViewBag.Folders = folders;
                ViewBag.Query = query;
                return View("SearchResults", matchedFiles);
            }
        }

        //[HttpGet]
        //public IActionResult Search(string query)
        //{
        //    if (string.IsNullOrWhiteSpace(query))
        //    {
        //        return View("SearchResults", new List<(string Folder, string File)>());
        //    }

        //    var uploadRoot = Path.Combine(_environment.WebRootPath, "uploads");
        //    var matchedFiles = new List<(string Folder, string File)>();

        //    var allFiles = Directory.GetFiles(uploadRoot, "*", SearchOption.AllDirectories);

        //    foreach (var filePath in allFiles)
        //    {
        //        var fileName = Path.GetFileName(filePath);
        //        var relativeFolder = Path.GetRelativePath(uploadRoot, Path.GetDirectoryName(filePath)!);

        //        // 👉 Chỉ thêm nếu file nằm trong thư mục con (tức là relativeFolder không rỗng)
        //        if (!string.IsNullOrEmpty(relativeFolder) &&
        //            fileName.Contains(query, StringComparison.OrdinalIgnoreCase))
        //        {
        //            matchedFiles.Add((relativeFolder, fileName));
        //        }
        //    }

        //    ViewBag.Query = query;
        //    return View("SearchResults", matchedFiles);
        //}

    }
}
