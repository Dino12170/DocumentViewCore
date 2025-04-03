using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentViewCore.Models
{
    public class GoogleDriveClone
    {

    }

    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class FileModel
    {
        [Key]
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; } = DateTime.Now;
        public int UserId { get; set; }
    }

    public class FolderModel
    {
        [Key]
        public int Id { get; set; }
        public string FolderName { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}
