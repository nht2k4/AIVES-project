# AIVES (Nhóm 4): Báo cáo chi tiết công việc của TV4 (Assignment 3)

Tài liệu này tổng hợp chi tiết toàn bộ các chức năng, đường dẫn file cụ thể và kết quả nghiệm thu mà **Thành viên 4 (TV4)** phụ trách trong dự án `AIVES.Ass3.MVC`.

---

## 1. Bảng phân công & Danh sách file thực hiện

| Mã việc | Tên chức năng | File thực hiện (Code / Giao diện / Model) | Mô tả chi tiết chức năng |
|---|---|---|---|
| **4.1** | **Xác thực & Đăng nhập / Đăng ký** | - `AIVES.Ass3.MVC/Controllers/AuthController.cs`<br>- `AIVES.Ass3.MVC/Controllers/HomeController.cs`<br>- `AIVES.Ass3.MVC/ViewModels/LoginViewModel.cs`<br>- `AIVES.Ass3.MVC/Views/Auth/Login.cshtml`<br>- `AIVES.Ass3.MVC/Views/Auth/Register.cshtml`<br>- `AIVES.Ass3.MVC/Views/Auth/AccessDenied.cshtml` | - Xác thực người dùng bằng Cookie Authentication.<br>- Xử lý đăng nhập, ghi nhớ, chống Open Redirect URL.<br>- Sinh viên tự đăng ký với vai trò mặc định `Pending`. Sau khi đăng ký gửi SignalR `accounts` báo cho Admin.<br>- Điều hướng trang chủ: Sinh viên vào `/MyExams`, Giảng viên/Admin vào `/Exams`.<br>- Cung cấp mẫu tài khoản sinh viên `sv.a@fu.edu.vn / Student@123`. |
| **4.2** | **Quản lý Tài khoản & Phân quyền** | - `AIVES.Ass3.MVC/Controllers/AccountsController.cs`<br>- `AIVES.Ass3.MVC/ViewModels/AccountViewModels.cs`<br>- `AIVES.Ass3.MVC/Views/Accounts/Index.cshtml`<br>- `AIVES.Ass3.MVC/Views/Accounts/Create.cshtml`<br>- `AIVES.Ass3.MVC/Views/Accounts/Edit.cshtml` | - Dành riêng cho `Admin`: Quản lý toàn bộ danh sách tài khoản.<br>- Danh sách đặt trong vùng `<div id="account-list" data-live="accounts">` tự làm mới thời gian thực khi có người đăng ký mới.<br>- Hiển thị cột vai trò kèm **Mã sinh viên (MSSV)**.<br>- Tạo mới có ô nhập MSSV khi chọn vai trò Sinh viên (không cho tạo vai `Pending`).<br>- Chỉnh sửa tài khoản: Nơi Admin **cấp vai trò** chính thức cho tài khoản `Pending`, đổi vai trò, khóa/mở khóa an toàn, đặt lại mật khẩu. |
| **4.3** | **Quản lý Phiên thi & Ngân hàng câu hỏi** | - `AIVES.Ass3.MVC/Controllers/ExamsController.cs`<br>- `AIVES.Ass3.MVC/ViewModels/ExamViewModels.cs`<br>- `AIVES.Ass3.MVC/Views/Exams/Index.cshtml`<br>- `AIVES.Ass3.MVC/Views/Exams/Create.cshtml`<br>- `AIVES.Ass3.MVC/Views/Exams/Details.cshtml`<br>- `AIVES.Ass3.MVC/Views/Exams/Questions.cshtml` | - Dành cho `Admin` và `Lecturer`.<br>- **Danh sách phiên thi:** bọc trong vùng `<div id="exam-list" data-live="exams">` tự cập nhật khi tạo/đóng/mở phiên.<br>- **Tạo phiên thi:** Cấu hình thời gian, khung giờ, số câu hỏi chính/xoáy, thời gian trả lời. Nhập danh sách sinh viên (`MSSV; Họ tên`). Bắt lỗi nếu có MSSV chưa được tạo tài khoản sinh viên.<br>- **Chi tiết phiên thi:** Vùng `<div id="exam-detail" data-live="exam-{id}">`. Bảng thí sinh theo dõi realtime tiến trình (`Scheduled` -> `InProgress` -> `Completed`). Hiện nút **Biên bản** (`/Interview/Transcript?participantId=...`). **Tuyệt đối không có nút thi hộ**.<br>- **Ngân hàng câu hỏi:** Thêm câu hỏi kèm ý chính (`ExpectedPoints`), bật/tắt câu hỏi trực tiếp. |
| **4.4** | **Nền tảng Web API & REST Endpoints** | - `AIVES.Ass3.MVC/Controllers/Api/ApiControllerBase.cs`<br>- `AIVES.Ass3.MVC/Controllers/Api/AuthApiController.cs`<br>- `AIVES.Ass3.MVC/Controllers/Api/AccountsApiController.cs` | - Xây dựng `ApiControllerBase` làm khung chuẩn RESTful cho toàn dự án kế thừa (chuẩn hóa HTTP status code 200/201/204/400/403/404 và JSON error format `{ "error": "..." }`).<br>- Triển khai đầy đủ REST API xác thực (`/api/auth`) và quản lý tài khoản (`/api/accounts`). |

