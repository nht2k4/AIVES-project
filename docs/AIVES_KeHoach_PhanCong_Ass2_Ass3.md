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
| **TV2** | DataAccess & Giọng đọc cục bộ | - Mở rộng Repository (`StudentCodeExistsAsync`, `GetParticipantsByStudentCodeAsync`).<br>- Triển khai `InterviewRepository` và `VoiceRepository`.<br>- Tích hợp hệ thống Text-to-Speech tiếng Việt cục bộ **VieNeu-TTS** (streaming audio PCM 48kHz).<br>- Dịch vụ quản lý giọng đọc `VoiceService` (clone giọng từ audio, gán giọng cho môn học) và `VieNeuLauncher` (`IHostedService`).<br>*(Xem chi tiết: [AIVES_TV2_Ass3.md](AIVES_TV2_Ass3.md))* | `*RepositoryTests` (31 tests), `VoiceServiceTests` (31 tests), Checklist H |
| **TV3** | Business Layer & Máy trạng thái phỏng vấn | - Mở rộng nghiệp vụ quản trị tài khoản: quy tắc mật khẩu không khoảng trắng, quản lý vai trò Sinh viên (bắt buộc MSSV duy nhất), cấu hình STT từ điển thuật ngữ (`SttTerms`).<br>- Mở rộng `ExamService`: ràng buộc thí sinh phải có tài khoản Sinh viên trước khi xếp lịch thi.<br>- Thiết kế và cài đặt lõi `InterviewService`: máy trạng thái lượt thi (`Scheduled` → `InProgress` → `Completed`), xử lý giới hạn thời gian trả lời (`AnswerSeconds + 15s`), đếm và giới hạn lượt hỏi xoáy. | 148 Unit Tests (Auth, Account, Assignment, Subject, Exam, Interview), Checklist B3 |
| **TV4** | Web MVC & Web API (Auth, Accounts, Exams) | - Bộ Controller MVC: `AuthController` (Đăng nhập, Đăng ký `Pending`), `AccountsController` (quản lý MSSV), `ExamsController` (quản lý phiên thi, ngân hàng câu hỏi).<br>- Xây dựng kiến trúc nền tảng Web API: `ApiControllerBase`, hỗ trợ chuẩn phản hồi RESTful.<br>- Triển khai Endpoints: `/api/auth` (cookie authentication, trả về mã 401/403 chuẩn REST), `/api/accounts`.<br>*(Xem chi tiết: [AIVES_TV4_Ass3.md](AIVES_TV4_Ass3.md))* | Checklist A, R3, B3, E3; `ass3.http` mục 0, 1, 2, 7 |
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

## 4. Chi tiết công việc & Danh sách file thực hiện của TV4 (Assignment 3)

Dưới đây là danh sách chi tiết các chức năng mà **TV4** đã trực tiếp lập trình, kèm tên và đường dẫn file cụ thể trong project `AIVES.Ass3.MVC`:

---

### 4.1. Chức năng Xác thực, Đăng nhập & Đăng ký tài khoản (Task 4.1 & 4.4)

