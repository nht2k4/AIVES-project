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

## Đợt 3 — Chi tiết và thao tác phiên thi (đã triển khai)

- Thêm `/Exams/Details/{id}`, liên kết từ danh sách và chuyển về chi tiết sau khi tạo.
- Bốn ô thông tin, lịch thi, câu hỏi và trạng thái từng thí sinh; không có nút bắt đầu thi.
- POST Đóng/Mở gửi `exams` và `exam-{id}`; POST Chọn lại gửi `exam-{id}` sau khi Service thành công.
- Vùng `exam-detail` / `data-live="exam-{id}"`; chỉ hiện Chọn lại khi mọi người chưa thi.
- Kiểm tra quyền, lỗi 403/404, validation và cập nhật qua hai trình duyệt theo checklist E/S.

Commit đề xuất: `feat(TV6): add exam details and session actions`

Trang `Details` dùng route `/Exams/Details/{id:int}`. Toàn bộ thông tin, trạng thái và form POST nằm trong vùng `exam-detail`, nên khi `live.js` cập nhật vùng này, nút thao tác và token chống giả mạo cũng được tải lại. Các form dùng Razor Form Tag Helper, không cần gắn lại JavaScript khi thay nội dung. Ass2 không có nút bắt đầu thi.

Đóng/Mở truyền trạng thái đích tường minh; thiếu hoặc sai giá trị không được âm thầm đóng phiên. Khi Service báo validation, tải lại thông tin và hiển thị lỗi; 403/404 được giữ nguyên. Chọn lại luôn gọi Service để kiểm tra trạng thái mới nhất, kể cả khi trang cũ vẫn hiện nút. Thành công mới gửi topic theo hợp đồng, sau đó redirect về chi tiết.

Trang Tạo ở đợt 2 nay chuyển thẳng tới phiên vừa tạo. Danh sách có liên kết từ tên phiên tới Chi tiết.

Kiểm tra tự động bổ sung trong `tools/TV6.Verify/DetailsChecks.cs`: tải chi tiết, quyền/ID không tồn tại cho cả GET và hai POST, thiếu trạng thái, Đóng/Mở gửi đúng hai topic, chọn lại gửi riêng topic chi tiết, chặn chọn lại sau khi có người thi và khi ngân hàng không đủ câu đang dùng. Dùng Service thật và repository trong bộ nhớ; không xác nhận kết nối SQL/SignalR qua trình duyệt.

Kết quả đợt 3: build `tools/TV6.Verify/TV6.Verify.csproj` cùng các project ứng dụng thành công, 0 warning / 0 error; **25/25 test TV6 đạt** (11 test phần tạo đã cập nhật chuyển hướng + 14 test chi tiết), không bỏ qua test nào. `git diff --check` sạch. Checklist trình duyệt bên dưới chưa chạy vì repository SQL của TV2 còn stub.

### Kiểm tra thủ công khi TV2 hoàn thiện repository

1. Admin hoặc giảng viên được phân công tạo phiên gồm ba sinh viên, mở Chi tiết và kiểm tra bốn ô thông tin, lịch thi, câu hỏi, trạng thái.
2. Mở Danh sách và Chi tiết cùng phiên ở hai trình duyệt. Đóng/Mở từ trình duyệt thứ hai: danh sách và chi tiết ở trình duyệt thứ nhất phải cập nhật đúng huy hiệu.
3. Chọn lại câu hỏi: Chi tiết tự cập nhật; tiếp tục bấm Đóng/Mở trên phần HTML vừa cập nhật để kiểm token chống giả mạo.
4. Giảng viên môn khác truy cập GET hoặc gửi POST tới phiên phải bị chặn. ID không tồn tại phải trả 404.
5. Khi một thí sinh đang thi/đã thi, nút Chọn lại biến mất và gửi POST thủ công cũng bị Service từ chối.
6. Ngắt mạng rồi nối lại và thực hiện một thao tác mới từ trình duyệt còn lại, xác nhận sự kiện vẫn tới.

## Phụ thuộc và kiểm tra

Kết quả đợt 1 (13/10/2026): `dotnet build AIVES.Ass2.sln` thành công, 0 warning / 0 error; bộ `AIVES.Tests` đạt 127/127 test. Test runner bị treo khi khởi động trong sandbox; chạy lại ngoài sandbox thành công. Đây là test Business hiện có, chưa kiểm chứng giao diện và cookie bằng trình duyệt.

`ExamRepository` và `QuestionRepository` hiện còn các phương thức ném `NotImplementedException` (phần TV2). Vì vậy build thành công chưa đảm bảo trang Phiên thi chạy được với SQL Server. TV6 gọi đúng interface Business; không tự thay phần repository của TV2.

Sau khi TV2 hoàn thiện: đăng nhập Admin và Giảng viên, kiểm tra `/Exams` chỉ trả dữ liệu được phép; kiểm tra trường hợp danh sách rỗng và có dữ liệu. Các kiểm tra runtime database/SignalR phải được thực hiện trước khi đánh dấu checklist hoàn tất.

## Chuẩn bị bảo vệ 6.1

- Thứ tự: Routing → Authentication → Authorization → các endpoint Razor Pages/Hub. Authentication đọc cookie và dựng User trước khi Authorization đánh giá policy.
- DbContext dùng Scoped để mỗi request có phạm vi làm việc riêng; bộ băm không giữ trạng thái theo request nên dùng Singleton.
- PageModel gửi SignalR sau khi Service thành công để chỉ thông báo dữ liệu đã được cập nhật, đồng thời giữ Business độc lập với giao diện.
- Mini-task đảo middleware: chưa thực hiện. Chỉ thử trên môi trường phát triển, ghi lại phản hồi thực tế rồi phục hồi thứ tự đúng.
