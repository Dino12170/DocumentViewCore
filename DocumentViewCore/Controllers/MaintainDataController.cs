using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Web;
using System;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using OfficeOpenXml;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using Microsoft.Extensions.Configuration;

namespace DocumentViewCore.Controllers
{
    public class MaintainDataController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public MaintainDataController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult MaintainData()
        {
            var uploadPathFolder = Path.Combine(_environment.WebRootPath, "uploads");

            if (!Directory.Exists(uploadPathFolder))
            {
                Directory.CreateDirectory(uploadPathFolder);
            }
            ViewBag.username = HttpContext.Session.GetString("UserName");
            ViewBag.userID = HttpContext.Session.GetString("UserId");
            ViewBag.deptname = HttpContext.Session.GetString("dept_name");
            var folders = Directory.GetDirectories(uploadPathFolder).Select(Path.GetFileName).ToList();
            ViewBag.Folders = folders;

            return View();
        }

        [HttpPost]
        public IActionResult MaintainWeekExpectTime(IFormFile file, string foldeDeprName, string folderSecName)
        {
            return RedirectToAction("MaintainData", "MaintainData");
        }


        [HttpPost]
        public async Task<IActionResult> MaintainImportantLession(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a valid Excel file.");
                return RedirectToAction("Index");
            }

            // 1. Đọc connection string từ cấu hình
            string connectionString = @"Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)(HOST = 10.41.1.29)(PORT = 1521))(CONNECT_DATA =(SERVER = DEDICATED)(SERVICE_NAME = vnothdb)));User Id=GEMTEK_ATTENDANCE;Password=ATTENDANCE01;";

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;

                    using (var connection = new OracleConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        for (int row = 2; row <= rowCount; row++) // bỏ dòng tiêu đề (nếu có)
                        {
                            string lessonName = worksheet.Cells[row, 1].Text.Trim();

                            if (!string.IsNullOrEmpty(lessonName))
                            {
                                using (var command = new OracleCommand())
                                {
                                    command.Connection = connection;
                                    //    command.CommandText = @"
                                    //INSERT INTO GEMTEK_ATTENDANCE.GM1_LESSION 
                                    //(PARENT_DEPT_NO, IDLESSION, LESSIONNAME, IMPORTAIN, CUSER, CDT)
                                    //VALUES (:parentDeptNo, :idLession, :lessonName, :importain, :cuser, :cdt)";

                                    command.CommandText = @"
                                        merge into GEMTEK_ATTENDANCE.GM1_LESSION t using (select :parentDeptNo AS parent_dept, :idLession AS idlession, :lessonName AS lessionname, :importain AS important, :cuser as cuser, current_date as cdt from dual) datas
                                        on (t.PARENT_DEPT_NO = datas.parent_dept and t.LESSIONNAME = datas.lessionname)
                                        when matched then
                                        update SET t.IMPORTAIN = 'Y'
                                        when not matched then
                                        insert (PARENT_DEPT_NO, IDLESSION, LESSIONNAME, IMPORTAIN, CUSER, CDT)
                                        values (datas.parent_dept, datas.idlession, datas.lessionname, datas.important, datas.cuser, datas.cdt)";

                                    command.Parameters.Add(":parentDeptNo", OracleDbType.Varchar2).Value = HttpContext.Session.GetString("DepNo");
                                    //command.Parameters.Add(":idLession", OracleDbType.Varchar2).Value = Guid.NewGuid().ToString("N").Substring(0, 10);
                                    command.Parameters.Add(":idLession", OracleDbType.Varchar2).Value = getITLession();
                                    command.Parameters.Add(":lessonName", OracleDbType.Varchar2).Value = lessonName;
                                    command.Parameters.Add(":importain", OracleDbType.Varchar2).Value = "Y";
                                    command.Parameters.Add(":cuser", OracleDbType.Varchar2).Value = HttpContext.Session.GetString("UserId") ?? "system";

                                    await command.ExecuteNonQueryAsync();
                                }
                            }
                        }
                        await connection.CloseAsync();
                    }
                }
            }

            return RedirectToAction("MaintainData", "MaintainData");
        }

        public string getITLession()
        {
            string id = "";
            string connectionString = @"Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)(HOST = 10.41.1.29)(PORT = 1521))(CONNECT_DATA =(SERVER = DEDICATED)(SERVICE_NAME = vnothdb)));User Id=GEMTEK_ATTENDANCE;Password=ATTENDANCE01;";
            string sql = @"select GEMTEK_ATTENDANCE.GETLESSIONID from dual";
            using (var con = new OracleConnection(connectionString))
            {
                con.Open();
                using (OracleCommand cmd = new OracleCommand(sql, con))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            id = reader["GETLESSIONID"].ToString();
                        }
                    }
                }
            }
            return id;
        }


        //private readonly YourDbContext _context;
        //public MaintainDataController(YourDbContext context)
        //{
        //    _context = context;
        //}

        [HttpPost]
        public async Task<IActionResult> MaintainLession(
        string LessonName,
        string Instructor,
        DateTime StartTime,
        DateTime EndTime,
        string Participants)
        {
            string connectionString = @"Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)(HOST = 10.41.1.29)(PORT = 1521))(CONNECT_DATA =(SERVER = DEDICATED)(SERVICE_NAME = vnothdb)));User Id=GEMTEK_ATTENDANCE;Password=ATTENDANCE01;";
            
            string deptNo = HttpContext.Session.GetString("DepNo");
            string UserId = HttpContext.Session.GetString("UserId") ?? "system";
            string lessionId = "";
            using (var connection = new OracleConnection(connectionString))
            {
                await connection.OpenAsync();
                string sqlCheckLession = @"select IDLESSION count from GEMTEK_ATTENDANCE.GM1_LESSION where PARENT_DEPT_NO = :parentDeptNo and LESSIONNAME = :lessionname";
                using (var command = new OracleCommand(sqlCheckLession, connection))
                {
                    command.Parameters.Add(":parentDeptNo", OracleDbType.Varchar2).Value = deptNo;
                    command.Parameters.Add(":lessionname", OracleDbType.Varchar2).Value = LessonName;
                    //int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    using(OracleDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            lessionId = reader["IDLESSION"].ToString();
                        }
                        else
                        {
                            lessionId = getITLession();
                            string sqlInsertLession = @"
                                insert into GEMTEK_ATTENDANCE.GM1_LESSION (PARENT_DEPT_NO, IDLESSION, LESSIONNAME, IMPORTAIN, CUSER, CDT)
                                values (:parentDeptNo, :idLession, :lessonName, 'Y', :cuser, current_date)";
                            using(var insertCommand = new OracleCommand(sqlInsertLession, connection))
                            {
                                insertCommand.Parameters.Add(":parentDeptNo", OracleDbType.Varchar2).Value = deptNo;
                                insertCommand.Parameters.Add(":idLession", OracleDbType.Varchar2).Value = lessionId;
                                insertCommand.Parameters.Add(":lessonName", OracleDbType.Varchar2).Value = LessonName;
                                insertCommand.Parameters.Add(":cuser", OracleDbType.Varchar2).Value = UserId;
                                await insertCommand.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
                string sqlInserTimeLession = @"
                    insert into GEMTEK_ATTENDANCE.GM1_LESSION_DETAILS (IDLESSION, INSTRUCTOR, STARTTIME, ENDTIME, PARTICIPANTS, CUSER, CDT)
                    values (:idLession, :instructor, :startTime, :endTime, :participants, :cuser, current_date)";

            }

            return RedirectToAction("MaintainData", "MaintainData");
        }
    }
}