| Thành phần | Đường dẫn file cụ thể | Chức năng chi tiết |
|---|---|---|
| **MVC Controller** | `AIVES.Ass3.MVC/Controllers/AuthController.cs` | - Xử lý đăng nhập (`Login`), đăng xuất (`Logout`), tự đăng ký (`Register`), trang không có quyền (`AccessDenied`).<br>- Sử dụng Cookie Authentication, bảo vệ chống chuyển hướng URL độc hại (`Url.IsLocalUrl`).<br>- Gửi thông báo SignalR `accounts` ngay khi sinh viên tự đăng ký thành công. |
| **Giao diện Đăng nhập** | `AIVES.Ass3.MVC/Views/Auth/Login.cshtml` | Form đăng nhập (Email, Mật khẩu, Ghi nhớ), hiển thị danh sách tài khoản mẫu (kèm tài khoản sinh viên `sv.a@fu.edu.vn / Student@123`). |
| **Giao diện Đăng ký** | `AIVES.Ass3.MVC/Views/Auth/Register.cshtml` | Form tự đăng ký dành cho sinh viên; đăng ký xong chuyển về màn hình đăng nhập với thông báo chờ Admin cấp quyền. |
| **Giao diện Chặn quyền** | `AIVES.Ass3.MVC/Views/Auth/AccessDenied.cshtml` | Màn hình cảnh báo 403 Forbidden khi người dùng truy cập trái thẩm quyền. |
| **Điều hướng Trang chủ** | `AIVES.Ass3.MVC/Controllers/HomeController.cs` | Sinh viên đăng nhập tự động chuyển sang `/MyExams`; Admin và Giảng viên chuyển sang `/Exams`. |
| **Dữ liệu Form (ViewModel)** | `AIVES.Ass3.MVC/ViewModels/LoginViewModel.cs` | Chứa `LoginViewModel` và `RegisterViewModel` (ràng buộc kiểm tra email hợp lệ, mật khẩu tối thiểu 6 ký tự không khoảng trắng). |
| **Web API Xác thực** | `AIVES.Ass3.MVC/Controllers/Api/AuthApiController.cs` | Cung cấp REST API: `POST /api/auth/login`, `POST /api/auth/register`, `POST /api/auth/logout`, `GET /api/auth/me`. |

---

### 4.2. Chức năng Quản lý Tài khoản & Phân quyền (Task 4.2 & 4.4)

| Thành phần | Đường dẫn file cụ thể | Chức năng chi tiết |
|---|---|---|
| **MVC Controller** | `AIVES.Ass3.MVC/Controllers/AccountsController.cs` | Dành riêng cho `Admin`: Tìm kiếm, Lọc vai trò, Thêm tài khoản, Sửa tài khoản/Cấp vai trò, Khóa/Mở khóa, Đặt lại mật khẩu. |
| **Giao diện Danh sách** | `AIVES.Ass3.MVC/Views/Accounts/Index.cshtml` | - Bảng danh sách tài khoản bọc trong thẻ `<div id="account-list" data-live="accounts">` tự làm mới thời gian thực khi có người đăng ký mới qua SignalR.<br>- Hiển thị cột vai trò kèm Mã sinh viên (MSSV) cho tài khoản Sinh viên, gắn nhãn vàng *Chờ cấp quyền* cho `Pending`. |
| **Giao diện Tạo mới** | `AIVES.Ass3.MVC/Views/Accounts/Create.cshtml` | Form tạo tài khoản; có ô nhập **Mã sinh viên (MSSV)** khi chọn vai trò Sinh viên; không cho phép tạo tài khoản vai `Pending`. |
| **Giao diện Sửa & Cấp quyền** | `AIVES.Ass3.MVC/Views/Accounts/Edit.cshtml` | Nơi Admin **cấp vai trò** chính thức cho tài khoản `Pending`, quản lý MSSV và có khung đặt lại mật khẩu mới. |
| **Dữ liệu Form (ViewModel)** | `AIVES.Ass3.MVC/ViewModels/AccountViewModels.cs` | Chứa `AccountListViewModel`, `AccountCreateViewModel` (có MSSV), `AccountEditViewModel` (có MSSV & đổi mật khẩu). |
| **Web API Tài khoản** | `AIVES.Ass3.MVC/Controllers/Api/AccountsApiController.cs` | Bộ REST API `/api/accounts` đầy đủ thao tác CRUD, cấp quyền, patch active, đổi mật khẩu. |

---

### 4.3. Chức năng Quản lý Phiên thi & Ngân hàng câu hỏi (Task 4.3)

