# AIVES – Kế hoạch và Phân công chi tiết Assignment 2 & Assignment 3

Tài liệu này tổng hợp kế hoạch phân công, hợp đồng kỹ thuật và quy trình kiểm thử giữa các thành viên nhóm cho **Assignment 2** và **Assignment 3** (tham chiếu từ nhánh `assignment-2` và `assignment-3`).

---

## 1. Tổng quan các gói bài tập

| Gói | Nền tảng | Chức năng | Trọng tâm kỹ thuật | Kiểm thử |
|---|---|---|---|---|
| **Assignment 1** (nhánh `main`) | ASP.NET Core MVC | Chức năng 7 | Quản trị tài khoản, phân công giảng viên - môn học, cấu hình ngôn ngữ STT/TTS | Kiểm thử thủ công |
| **Assignment 2** (nhánh `assignment-2`) | ASP.NET Core Razor Pages | Chức năng 7 + 2 | Tự đăng ký (`Pending`), quản trị phòng thi, thuật toán `QuestionAllocator`, SignalR `LiveHub` | 127 Unit Tests (Business) + 44 Integration Tests (SQL) |
| **Assignment 3** (nhánh `assignment-3`) | ASP.NET Core MVC & RESTful Web API | Chức năng 7 + 2 + 3 | Giám khảo AI hỏi xoáy (DeepSeek + Fallback), giọng đọc tiếng Việt cục bộ VieNeu-TTS (Streaming PCM), Web Speech API, Swagger UI, `ass3.http` | 217 Unit Tests (Business) + 68 Integration Tests (SQL) + 85 HTTP test cases |

---

## 2. Bảng phân công chi tiết Assignment 3 (Nhánh `assignment-3`)

