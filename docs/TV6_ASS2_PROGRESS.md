# TV6 — Assignment 2: ba đợt triển khai

Phạm vi: mục A2 / TV6, hợp đồng A3.3–A3.4 và checklist A, E, S trong `AIVES_PhanCong_Ass2_Ass3.md`.

## Đợt 1 — Khung ứng dụng và danh sách phiên thi

- Kế thừa cấu hình DI đã có: hasher Singleton, bảy service Scoped, web chỉ tham chiếu Business.
- Giữ các policy, cookie 8 giờ, middleware và SignalR đã có; hủy cookie khi ID không hợp lệ hoặc vai trò đã thay đổi.
- Bổ sung `EnumExtensions.GetDisplayName`; helper vai trò hiện có dùng tên trong `DisplayAttribute`.
- Thêm mục Phiên thi trên layout chung cho Admin/Giảng viên.
- Thêm `/Exams`: gọi `IExamService.GetListAsync(CurrentUserId)`, hiển thị môn, giờ bắt đầu, số thí sinh, trạng thái và trường hợp rỗng.
- Đặt toàn bộ danh sách trong `exam-list` / `data-live="exams"` để dùng `live.js` hiện có.
- Nút Tạo được bổ sung ở đợt 2; liên kết Chi tiết được bổ sung ở đợt 3.

Commit đề xuất: `feat(TV6): complete web foundation and exam list`

## Đợt 2 — Tạo phiên thi (đã triển khai)

- Thêm `Pages/Exams/Create` và nút Tạo trên danh sách.
- Chỉ lấy môn được phép từ Service; mặc định sáng mai 08:00, các số 10/3/4/2/60.
- Kiểm tra dữ liệu nhập, giữ dữ liệu khi lỗi; Service quyết định luật nghiệp vụ và quyền.
- Sau khi tạo thành công gửi topic `exams`. Trước khi có trang Chi tiết, chuyển về danh sách.

Commit đề xuất: `feat(TV6): add exam creation and live notification`

File chính: `Pages/Exams/Create.cshtml`, `Create.cshtml.cs`; nút Tạo nằm trong `Pages/Exams/Index.cshtml`.

Form dùng DataAnnotations và HTML required/min/max để báo lỗi nhập liệu; PageModel gọi `IExamService` để kiểm tra quyền, thời gian quá khứ, MSSV thiếu/trùng và số câu hỏi. Khi lỗi validation, giữ dữ liệu nhập và nạp lại danh sách môn. Lỗi quyền/không tồn tại ánh xạ về 403/404. Chỉ gửi `changed("exams", ...)` sau khi Service tạo thành công; thông báo không chứa thông tin sinh viên.

Kiểm tra riêng cho TV6: `dotnet test tools/TV6.Verify/TV6.Verify.csproj`. Dùng PageModel và ExamService thật với repository trong bộ nhớ, kiểm tra dữ liệu mặc định, danh sách môn được phép, tạo lịch ba sinh viên, lỗi form/nghiệp vụ, 403/404 và thứ tự lưu trước khi gửi SignalR. Bộ này không thay thế kiểm thử trình duyệt, cookie, antiforgery hoặc kết nối SignalR thật.

Kết quả đợt 2: build project kiểm tra và các project ứng dụng được tham chiếu thành công (0 warning, 0 error); đạt **11/11 test**, không bỏ qua test nào. Chưa kiểm tra SQL Server và hai trình duyệt.

## Đợt 3 — Chi tiết và thao tác phiên thi

- Thêm `/Exams/Details/{id}`, liên kết từ danh sách và chuyển về chi tiết sau khi tạo.
- Bốn ô thông tin, lịch thi, câu hỏi và trạng thái từng thí sinh; không có nút bắt đầu thi.
- POST Đóng/Mở gửi `exams` và `exam-{id}`; POST Chọn lại gửi `exam-{id}` sau khi Service thành công.
- Vùng `exam-detail` / `data-live="exam-{id}"`; chỉ hiện Chọn lại khi mọi người chưa thi.
- Kiểm tra quyền, lỗi 403/404, validation và cập nhật qua hai trình duyệt theo checklist E/S.

Commit đề xuất: `feat(TV6): add exam details and session actions`

## Phụ thuộc và kiểm tra

Kết quả đợt 1 (13/10/2026): `dotnet build AIVES.Ass2.sln` thành công, 0 warning / 0 error; bộ `AIVES.Tests` đạt 127/127 test. Test runner bị treo khi khởi động trong sandbox; chạy lại ngoài sandbox thành công. Đây là test Business hiện có, chưa kiểm chứng giao diện và cookie bằng trình duyệt.

`ExamRepository` và `QuestionRepository` hiện còn các phương thức ném `NotImplementedException` (phần TV2). Vì vậy build thành công chưa đảm bảo trang Phiên thi chạy được với SQL Server. TV6 gọi đúng interface Business; không tự thay phần repository của TV2.

Sau khi TV2 hoàn thiện: đăng nhập Admin và Giảng viên, kiểm tra `/Exams` chỉ trả dữ liệu được phép; kiểm tra trường hợp danh sách rỗng và có dữ liệu. Các kiểm tra runtime database/SignalR phải được thực hiện trước khi đánh dấu checklist hoàn tất.

## Chuẩn bị bảo vệ 6.1

- Thứ tự: Routing → Authentication → Authorization → các endpoint Razor Pages/Hub. Authentication đọc cookie và dựng User trước khi Authorization đánh giá policy.
- DbContext dùng Scoped để mỗi request có phạm vi làm việc riêng; bộ băm không giữ trạng thái theo request nên dùng Singleton.
- PageModel gửi SignalR sau khi Service thành công để chỉ thông báo dữ liệu đã được cập nhật, đồng thời giữ Business độc lập với giao diện.
- Mini-task đảo middleware: chưa thực hiện. Chỉ thử trên môi trường phát triển, ghi lại phản hồi thực tế rồi phục hồi thứ tự đúng.