---

## 2. Kết quả kiểm thử & Nghiệm thu (Checklist B4.3 & ass3.http)

- [x] **Checklist A (Đăng nhập):** Đăng nhập đúng/sai thông báo chuẩn; tài khoản `Pending` báo chờ duyệt; đăng nhập `sv.a@fu.edu.vn` chuyển hướng sang Bài thi của tôi.
- [x] **Checklist R3 (Tự đăng ký):** Đăng ký tài khoản thành công ở form hoặc API; SignalR gửi thông báo và màn hình Admin tự hiện dòng mới không cần F5.
- [x] **Checklist B3 (Tài khoản):** Tạo sinh viên bắt buộc có MSSV duy nhất; sửa/cấp vai trò hoạt động chuẩn; khóa/đặt lại mật khẩu an toàn.
- [x] **Checklist E3 (Phiên thi & Câu hỏi):** Tạo phiên thi có sinh viên chưa tồn tại báo lỗi ngay; chi tiết phiên thi cập nhật realtime; có nút Biên bản, không có nút thi hộ; ngân hàng câu hỏi thêm/bật/tắt tức thì.
- [x] **Kiểm thử Web API (`ass3.http`):** Vượt qua 100% các mục:
  - Mục 0 & 1: Xác thực API, đăng nhập, kiểm tra cookie 401/403.
  - Mục 2: Quản lý tài khoản (tạo sinh viên, trùng MSSV, thiếu MSSV, tự khóa, reset mật khẩu).
  - Mục 7: Đăng ký ẩn danh và quy trình cấp quyền qua API.
- [x] **Trạng thái Build:** Toàn bộ code biên dịch thành công `0 Errors, 0 Warnings`.

---

## 3. Mini-task và Câu hỏi bảo vệ

### Mini-task: So sánh trang Tạo phiên thi giữa Razor Pages (Ass2) và MVC (Ass3)
- **Tương quan cấu trúc:**
  - *Razor Pages (Ass2):* Gom chung logic xử lý trong file PageModel `Pages/Exams/Create.cshtml.cs` với các handler `OnGet()` và `OnPost()`. Dữ liệu form liên kết trực tiếp qua các thuộc tính `[BindProperty]`.
  - *MVC (Ass3):* Tách biệt rõ ràng thành 3 phần: View Model `ExamCreateViewModel.cs` (chứa dữ liệu & validation rule), Controller `ExamsController.cs` (action `Create` GET/POST xử lý điều hướng & gọi Business Service), và View `Views/Exams/Create.cshtml`.
- **Khả năng kiểm thử (Testability):**
  - Bản **MVC dễ viết Unit Test hơn** vì Action method nhận đầu vào là ViewModel độc lập và trả về `IActionResult` (`ViewResult`, `RedirectToActionResult`). Có thể dễ dàng mock Service và test trực tiếp controller method mà không cần dựng ngữ cảnh `PageContext` hay HTTP lifecycle phức tạp như Razor Pages.

### Câu hỏi bảo vệ:
1. **MVC khác Razor Pages ở đâu khi một trang có nhiều nút POST?**
   - *Razor Pages:* Sử dụng thuộc tính `asp-page-handler` để định tuyến tới các phương thức handler khác nhau trên cùng một PageModel (ví dụ: `asp-page-handler="SetOpen"`, `asp-page-handler="Reallocate"` tương ứng với `OnPostSetOpenAsync`, `OnPostReallocateAsync`).
   - *MVC:* Mỗi form POST hướng thẳng tới một Action riêng biệt thông qua `asp-action` (ví dụ: `asp-action="SetOpen"`, `asp-action="Reallocate"` trong Controller). Điều này giúp code gọn gàng, tuân thủ nguyên tắc Single Responsibility cho từng action.
2. **Vì sao ô MSSV không đặt `[Required]` ở ViewModel mà để Service kiểm tra?**
   - Biểu mẫu tài khoản (`AccountCreateViewModel` / `AccountEditViewModel`) dùng chung cho nhiều vai trò (`Admin`, `Lecturer`, `Student`).
   - Chỉ có vai trò **Sinh viên (`Student`)** mới bắt buộc phải có MSSV; các vai trò `Admin` hay `Lecturer` thì trường này phải là `null`. Do đó, nếu đặt `[Required]` cứng tại ViewModel sẽ khiến không thể tạo hoặc cập nhật tài khoản Admin/Giảng viên. Việc giao quyền kiểm tra điều kiện logic cho Service đảm bảo tính linh hoạt và đúng nghiệp vụ.