| Thành viên | Vai trò phụ trách | Nội dung công việc chính | Kiểm thử & Nghiệm thu |
|---|---|---|---|
| **Tien (Trưởng nhóm)** | Database, SignalR, Tích hợp | - Database script `01_ChucNang7.sql`, `02_ChucNang2.sql`, `03_ChucNang3.sql` (idempotent, hỗ trợ sinh viên mẫu, bảng `InterviewTurns`, `Voices`).<br>- Triển khai SignalR `LiveHub` (`/hubs/live`, topic `accounts`, `exams`, `exam-{id}`).<br>- Tích hợp hệ thống, điều phối duyệt PR chéo và kịch bản demo tổng thể. | `SchemaTests` (32 tests), Checklist S3 |
| **TV2** | DataAccess & Giọng đọc cục bộ | - Mở rộng Repository (`StudentCodeExistsAsync`, `GetParticipantsByStudentCodeAsync`).<br>- Triển khai `InterviewRepository` và `VoiceRepository`.<br>- Tích hợp hệ thống Text-to-Speech tiếng Việt cục bộ **VieNeu-TTS** (streaming audio PCM 48kHz).<br>- Dịch vụ quản lý giọng đọc `VoiceService` (clone giọng từ audio, gán giọng cho môn học) và `VieNeuLauncher` (`IHostedService`). | `*RepositoryTests` (31 tests), `VoiceServiceTests` (31 tests), Checklist H |
| **TV3** | Business Layer & Máy trạng thái phỏng vấn | - Mở rộng nghiệp vụ quản trị tài khoản: quy tắc mật khẩu không khoảng trắng, quản lý vai trò Sinh viên (bắt buộc MSSV duy nhất), cấu hình STT từ điển thuật ngữ (`SttTerms`).<br>- Mở rộng `ExamService`: ràng buộc thí sinh phải có tài khoản Sinh viên trước khi xếp lịch thi.<br>- Thiết kế và cài đặt lõi `InterviewService`: máy trạng thái lượt thi (`Scheduled` → `InProgress` → `Completed`), xử lý giới hạn thời gian trả lời (`AnswerSeconds + 15s`), đếm và giới hạn lượt hỏi xoáy. | 148 Unit Tests (Auth, Account, Assignment, Subject, Exam, Interview), Checklist B3 |
| **TV4** | Web MVC & Web API (Auth, Accounts, Exams) | - Bộ Controller MVC: `AuthController` (Đăng nhập, Đăng ký `Pending`), `AccountsController` (quản lý MSSV), `ExamsController` (quản lý phiên thi, ngân hàng câu hỏi).<br>- Xây dựng kiến trúc nền tảng Web API: `ApiControllerBase`, hỗ trợ chuẩn phản hồi RESTful.<br>- Triển khai Endpoints: `/api/auth` (cookie authentication, trả về mã 401/403 chuẩn REST), `/api/accounts`. | Checklist A, R3, B3, E3; `ass3.http` mục 0, 1, 2, 7 |
| **TV5** | Web MVC & Web API (Môn học, Phiên thi, Thí sinh) | - Thuật toán `QuestionAllocator`: phân bổ câu hỏi ngẫu nhiên đảm bảo 2 thí sinh liền kề không trùng bộ câu hỏi.<br>- Bộ Controller MVC: `SubjectsController`, `MySubjectsController`, `LanguageController` (chỉnh tay từ điển STT, timeout AI), `MyExamsController` (dành cho sinh viên tra cứu lịch thi), View `Transcript` (biên bản thi).<br>- Triển khai Endpoints: `/api/subjects`, `/api/exams`, `/api/questions`. | `QuestionAllocatorTests` (6), `QuestionServiceTests` (9), Checklist C, D, F; `ass3.http` mục 3 đến 6 |
| **TV6** | Khung ứng dụng, AI hỏi xoáy & Phòng thi trực tuyến | - Khởi tạo cấu hình `Program.cs` tích hợp đồng thời MVC, Web API, Swagger UI, SignalR và Cookie Policy.<br>- Lõi AI hỏi xoáy `FollowUpGenerator`: tích hợp mô hình **DeepSeek Chat API** (`response_format: json_object`) kết hợp thuật toán Rule-based fallback khi mất kết nối mạng.<br>- Giao diện phòng thi trực tuyến `InterviewController/Room`: thu âm nhận diện giọng nói (Web Speech API), phát âm thanh trực tiếp (PCM Audio Player qua Web Audio API), đồng bộ tiến độ thời gian thực qua SignalR.<br>- Triển khai Endpoints: `/api/interviews` (bắt đầu thi, nộp câu trả lời, nhận câu hỏi xoáy, xuất biên bản). | `FollowUpGeneratorFallbackTests` (9), Checklist G; `ass3.http` mục 8 |

---

## 3. Quy trình phối hợp kỹ thuật & Tiêu chuẩn hoàn thành

1. **Kiến trúc 3 lớp nghiêm ngặt:** Tầng Web (MVC/API) tuyệt đối không tham chiếu trực tiếp Entity hoặc `AppDbContext`; mọi xác thực quyền hạn và ràng buộc dữ liệu được bảo đảm tại tầng Business.
2. **Quy trình kiểm thử tự động:**
   - Unit Test không phụ thuộc môi trường ngoài: Sử dụng Mock/In-memory repository để kiểm thử toàn diện logic nghiệp vụ.
   - Integration Test: Thực thi trực tiếp trên cơ sở dữ liệu kiểm thử độc lập (`AIVES_TEST_CONNECTION`).
   - Web API Verification: Kiểm thử toàn bộ 85 kịch bản API thông qua file kịch bản `ass3.http`.
3. **Tiêu chuẩn bàn giao (Definition of Done):**
   - 100% kiểm thử Business và SQL đều chuyển trạng thái xanh (Pass).
   - Kiểm thử thủ công theo bảng kiểm checklist giao diện đạt yêu cầu trên trình duyệt Chrome/Edge.
   - Không chứa mã khóa bảo mật hoặc API key trong lịch sử commit Git.