| Thành phần | Đường dẫn file cụ thể | Chức năng chi tiết |
|---|---|---|
| **MVC Controller** | `AIVES.Ass3.MVC/Controllers/ExamsController.cs` | Dành cho `Admin` và `Lecturer`: Quản lý danh sách phiên thi, tổ chức ca thi mới, theo dõi tiến trình thí sinh, chọn lại bộ câu hỏi và quản lý ngân hàng câu hỏi môn học. |
| **Giao diện Danh sách phiên** | `AIVES.Ass3.MVC/Views/Exams/Index.cshtml` | Bọc trong vùng `<div id="exam-list" data-live="exams">` tự động cập nhật khi có phiên thi mới hoặc đóng/mở phiên. |
| **Giao diện Tạo phiên thi** | `AIVES.Ass3.MVC/Views/Exams/Create.cshtml` | - Form chọn môn học, thời gian bắt đầu, khung giờ (phút/bạn), số câu hỏi chính/xoáy, thời gian trả lời.<br>- Nhập danh sách sinh viên (`MSSV; Họ tên`). **Kiểm tra chặt chẽ:** Nếu có MSSV nào chưa có tài khoản sinh viên, hệ thống báo lỗi cụ thể ngay trên form và không tạo phiên thi. |
| **Giao diện Chi tiết ca thi** | `AIVES.Ass3.MVC/Views/Exams/Details.cshtml` | - Bọc trong vùng `<div id="exam-detail" data-live="exam-{id}">` đồng bộ trạng thái thí sinh realtime (`Chờ thi` -> `Đang thi` -> `Đã thi`).<br>- Khi thí sinh bắt đầu hoặc hoàn thành, tự động hiện nút **Biên bản** (`/Interview/Transcript?participantId=...`).<br>- **Tuyệt đối không có nút "Bắt đầu thi"** (đảm bảo giảng viên/admin chỉ theo dõi, không thi thay thí sinh).<br>- Có nút **Đóng/Mở phiên** và nút **Chọn lại câu hỏi** (khi chưa có ai bắt đầu thi). |
| **Giao diện Ngân hàng câu hỏi** | `AIVES.Ass3.MVC/Views/Exams/Questions.cshtml` | Quản lý câu hỏi môn học: form thêm câu hỏi mới kèm ý chính dự kiến (`ExpectedPoints`), nút Bật/Tắt câu hỏi trực tiếp trên từng dòng. |
| **Dữ liệu Form (ViewModel)** | `AIVES.Ass3.MVC/ViewModels/ExamViewModels.cs` | Chứa `ExamListViewModel`, `ExamCreateViewModel`, `ExamDetailViewModel`, `ExamQuestionsViewModel`, `ExamAddQuestionInput`. |
| **Kiến trúc Nền tảng Web API** | `AIVES.Ass3.MVC/Controllers/Api/ApiControllerBase.cs` | Lớp base Controller Web API do TV4 dựng để toàn bộ TV5 và TV6 kế thừa, chuẩn hóa mã phản hồi 200/201/204/400/403/404 và format lỗi JSON `{ "error": "..." }`. |

---

### 4.4. Nghiệm thu & Checklist kiểm thử hoàn thành 100%

- **Checklist A (Đăng nhập):** Hoàn thành chuẩn xác, hỗ trợ tài khoản mẫu `sv.a@fu.edu.vn`.
- **Checklist R3 (Tự đăng ký):** Hoàn thành, sinh viên tự đăng ký -> Admin thấy thông báo realtime qua SignalR.
- **Checklist B3 (Tài khoản):** Hoàn thành, hỗ trợ ô MSSV duy nhất cho sinh viên, cấp quyền mượt mà.
- **Checklist E3 (Phiên thi):** Hoàn thành, chặn tạo phiên nếu thí sinh thiếu tài khoản, theo dõi thí sinh qua SignalR, xem biên bản chi tiết.
- **REST Web API (`ass3.http`):** Vượt qua 100% các bài test mục 0, 1, 2, 7.
- **Biên dịch:** Đạt `0 Errors, 0 Warnings`.


