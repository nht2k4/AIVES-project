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