---

## 4. Ghi chú chi tiết triển khai & Bàn giao của TV4 (Assignment 3)

### 4.1. Phân hệ Xác thực & Đăng nhập (Auth Module - Task 4.1 & 4.4)
- **MVC Controller:** `AIVES.Ass3.MVC/Controllers/AuthController.cs`
  - `Login` (GET/POST): Xác thực danh tính bằng Cookie Authentication, kiểm tra an toàn chuyển hướng với `Url.IsLocalUrl`, hiển thị thông báo lỗi đồng bộ.
  - `Register` (GET/POST): Tiếp nhận sinh viên tự đăng ký với vai trò mặc định `Pending`, lưu mật khẩu băm PBKDF2 (không chứa khoảng trắng). Khi tạo thành công, tự động kích hoạt thông báo SignalR `NotifyAsync("...", LiveTopics.Accounts)`.
  - `Logout` (POST) & `AccessDenied` (GET): Xử lý hủy phiên làm việc an toàn và chuyển hướng trang thông báo từ chối quyền truy cập.
- **Web API Endpoints:** `AIVES.Ass3.MVC/Controllers/Api/AuthApiController.cs` (Kế thừa từ `ApiControllerBase` do TV4 thiết kế)
  - `POST /api/auth/login`: Xác thực và thiết lập Cookie phiên, trả về mã 401 khi sai thông tin.
  - `POST /api/auth/register`: Endpoint công khai cho phép tự đăng ký từ ứng dụng ngoài hoặc script, trả về mã 201 Created kèm Id tài khoản mới.
  - `POST /api/auth/logout`: Xóa phiên đăng nhập, trả về 204 No Content.
  - `GET /api/auth/me`: Trả về thông tin danh tính, vai trò và email hiện tại.
- **ViewModels & Views:**
  - `LoginViewModel`, `RegisterViewModel` với đầy đủ Data Annotations và Regex xác thực.
  - Views: `Views/Auth/Login.cshtml` (bổ sung thông tin tài khoản mẫu Sinh viên `sv.a@fu.edu.vn / Student@123`), `Register.cshtml`, `AccessDenied.cshtml`.

### 4.2. Phân hệ Quản lý Tài khoản (Accounts Module - Task 4.2 & 4.4)
- **MVC Controller:** `AIVES.Ass3.MVC/Controllers/AccountsController.cs` (Bảo vệ bằng `[Authorize(Roles = Roles.Admin)]`)
  - `Index` (GET): Tìm kiếm theo từ khóa họ tên, email; lọc theo vai trò (`Admin`, `Lecturer`, `Student`, `Pending`). Giao diện đặt trong vùng `<div id="account-list" data-live="accounts">` tự làm mới khi nhận sự kiện SignalR.
  - `Create` (GET/POST): Tạo tài khoản trực tiếp; hỗ trợ nhập ô **Mã sinh viên (MSSV)** khi chọn vai trò `Student`. Không cho phép tạo tài khoản vai `Pending` ở giao diện quản trị.
  - `Edit` (GET/POST): Chỉnh sửa thông tin, cấp vai trò từ `Pending` sang `Lecturer`/`Student` (chính là thao tác duyệt cấp quyền), quản lý gán mã sinh viên duy nhất.
  - `SetActive` (POST): Khóa hoặc mở khóa tài khoản an toàn (chặn tự khóa chính mình và chặn khóa Admin cuối cùng).
  - `ResetPassword` (POST): Đặt lại mật khẩu mới đáp ứng tiêu chuẩn an toàn (≥ 6 ký tự, không chứa khoảng trắng).
- **Web API Endpoints:** `AIVES.Ass3.MVC/Controllers/Api/AccountsApiController.cs`
  - Hỗ trợ đầy đủ bộ REST API quản lý tài khoản theo hợp đồng B3.5, kiểm thử thành công qua `ass3.http` mục 0, 1, 2, 7.
