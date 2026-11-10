# AIVES (Nhóm 4): Việc của TV4 ở Ass3

Trích xuất từ [AIVES_PhanCong_Ass2_Ass3.md](../AIVES_PhanCong_Ass2_Ass3.md), phần B (Ass3: MVC + Web API, chức năng 7 + 2 + 3).

**Phần phụ trách của TV4:** Đăng nhập, Đăng ký, Quản lý tài khoản, Quản lý phiên thi, Ngân hàng câu hỏi (giao diện MVC) và Web API nền tảng + xác thực, tài khoản.

---

## 1. Tóm tắt phân công TV4

| Mã việc | Nội dung | Công nghệ / Thành phần | Checklist / Kiểm thử |
|---|---|---|---|
| **4.1** | Đăng nhập, Đăng ký, Đăng xuất, Không có quyền | MVC: `AuthController`, `LoginViewModel`, `RegisterViewModel` + Views | Checklist A, R3; SignalR `accounts` |
| **4.2** | Quản lý tài khoản (hỗ trợ MSSV, cấp vai trò) | MVC: `AccountsController`, `AccountViewModels` + Views | Checklist B3; vùng `account-list` (`accounts`) |
| **4.3** | Quản lý phiên thi & Ngân hàng câu hỏi | MVC: `ExamsController`, `ExamViewModels` + Views | Checklist E3; vùng `exam-list` (`exams`), `exam-detail` (`exam-{id}`) |
| **4.4** | Web API nền tảng, xác thực, tài khoản | Web API: `ApiControllerBase`, `AuthApiController`, `AccountsApiController` | `ass3.http` mục 0, 1, 2, 7 |

- **Người duyệt chéo:** TV6 (TV4 ↔ TV6).
- **Phụ thuộc vào người khác:**
  - Tien 1.2 (`LiveHub`, `live.js`) để gắn vùng `data-live`.
  - TV6 6.1 (`Program.cs`, `AppController`, `_Layout`) để dựng giao diện.
  - TV3 3.1 & 3.2 (Business `AuthService`, `AccountService`, `ExamService`).
- **Người khác chờ TV4:**
  - TV5 và TV6 kế thừa `ApiControllerBase` của TV4 để viết các API tiếp theo.
  - Sinh viên truy cập vào phòng thi từ danh sách phiên thi / tài khoản sinh viên do TV4 quản lý.

---

## 2. Chi tiết triển khai

### 4.1. Đăng nhập, Đăng ký (MVC, 1,5 buổi)
- `AuthController`:
  - `Login` (GET/POST): Xác thực cookie với các claim `NameIdentifier`, `Name`, `Email`, `Role`. Kiểm tra chống Open Redirect (`Url.IsLocalUrl`).
  - `Register` (GET/POST): Tiếp nhận thông tin sinh viên tự đăng ký với vai trò `Pending`. Sau khi đăng ký thành công, tự động gửi SignalR topic `accounts` để cập nhật danh sách cho Admin.
  - `Logout` (POST) & `AccessDenied` (GET).
- `HomeController.Index`: Tự động điều hướng: Sinh viên → `/MyExams`, Quản trị viên/Giảng viên → `/Exams`.
- Form Đăng nhập bổ sung tài khoản sinh viên mẫu: `sv.a@fu.edu.vn / Student@123`.

### 4.2. Quản lý tài khoản (MVC, 2 buổi)
- `AccountsController` (`[Authorize(Roles = Roles.Admin)]`):
  - `Index`: Tìm kiếm họ tên, email; lọc vai trò (Admin, Giảng viên, Sinh viên, Chờ cấp quyền). Bảng đặt trong vùng `<div id="account-list" data-live="accounts">` tự làm mới qua SignalR. Hiển thị MSSV cạnh vai trò.
  - `Create`: Tạo tài khoản trực tiếp (có ô nhập **Mã sinh viên - MSSV**). Không cho phép tạo tài khoản vai `Pending`.
  - `Edit`: Chỉnh sửa thông tin, cập nhật MSSV; **cấp quyền** (chuyển vai trò từ `Pending` sang `Student` hoặc `Lecturer`).
  - `SetActive` & `ResetPassword`: Khóa/mở khóa tài khoản an toàn (chặn tự khóa chính mình và chặn hạ Admin cuối); đặt lại mật khẩu không chứa khoảng trắng.

