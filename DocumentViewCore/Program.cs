using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentViewCore
{
    public class Program
    {
        //args là các đối số dòng lệnh, có thể truyền vào khi chạy ứng dụng
        //vd: dotnet run --environment "Development" -> args sẽ chứa "Development", --enviroment là tên của đối số
        //vd: dotnet run --urls "http://localhost:5000" -> args sẽ chứa "http://localhost:5000"
        //sử dụng args để cấu hình ứng dụng, ví dụ như cấu hình môi trường, địa chỉ URL, cổng, ... (để cho phép người dùng tùy chỉnh hành vi khởi động của ứng dụng thông qua dòng lệnh)
        //nếu không truyền giá trị gì vào thì args sẽ là một mảng rỗng string[0]="";
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