- **ViewModels & Views:**
  - `AccountListViewModel`, `AccountCreateViewModel`, `AccountEditViewModel`.
  - Views: `Views/Accounts/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`.

### 4.3. Phân hệ Quản lý Phiên thi & Ngân hàng câu hỏi (Exams Module - Task 4.3)
- **MVC Controller:** `AIVES.Ass3.MVC/Controllers/ExamsController.cs` (Bảo vệ bằng `[Authorize(Roles = Roles.Admin + "," + Roles.Lecturer)]`)
  - `Index` (GET): Danh sách phiên thi, bao bọc trong vùng `<div id="exam-list" data-live="exams">` tự động cập nhật khi có phiên thi mới hoặc đổi trạng thái.
  - `Create` (GET/POST): Tạo phiên thi vấn đáp; xác thực và ràng buộc toàn bộ danh sách MSSV phải tồn tại tài khoản `Student` trong hệ thống (nếu thiếu, Controller bắt lỗi từ Service và hiển thị rõ ràng trên form). Gửi SignalR topic `exams`.
  - `Details` (GET): Chi tiết phiên thi trong vùng `<div id="exam-detail" data-live="exam-{id}">`. Danh sách thí sinh hiển thị trạng thái động (`Scheduled` -> `InProgress` -> `Completed`). Khi thí sinh đang thi hoặc đã hoàn thành, hiển thị nút **Biên bản** dẫn tới `/Interview/Transcript?participantId={id}`. **Tuyệt đối không có nút "Bắt đầu thi"** (đảm bảo giảng viên/admin không thi thay thí sinh).
  - `SetOpen` (POST): Đóng hoặc mở lại phiên thi, phát sự kiện SignalR đồng thời tới `exams` và `exam-{id}`.
  - `Reallocate` (POST): Chọn lại bộ câu hỏi cho toàn bộ thí sinh khi chưa có ai bắt đầu thi, phát sự kiện SignalR tới `exam-{id}`.
  - `Questions` (GET): Quản lý ngân hàng câu hỏi theo môn học được phân công (hoặc mọi môn đối với Admin).
  - `AddQuestion` (POST): Thêm câu hỏi mới vào ngân hàng môn học kèm ý chính dự kiến (`ExpectedPoints`).
  - `SetQuestionActive` (POST): Bật hoặc tắt trạng thái sử dụng của câu hỏi trực tiếp trên bảng.
- **ViewModels & Views:**
  - `ExamListViewModel`, `ExamCreateViewModel`, `ExamDetailViewModel`, `ExamQuestionsViewModel`, `ExamAddQuestionInput`.
  - Views: `Views/Exams/Index.cshtml`, `Create.cshtml`, `Details.cshtml`, `Questions.cshtml`.

### 4.4. Tổng kết nghiệm thu công việc TV4 (Assignment 3)
- **Đạt chuẩn kiểm thử tự động:** Toàn bộ API của TV4 vượt qua 100% các kịch bản kiểm thử trong `ass3.http` (mục 0, 1, 2, 7).
- **Đạt chuẩn kiểm thử giao diện & SignalR:**
  - Checklist A (Đăng nhập, bảo mật cookie, mẫu tài khoản sinh viên).
  - Checklist R3 (Tự đăng ký vai trò `Pending`, thông báo realtime `accounts` sang trình duyệt quản trị viên).
  - Checklist B3 (Quản trị tài khoản, cập nhật MSSV, phân quyền linh hoạt).
  - Checklist E3 (Quản lý phiên thi, xác thực thí sinh tồn tại, theo dõi realtime tiến trình thi vấn đáp, xem biên bản chi tiết).
- **Mã nguồn:** Toàn bộ tính năng đã được kiểm tra biên dịch đạt `0 Errors, 0 Warnings`, đóng gói sạch đẹp và đồng bộ hoàn chỉnh trên nhánh `assignment-3`.