### 4.3. Quản lý phiên thi & Ngân hàng câu hỏi (MVC, 3 buổi)
- `ExamsController` (`[Authorize(Roles = Roles.Admin + "," + Roles.Lecturer)]`):
  - `Index`: Danh sách phiên thi đặt trong vùng `<div id="exam-list" data-live="exams">`.
  - `Create`: Cấu hình ca thi, khung giờ, số câu hỏi chính/xoáy, thời gian trả lời và danh sách sinh viên (`MSSV; Họ tên`). Bắt lỗi từ Service nếu có MSSV chưa được tạo tài khoản sinh viên trong hệ thống (`Thí sinh ... chưa có tài khoản Sinh viên`).
  - `Details`: Bọc trong vùng `<div id="exam-detail" data-live="exam-{id}">`. Bảng thí sinh hiển thị trạng thái realtime (`Scheduled`, `InProgress`, `Completed`). Khi thí sinh đã bắt đầu hoặc hoàn thành, hiển thị nút **Biên bản** (`/Interview/Transcript?participantId=...`). **Tuyệt đối không có nút "Bắt đầu thi"** (đảm bảo giảng viên/admin không thi thay thí sinh).
  - `SetOpen`: Đóng hoặc mở lại phiên thi, gửi SignalR `exams` và `exam-{id}`.
  - `Reallocate`: Chọn lại bộ câu hỏi ngẫu nhiên cho thí sinh khi chưa ai bắt đầu thi.
  - `Questions`: Quản lý ngân hàng câu hỏi môn học.
  - `AddQuestion`: Thêm câu hỏi mới kèm các ý chính cần đạt (`ExpectedPoints`).
  - `SetQuestionActive`: Bật/tắt trạng thái sử dụng của từng câu hỏi.

### 4.4. Web API: Nền tảng + Xác thực + Tài khoản (2 buổi)
- `Controllers/Api/ApiControllerBase.cs`: Lớp cơ sở dùng chung cho toàn bộ Web API của dự án:
  - `[ApiController]`, `[Authorize]`, `[Produces("application/json")]`.
  - Thuộc tính `CurrentUserId`.
  - Các hàm chuẩn hóa phản hồi: `Respond(ServiceResult)` (thành công → 204), `Respond<T>(ServiceResult<T>)` (thành công → 200 + data), tự động map lỗi 400 / 404 / 403 với JSON format `{ "error": "..." }`.
- `AuthApiController` (`/api/auth`): `login`, `logout`, `me`, `register` (cho phép tự đăng ký ẩn danh → 201 + SignalR `accounts`).
- `AccountsApiController` (`/api/accounts`): Bộ REST API đầy đủ dành riêng cho Admin.

---

## 3. Checklist nghiệm thu (B4.3 & ass3.http)

- [ ] **Checklist A (Đăng nhập):** Đăng nhập đúng/sai thông báo chuẩn; tài khoản `Pending` báo chờ duyệt; đăng nhập `sv.a@fu.edu.vn` chuyển hướng sang Bài thi của tôi.
- [ ] **Checklist R3 (Tự đăng ký):** Đăng ký tài khoản thành công ở form hoặc API; SignalR gửi thông báo và màn hình Admin tự hiện dòng mới.
- [ ] **Checklist B3 (Tài khoản):** Tạo sinh viên bắt buộc có MSSV duy nhất; sửa/cấp vai trò hoạt động chuẩn; khóa/đặt lại mật khẩu an toàn.
- [ ] **Checklist E3 (Phiên thi & Câu hỏi):** Tạo phiên thi có sinh viên chưa tồn tại báo lỗi ngay; chi tiết phiên thi cập nhật realtime; có nút Biên bản, không có nút thi hộ; ngân hàng câu hỏi thêm/bật/tắt tức thì.
- [ ] **Kiểm thử Web API (`ass3.http`):** Vượt qua 100% các mục:
  - Mục 0 & 1: Xác thực API, đăng nhập, kiểm tra cookie 401/403.
  - Mục 2: Quản lý tài khoản (tạo sinh viên, trùng MSSV, thiếu MSSV, tự khóa, reset mật khẩu).
  - Mục 7: Đăng ký ẩn danh và quy trình cấp quyền qua API.

---

## 4. Mini-task và Câu hỏi bảo vệ

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
