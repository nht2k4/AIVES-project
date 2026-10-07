// TV6 - Ass3 - việc 6.1: viết Program.cs thật (xem AIVES_PhanCong_Ass2_Ass3.md). File này chỉ để solution biên dịch được.
var app = WebApplication.CreateBuilder(args).Build();
app.MapGet("/", () => "AIVES.Ass3.MVC: chưa có gì. TV6 viết Program.cs.");
app.Run();
