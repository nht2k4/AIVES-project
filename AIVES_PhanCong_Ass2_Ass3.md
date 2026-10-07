# AIVES (Nhóm 4): Phân công Ass2 và Ass3

Đây là **file duy nhất** nhóm cần đọc cho Ass2 và Ass3: kế hoạch, giao việc từng người, hợp đồng (database, tầng Business, giao diện, SignalR) và kiểm thử. Phát file này kèm gói tương ứng.

| Gói | Phát khi nào | Trong gói có | Nhóm phải viết |
|---|---|---|---|
| `AIVES_Goi_Ass2` | sau khi xong Ass1 | solution 3 lớp: Entities, `AppDbContext`, Interfaces, DTOs, Models (hợp đồng, **không sửa**), stub `NotImplementedException`, **124 test Business + 44 test SQL đang đỏ**, khung web Razor Pages rỗng | toàn bộ code của chức năng 7 + 2 bằng Razor Pages, đăng ký tài khoản, SignalR, script database |
| `AIVES_Goi_Ass3` | sau khi xong Ass2 | như trên cho chức năng 7 + 2 + 3: **204 test Business + 63 test SQL đỏ**, khung web MVC rỗng (đã có gói Swagger), file `ass3.http` (78 lời gọi API có kết quả mong đợi) | toàn bộ code của chức năng 7 + 2 + 3 bằng MVC **và Web API**, SignalR, VieNeu-TTS, AI hỏi xoáy, script database |

> **Trạng thái khi phát gói:** phần việc của **Tien** đã xong và có sẵn trong gói: script database (`Database/01_ChucNang7.sql`, `02_ChucNang2.sql`, Ass3 thêm `03_ChucNang3.sql`) và SignalR (`Infrastructure/LiveHub.cs`, `wwwroot/js/live.js`). Các bạn dùng ngay để tạo database và nối trang; việc còn lại của Tien là tích hợp, duyệt PR và demo.

Mỗi gói là **một repo độc lập**, không chép đè lên gói trước. Được phép (và nên) **tham khảo code chính nhóm đã viết ở bài trước** (Ass1 → Ass2 → Ass3); bộ test sẽ cho biết chỗ nào cần sửa theo yêu cầu mới.

**Nguyên tắc học thật:** mỗi người tự viết phần của mình; bị kẹt quá 30 phút thì hỏi người cùng việc hoặc Tien, **không** chép code của người khác hay của nhóm khác. Khi bảo vệ, mỗi người phải giải thích được từng dòng mình viết (mỗi người có một mini-task và câu hỏi bảo vệ ở cuối phần việc của mình).

## Mục lục

- [0. Chung cho cả hai bài](#0-chung-cho-cả-hai-bài)
- [PHẦN A: ASS2 (Razor Pages, chức năng 7 + 2, 3 lớp, SignalR)](#phần-a-ass2-razor-pages-chức-năng-7--2-3-lớp-signalr)
  - [A1. Kế hoạch](#a1-kế-hoạch) · [A2. Giao việc](#a2-giao-việc) · [A3. Hợp đồng](#a3-hợp-đồng) · [A4. Kiểm thử](#a4-kiểm-thử)
- [PHẦN B: ASS3 (MVC + Web API, chức năng 7 + 2 + 3, 3 lớp, SignalR)](#phần-b-ass3-mvc--web-api-chức-năng-7--2--3-3-lớp-signalr)
  - [B1. Kế hoạch](#b1-kế-hoạch) · [B2. Giao việc](#b2-giao-việc) · [B3. Hợp đồng](#b3-hợp-đồng) · [B4. Kiểm thử](#b4-kiểm-thử)
- [Phụ lục: tài khoản mẫu, lệnh hay dùng](#phụ-lục)

---

# 0. Chung cho cả hai bài

## 0.1 Kiến trúc 3 lớp

```
AIVES.AssN.<Web>   (Presentation: Pages hoặc Controllers/Views, Hub SignalR)   chỉ tham chiếu Business
        │  gọi interface IxxxService, nhận DTO + ServiceResult
AIVES.Business     (Services, Interfaces, DTOs, Models, ServiceResult)       tham chiếu DataAccess
        │  gọi interface IxxxRepository, nhận Entity
AIVES.DataAccess   (Entities, AppDbContext Database First, Repositories)     nói chuyện với SQL Server
```

Bốn luật không được phá:
1. Tầng web **không** dùng `AppDbContext` hay Entity; không có luật nghiệp vụ trong Controller/PageModel.
2. Mọi luật và **mọi kiểm tra quyền** nằm ở Service (thuộc tính `[Authorize]` chỉ chặn sớm; Service mới quyết định).
3. Service trả `ServiceResult` (`Validation` / `NotFound` / `Forbidden`), không ném exception cho lỗi nghiệp vụ.
4. Tầng web chỉ gọi **một dòng** `builder.Services.AddBusinessLayer(...)`; nó không biết DataAccess tồn tại.

## 0.2 Tính năng mới so với Ass1 (có ở cả Ass2 và Ass3)

**Tự đăng ký tài khoản, admin cấp vai trò.**
- Trang đăng nhập có link **Tạo tài khoản** (`/Auth/Register`, ai cũng vào được): Họ tên, Email, Mật khẩu, Nhập lại mật khẩu.
- Tài khoản mới có vai trò **`Pending`** ("Chờ cấp quyền"), đang hoạt động nhưng **chưa đăng nhập được**: đăng nhập đúng mật khẩu thì báo *"Tài khoản đang chờ quản trị viên cấp vai trò..."*; sai mật khẩu vẫn là thông báo chung (không lộ trạng thái).
- Admin vào **Tài khoản**, thấy huy hiệu vàng *Chờ cấp quyền*, bấm **Sửa** và chọn vai trò. Từ đó người dùng đăng nhập được.
- `Pending` không giữ được phiên đăng nhập (`GetActiveRoleAsync` trả `null`), nên admin đổi ai đó về `Pending` thì người đó bị đăng xuất ở request kế tiếp.

**SignalR: cập nhật trực tiếp, không cần F5.** Một hub `LiveHub` ở `/hubs/live` (chỉ người đã đăng nhập). Server gửi sự kiện `changed(topic, message)`; trang nào có vùng `<div id="..." data-live="<topic>">` thì vùng đó tự tải lại và hiện thông báo nhỏ. Chi tiết ở A3.4 và B3.4.

## 0.3 Bắt đầu với gói (mỗi người làm một lần)

1. Tien tạo repo GitHub từ gói, đẩy lên nhánh `main`, bật bảo vệ nhánh (bắt buộc PR + 1 người duyệt). Mọi người clone.
2. Mở `AIVES.AssN.sln`, `dotnet build` (phải **sạch**), `dotnet test AIVES.Tests` (phải **đỏ hết**: 124 hoặc 204 test báo `NotImplementedException`). Đỏ là đúng: đó là việc của cả nhóm.
3. Test SQL (`AIVES.Tests.Data`) tự **bỏ qua** cho tới khi đặt biến môi trường trỏ tới một database **riêng để test**:
   ```
   $env:AIVES_TEST_CONNECTION = "Server=.;Database=AIVESDb_Test;Trusted_Connection=True;TrustServerCertificate=True"
   dotnet test AIVES.Tests.Data
   ```
4. Chạy một nhóm test: `dotnet test AIVES.Tests --filter "FullyQualifiedName~AccountServiceTests"`.

## 0.4 Quy trình Git (cả hai bài)

- Một việc = một nhánh `tv3/3.2-account-service` = một PR. PR nhỏ (dưới khoảng 400 dòng).
- Mô tả PR: việc gì, test nào đã xanh (dán kết quả), mục checklist nào đã tick, ảnh chụp với việc giao diện.
- **Người duyệt chạy thử lại** (không tin lời người viết): test xanh, checklist bấm lại từng mục.
- Cặp duyệt chéo cố định: Tien ↔ TV2, TV3 ↔ TV5, TV4 ↔ TV6 (người duyệt học được phần của bạn).
- Không ai sửa file hợp đồng (Entities, `AppDbContext`, Interfaces, DTOs, Models, test). Thấy hợp đồng sai thì báo Tien, cả nhóm thống nhất rồi một người sửa.
- Không commit `bin/`, `obj/`, `.vs/`, `VieNeu-TTS/`, khóa API. Gắn tag `ass2-final` / `ass3-final` khi nộp.

## 0.5 "Xong" một việc nghĩa là

- [ ] Test của việc đó xanh (hoặc mục checklist tương ứng đã tick nếu là giao diện).
- [ ] Không còn `NotImplementedException` trong file của mình; build không cảnh báo mới.
- [ ] PR được duyệt và đã merge; người duyệt chạy lại được.
- [ ] Bạn trả lời được câu hỏi bảo vệ của việc đó.

---

# PHẦN A: ASS2 (Razor Pages, chức năng 7 + 2, 3 lớp, SignalR)

**Đề:** làm lại chức năng 7 (quản trị: tài khoản, phân công giảng viên theo môn, ngôn ngữ STT/TTS) bằng **Razor Pages**, thêm **tự đăng ký tài khoản**, thêm **chức năng 2** (phiên thi vấn đáp, lịch thi, ngân hàng câu hỏi, chọn bộ câu hỏi không trùng giữa hai thí sinh liền nhau) và **SignalR**.

## A1. Kế hoạch

### A1.1 Bảng việc

| Người | Việc | Test / kiểm tra |
|---|---|---|
| **Tien** | ~~1.1 script `01_ChucNang7.sql`~~ · ~~1.2 script `02_ChucNang2.sql`~~ · ~~1.3 SignalR (`LiveHub`, `live.js`)~~ (**đã xong, có trong gói**) · 1.4 tích hợp, duyệt PR, demo | `SchemaTests` (24) · checklist S |
| **TV2** | 2.1 repository chức năng 7 · 2.2 repository chức năng 2 · 2.3 DI tầng DataAccess | `*RepositoryTests` (20) |
| **TV3** | 3.1 băm mật khẩu + `AuthService` · 3.2 `AccountService` (có đăng ký) · 3.3 phân công, môn, ngôn ngữ · 3.4 `ExamService` | 9 + 9 + 30 + 20 + 41 test |
| **TV4** | 4.1 trang Đăng nhập, Đăng ký, Đăng xuất, Không có quyền · 4.2 trang Tài khoản (cấp vai trò, cập nhật trực tiếp) | checklist A, B, R |
| **TV5** | 5.1 `QuestionAllocator` + `QuestionService` · 5.2 trang Môn học, Môn của tôi, Ngôn ngữ · 5.3 trang Ngân hàng câu hỏi | 6 + 9 test · checklist C, D, E (câu hỏi) |
| **TV6** | 6.1 `Program.cs` + DI Business + nền tầng web · 6.2 trang Phiên thi (Danh sách, Tạo, Chi tiết) có cập nhật trực tiếp | checklist A (phần khung), E, S |

### A1.2 Ai chờ ai

```
Tien 1.1, 1.2 (database) ──► TV2 chạy Tests.Data ──► TV2 2.3 (AddDataAccessLayer)
                                                          │
TV3 3.1..3.4, TV5 5.1 (test trong bộ nhớ, KHÔNG cần DB) ──┤
                                                          ▼
                          TV6 6.1 (Program.cs, AddBusinessLayer, AppPageModel, _Layout) ──► TV4, TV5, TV6 làm trang
Tien 1.3 (LiveHub + live.js) ──► TV4 4.2 (vùng "accounts"), TV6 6.2 (vùng "exams", "exam-{id}")
```

| Việc | Cần có trước | Chưa có thì làm gì |
|---|---|---|
| TV2 2.1, 2.2 | database của Tien | viết code theo hợp đồng trước, chạy test sau |
| TV3, TV5 5.1 | không | làm ngay ngày đầu |
| TV6 6.1 | không (stub vẫn biên dịch) | làm **đầu tiên**, đẩy sớm nhất: cả nhóm cần |
| TV4, TV5 trang | TV6 6.1 | dựng HTML trước, nối Service sau |
| Gắn SignalR vào trang | Tien 1.3 | làm trang trước, thêm vùng `data-live` sau |

### A1.3 Giai đoạn

| Giai đoạn | Việc | Sang giai đoạn sau khi |
|---|---|---|
| **A. Nền** (2 buổi) | (database và SignalR của Tien đã có sẵn) TV6 6.1; TV2 2.1; TV3 3.1 | build sạch; mở được trang đăng nhập; `SchemaTests` xanh |
| **B. Tầng dưới** (3 buổi, song song) | TV2 2.2, 2.3; TV3 3.2 đến 3.4; TV5 5.1; Tien 1.3 | **124 test Business xanh**, 44 test SQL xanh |
| **C. Giao diện** (3 đến 4 buổi) | TV4 4.1, 4.2; TV5 5.2, 5.3; TV6 6.2 | checklist A, B, C, D, E, R, S tick hết |
| **D. Tích hợp, demo** (1 đến 2 buổi) | Tien 1.4: chạy kịch bản demo, sửa lỗi chéo, mini-task, tập bảo vệ | demo chạy trơn |

**Kịch bản demo Ass2:** sinh viên tự tạo tài khoản → trang Tài khoản của admin (đang mở) tự hiện dòng mới → admin cấp vai trò Giảng viên → giảng viên đăng nhập, được phân công PRN222 → thêm câu hỏi → tạo phiên thi 4 sinh viên → trang Phiên thi ở trình duyệt thứ hai tự hiện phiên mới → chỉ ra hai sinh viên liền nhau không trùng câu → chọn lại bộ câu hỏi → giảng viên gõ tay địa chỉ phiên thi của môn khác → bị chặn.

### A1.4 Định nghĩa xong Ass2

- [ ] `dotnet build` sạch; 124/124 test `AIVES.Tests` xanh; 44/44 test `AIVES.Tests.Data` xanh trên SQL Server thật.
- [ ] Checklist A, B, C, D, E, R, S (mục A4.3) tick đủ, mỗi mục có người duyệt bấm lại.
- [ ] Không còn `NotImplementedException` trong solution (`grep -r NotImplementedException --include=*.cs`).
- [ ] Mỗi người đã làm mini-task và trả lời câu hỏi bảo vệ. Tag `ass2-final`.

## A2. Giao việc

### Tien (trưởng nhóm): database, SignalR, tích hợp

> **Đã xong:** 1.1, 1.2, 1.3 có sẵn trong gói (`Database/`, `AIVES.Ass2.Razor/Infrastructure/LiveHub.cs`, `wwwroot/js/live.js`). Người dùng lại: TV2 (chạy `Tests.Data`), TV6 (nạp 2 thẻ `<script>` của SignalR vào `_Layout` khi đã đăng nhập, `AddSignalR`, `MapHub<LiveHub>(LiveHub.Path)`), TV4 và TV6 (đặt vùng `data-live`, gọi `NotifyAsync`). Mô tả bên dưới giữ lại để cả nhóm hiểu và để Tien trả lời câu hỏi bảo vệ.

**1.1 `Database/01_ChucNang7.sql`** (1 buổi). Script **xóa và tạo lại** `AIVESDb`, tạo 3 bảng `Accounts`, `Subjects`, `LecturerSubjects` và dữ liệu mẫu đúng **A3.1**. Điểm mới so với Ass1: `CK_Accounts_Role` cho phép `'Admin'`, `'Lecturer'`, `'Pending'`; thêm tài khoản mẫu `moi.dangky@fu.edu.vn` vai `Pending`.

**1.2 `Database/02_ChucNang2.sql`** (1 buổi). 4 bảng `Questions`, `ExamSessions`, `ExamParticipants`, `ParticipantQuestions` + câu hỏi mẫu. **Idempotent** (chạy lại không lỗi, không mất dữ liệu). Kiểm tra: `dotnet test AIVES.Tests.Data --filter "FullyQualifiedName~SchemaTests"` (24 test, so từng ràng buộc với hợp đồng).

**1.3 SignalR** (1,5 buổi). Theo **A3.4**:
- `AIVES.Ass2.Razor/Infrastructure/LiveHub.cs`: lớp `LiveHub : Hub` có `[Authorize]`, hằng `Path = "/hubs/live"`; lớp `LiveTopics` (hằng `Accounts`, `Exams`, hàm `Exam(int id)`); hàm mở rộng `NotifyAsync(this IHubContext<LiveHub> hub, string message, params string[] topics)` gửi `changed(topic, message)` cho mọi người.
- `wwwroot/js/live.js`: kết nối hub (`withAutomaticReconnect`), khi nhận `changed` thì tìm các vùng `[data-live][id]` cùng topic, `fetch(location.href)`, thay `innerHTML` của đúng vùng đó bằng vùng cùng `id` của trang mới tải, hiện thông báo nhỏ góc dưới 4 giây. Trang không có vùng nào thì không kết nối.
- `_Layout.cshtml`: chỉ khi đã đăng nhập mới nạp `https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.7/signalr.min.js` và `~/js/live.js`.
- Học trước: Hub, `IHubContext<T>`, WebSocket và "negotiate", vì sao server gửi chủ đề chứ không gửi dữ liệu.

**1.4 Tích hợp và demo** (suốt bài). Merge theo thứ tự ở A1.2; mỗi tối chạy toàn bộ test; viết kịch bản demo; điều phối duyệt chéo.

**Mini-task:** (1) viết thêm một topic `subjects`: khi admin phân công giảng viên, trang Môn của tôi của giảng viên đó tự cập nhật. (2) Đo: mở 2 trình duyệt, đo thời gian từ lúc bấm Tạo phiên thi tới lúc trình duyệt kia đổi (DevTools, tab Network → WS).
**Câu hỏi bảo vệ:** (1) Vì sao script 02 phải idempotent còn 01 thì được xóa database? (2) Chỉ mục có điều kiện cần `SET QUOTED_IDENTIFIER ON` vì sao? (3) SignalR khác gì so với trang tự `setInterval` gọi lại server mỗi 5 giây? (4) Vì sao thông điệp SignalR không chứa email hay dữ liệu riêng mà để trang tự tải lại?

### TV2: DataAccess (repository)

**2.1 Repository chức năng 7** (1,5 buổi): `AccountRepository`, `SubjectRepository`, `LecturerSubjectRepository`. Có thể lấy từ Ass1 của bạn. Đặc tả ở **A3.2**.
**2.2 Repository chức năng 2** (1,5 buổi): `QuestionRepository`, `ExamRepository` (thêm **cả đồ thị** phiên → thí sinh → câu hỏi bằng một `SaveChanges`).
**2.3 `AIVES.DataAccess/DependencyInjection.cs`** (0,25 buổi): `AddDataAccessLayer(this IServiceCollection, string connectionString)` đăng ký `AppDbContext` (SQL Server) và 5 repository, vòng đời **Scoped**.
- Học trước: LINQ to Entities, `AsNoTracking` và theo dõi thay đổi, `Include/ThenInclude`, `SaveChangesAsync`.
- Kiểm tra: `AccountRepositoryTests` (6), `SubjectRepositoryTests` (5), `LecturerSubjectRepositoryTests` (2), `QuestionRepositoryTests` (2), `ExamRepositoryTests` (5).
- Hay sai: dùng `AsNoTracking` ở hàm mà Service sẽ sửa (`GetByIdAsync` của môn, `GetDetailAsync` của phiên thi); `ToLower()` trong truy vấn; tạo `new AppDbContext()`.

**Mini-task:** phân trang `SearchAsync(keyword, role, page, pageSize)` bằng `Skip/Take`, dán câu SQL thật (log EF) vào PR. **Câu hỏi bảo vệ:** (1) Vì sao `ExamRepository.GetDetailAsync` không dùng `AsNoTracking` còn `GetListAsync` thì có? (2) N+1 là gì, dự án tránh ở đâu? (3) Vì sao repository và `DbContext` là Scoped?

### TV3: Business chức năng 7 và phiên thi

Đặc tả đầy đủ ở **A3.2**; test là bản đặc tả bằng code, đọc test trước khi viết. Có thể lấy code Ass1 của bạn rồi sửa cho xanh.

**3.1** `Security/Pbkdf2PasswordHasher.cs` + `Services/AuthService.cs` (1,5 buổi). Điểm mới: chặn đăng nhập `Pending` **sau** khi kiểm mật khẩu; `GetActiveRoleAsync` trả `null` cho `Pending`. Test: `PasswordHasherTests` (9), `AuthServiceTests` (9).
**3.2** `Services/AccountService.cs` (1,5 buổi). Điểm mới: `RegisterAsync` (cùng kiểm tra như `CreateAsync`, vai trò luôn `Pending`). Tự tạo hai lớp `internal static` dùng chung: `AccountRules` (chuẩn hóa email, kiểm email, mật khẩu tối thiểu 6) và `Mapper` (Entity → DTO, `ParseRole`; vai trò lạ → `Pending`). Test: `AccountServiceTests` (30).
**3.3** `AssignmentService`, `SubjectService`, `LanguageConfigService` (1,5 buổi). `CanAccessSubjectAsync` là hàm quan trọng nhất hệ thống. Test: `AssignmentServiceTests` (10), `SubjectServiceTests` (4), `LanguageConfigServiceTests` (6).
**3.4** `Services/ExamService.cs` (2 buổi). Tạo phiên thi, đọc danh sách sinh viên, xếp khung giờ, gọi `QuestionAllocator` (của TV5), chọn lại bộ câu hỏi. Test: `ExamServiceTests` (41, có giá trị biên).

**Mini-task:** TDD một luật mới "mật khẩu không được trùng phần trước `@` của email" (test trước, chụp đỏ, rồi code). **Câu hỏi bảo vệ:** (1) Vì sao kiểm quyền ở Service mà không chỉ `[Authorize]`? (2) Vì sao chặn `Pending` sau khi kiểm mật khẩu? (3) `ReallocateAsync` vì sao phải xóa bộ cũ và lưu trước rồi mới thêm bộ mới? (4) `AccountService` test được không cần SQL Server nhờ đâu?

### TV4: Đăng nhập, Đăng ký, Tài khoản (Razor Pages)

**4.1** `Pages/Auth/Login`, `Register`, `Logout`, `AccessDenied` (2 buổi). Login tạo cookie với claim `NameIdentifier`, `Name`, `Email`, `Role`; chỉ nhận `ReturnUrl` cục bộ. **Register**: `InputModel` có `[Required]`, `[EmailAddress]`, `[StringLength(100, MinimumLength = 6)]`, `[Compare]`; gọi `IAccountService.RegisterAsync`; thành công thì gửi SignalR topic `accounts` (`"Có tài khoản mới đăng ký, đang chờ cấp vai trò."`), flash thông báo, về trang đăng nhập. Đã đăng nhập mà mở Register thì về `/`.
**4.2** `Pages/Accounts/Index`, `Create`, `Edit` (2,5 buổi). Danh sách có ô lọc vai trò (gồm "Chờ cấp quyền"), cột vai trò hiện huy hiệu vàng *Chờ cấp quyền* cho `Pending`; bảng nằm trong `<div id="account-list" data-live="accounts">`. Trang Tạo **không** cho chọn `Pending`. Trang Sửa là nơi **cấp vai trò**. Khóa/Mở khóa, đặt lại mật khẩu.
- Học trước: `PageModel`, `[BindProperty]`, handler `OnPostXxxAsync` (`asp-page-handler`), PRG + `TempData`, `Html.GetEnumSelectList<AppRole>()`.

**Mini-task:** thêm số đếm "N tài khoản chờ cấp quyền" trên thanh điều hướng của admin, cũng tự cập nhật bằng SignalR. **Câu hỏi bảo vệ:** (1) Vì sao đăng xuất là POST? (2) `Url.IsLocalUrl` chống tấn công gì? (3) Nếu bỏ `[Compare]` thì Service còn kiểm "nhập lại mật khẩu" không, và có cần không?

### TV5: Câu hỏi, Môn học, Ngôn ngữ

**5.1** `Services/QuestionAllocator.cs` + `Services/QuestionService.cs` (2 buổi). Thuật toán ở **A3.2**. Test: `QuestionAllocatorTests` (6), `QuestionServiceTests` (9). Ghi vào PR: độ phức tạp và một ví dụ chạy tay 5 câu, 3 sinh viên, 2 câu mỗi người.
**5.2** `Pages/Subjects/Index`, `Details` (phân công/gỡ, khung Ngôn ngữ, khung **Ngân hàng câu hỏi** với nút "Quản lý câu hỏi"), `Pages/MySubjects/Index` (nút Đổi ngôn ngữ, **Câu hỏi**), `Pages/Language/Edit` (2,5 buổi).
**5.3** `Pages/Exams/Questions` (1 buổi): bảng câu hỏi (Tắt/Bật), form Thêm câu hỏi.

**Mini-task:** trang Ngân hàng câu hỏi cho phép **sửa** nội dung một câu (thêm hàm vào `IQuestionService` cùng test của bạn). **Câu hỏi bảo vệ:** (1) Giải thích `QuestionAllocator` với 8 câu, 3 câu mỗi người, 4 sinh viên (vẽ bảng). (2) Vì sao giảng viên gõ tay `/Language/Edit?subjectId=` của môn khác vẫn bị chặn dù trang cho mọi người đã đăng nhập vào?

### TV6: Khung ứng dụng và trang Phiên thi

**6.1** (2 buổi, **làm đầu tiên**):
- `AIVES.Business/DependencyInjection.cs`: `AddBusinessLayer(this IServiceCollection, string connectionString)` gọi `AddDataAccessLayer` của TV2, đăng ký `IPasswordHasher` (**Singleton**) và 7 service (**Scoped**): Auth, Account, Subject, Assignment, LanguageConfig, Question, Exam.
- `Program.cs`: `AddRazorPages` với `AuthorizeFolder("/")`; cho ẩn danh `/Auth/Login`, `/Auth/Register`, `/Auth/AccessDenied`, `/Error`; policy `AdminOnly` (`/Accounts`, `/Subjects`), `LecturerOnly` (`/MySubjects`), `Staff` (`/Exams`). Cookie `.AIVES.Ass2`, 8 giờ, trượt; `OnValidatePrincipal` gọi `GetActiveRoleAsync` mỗi request. `AddSignalR()`, `MapHub<LiveHub>(LiveHub.Path)`. Thứ tự middleware đúng.
- `Infrastructure/AppPageModel.cs` (`CurrentUserId`, `FlashSuccess/Error`, `MapFailure`), `ClaimsPrincipalExtensions`, `EnumExtensions`; `Pages/Index` chuyển hướng theo vai trò; `Shared/_Layout`, `_Alerts`, `_ValidationScriptsPartial`.
**6.2** `Pages/Exams/Index`, `Create`, `Details` (3 buổi). Bảng danh sách trong vùng `exam-list` / topic `exams`; nội dung chi tiết trong vùng `exam-detail` / topic `exam-{id}`. Tạo phiên thành công → gửi `exams`; Đóng/Mở → gửi `exams` và `exam-{id}`; Chọn lại câu hỏi → gửi `exam-{id}`.

**Mini-task:** đảo `UseAuthentication` và `UseAuthorization`, ghi lại điều xảy ra. **Câu hỏi bảo vệ:** (1) Trình bày thứ tự middleware. (2) Vì sao `DbContext` Scoped còn bộ băm Singleton? (3) Vì sao gửi SignalR ở PageModel **sau** khi Service thành công mà không gửi trong Service?

## A3. Hợp đồng

### A3.1 Database (Tien viết; Entities trong gói là bản mô tả chính xác)

Quy ước tên ràng buộc: `PK_Bảng`, `UQ_Bảng_Cột`, `FK_Bảng_BảngCha`, `CK_Bảng_Cột`, `DF_Bảng_Cột`. Chữ có dấu `NVARCHAR`, chữ không dấu `VARCHAR`, thời gian `DATETIME2`, giờ hệ thống ghi bằng `SYSUTCDATETIME()` (UTC). File `.sql` lưu **UTF-8 có BOM**.

**`01_ChucNang7.sql`**

`Accounts`: `Id` INT IDENTITY `PK_Accounts` · `FullName` NVARCHAR(100) NOT NULL · `Email` VARCHAR(150) NOT NULL `UQ_Accounts_Email` · `PasswordHash` VARCHAR(256) NOT NULL (`vòng.salt.key`) · `Role` VARCHAR(20) NOT NULL `CK_Accounts_Role` ∈ `'Admin','Lecturer','Pending'` · `IsActive` BIT NOT NULL · `CreatedAt` DATETIME2 NOT NULL `DF_Accounts_CreatedAt` = `SYSUTCDATETIME()`.

`Subjects`: `Id` `PK_Subjects` · `Code` VARCHAR(20) `UQ_Subjects_Code` · `Name` NVARCHAR(150) · `IsActive` BIT · `SttLanguage`, `TtsLanguage` VARCHAR(10) `CK_Subjects_SttLanguage` / `CK_Subjects_TtsLanguage` ∈ `'vi-VN','en-US'` · `LanguageUpdatedAt` DATETIME2 NULL · `LanguageUpdatedBy` VARCHAR(150) NULL.

`LecturerSubjects`: `LecturerId`, `SubjectId` (khóa ghép `PK_LecturerSubjects`; `FK_LecturerSubjects_Accounts`, `FK_LecturerSubjects_Subjects` đều ON DELETE CASCADE) · `AssignedAt` DATETIME2 `DF_LecturerSubjects_AssignedAt`.

Dữ liệu mẫu (mật khẩu đã băm, dùng đúng chuỗi này):

| Email | Họ tên | Vai trò | Mật khẩu | `PasswordHash` |
|---|---|---|---|---|
| admin@fu.edu.vn | Quản trị hệ thống | Admin | Admin@123 | `100000.uSRX8Si9+0CW3kmVg4f2+A==.MZn9CqsOUDr/aDtD0lfZynz0ovwimrQOiyYlxU3CFOE=` |
| an.nv@fu.edu.vn | Nguyễn Văn An | Lecturer | Lecturer@123 | `100000.aVHHCR5cg5+0AU+lX4mHHA==.+hubtA5WNK07tC/mMFfOkFw9UC644hUi1xpeZR9+nMU=` |
| binh.tt@fu.edu.vn | Trần Thị Bình | Lecturer | Lecturer@123 | (như dòng trên) |
| moi.dangky@fu.edu.vn | Lê Mới Đăng Ký | **Pending** | Lecturer@123 | (như dòng trên) |

Môn: `PRN222` (vi/vi), `SWP391` (vi), `ENW492c` (en/en), `SSG104` (vi), `PRN211` (đã ngừng, `IsActive = 0`). Phân công: an.nv → PRN222, binh.tt → ENW492c.

**`02_ChucNang2.sql`** (idempotent: `IF OBJECT_ID(...) IS NULL CREATE TABLE`, chỉ chèn mẫu khi bảng trống)

`Questions`: `Id` `PK_Questions` · `SubjectId` `FK_Questions_Subjects` CASCADE · `Content` NVARCHAR(1000) NOT NULL · `ExpectedPoints` NVARCHAR(1000) NULL (ý chính, ngăn cách `;`) · `IsActive` BIT `DF_Questions_IsActive` = 1.

`ExamSessions`: `Id` `PK_ExamSessions` · `SubjectId` `FK_ExamSessions_Subjects` (**không** cascade) · `Title` NVARCHAR(150) · `StartTime` DATETIME2 · `SlotMinutes` INT `CK_ExamSessions_Slot` 1..120 · `MainQuestionCount` `CK_ExamSessions_Main` 1..20 · `MaxFollowUps` `CK_ExamSessions_FU` 0..40 · `MaxFollowUpsPerQuestion` `CK_ExamSessions_FUQ` 0..5 · `AnswerSeconds` `CK_ExamSessions_Ans` 10..600 · `Status` VARCHAR(10) `CK_ExamSessions_Status` ∈ `'Open','Closed'` · `CreatedBy` `FK_ExamSessions_Accounts` · `CreatedAt` `DF_ExamSessions_CreatedAt`.

`ExamParticipants`: `Id` `PK_ExamParticipants` · `SessionId` `FK_ExamParticipants_Sessions` CASCADE · `StudentCode` VARCHAR(30) · `FullName` NVARCHAR(100) · `SlotStart`, `SlotEnd` DATETIME2 · `Status` VARCHAR(15) `CK_ExamParticipants_Status` ∈ `'Scheduled','InProgress','Completed'` · `UQ_ExamParticipants_Student` UNIQUE `(SessionId, StudentCode)`.

`ParticipantQuestions`: `ParticipantId` (`FK_ParticipantQuestions_Participants` CASCADE), `QuestionId` (`FK_ParticipantQuestions_Questions`), khóa ghép `PK_ParticipantQuestions` · `OrderNo` INT (từ 1).

Dữ liệu mẫu: ít nhất 8 câu PRN222 và 4 câu ENW492c, câu nào cũng có `ExpectedPoints`.

### A3.2 Tầng Business và DataAccess (interface đã cho sẵn, test là đặc tả)

**Repository** (TV2). Hàm đọc để hiển thị dùng `AsNoTracking`; hàm trả entity mà Service sẽ sửa thì **có theo dõi**.

| Interface | Hàm | Ghi chú |
|---|---|---|
| `IAccountRepository` | `GetByIdAsync`, `GetByEmailAsync`, `EmailExistsAsync(email, excludeId)`, `SearchAsync(keyword, role)`, `GetActiveByRoleAsync`, `CountActiveByRoleAsync`, `AddAsync`, `UpdateAsync` | Search: họ tên **hoặc** email chứa từ khóa, sắp theo `Role` rồi `FullName` |
| `ISubjectRepository` | `GetByIdAsync` (theo dõi), `GetWithLecturersAsync`, `GetAllWithAssignmentsAsync`, `GetByLecturerAsync`, `UpdateAsync` | sắp theo `Code` |
| `ILecturerSubjectRepository` | `ExistsAsync`, `CountByLecturerAsync`, `AddAsync`, `RemoveAsync` (trả `false` nếu không có) | không nuốt `DbUpdateException` |
| `IQuestionRepository` | `GetBySubjectAsync(subjectId, onlyActive)`, `GetByIdAsync` (theo dõi), `AddAsync`, `UpdateAsync` | sắp theo `Id` |
| `IExamRepository` | `GetListAsync(subjectIds)` (`null` = mọi môn, rỗng = không môn nào; mới nhất trước), `GetDetailAsync` (theo dõi, nạp đủ đồ thị), `AddAsync` (cả đồ thị, một lần lưu), `SaveAsync` | |

**Service** (TV3, TV5). Kiểm theo thứ tự, dừng ở lỗi đầu tiên, không để thay đổi nửa chừng.

- `IPasswordHasher`: PBKDF2-SHA256, 100.000 vòng, salt 16 byte ngẫu nhiên mỗi lần, key 32 byte, chuỗi `{vòng}.{salt base64}.{key base64}`; `Verify` đọc số vòng từ chuỗi, so sánh **thời gian cố định**, chuỗi hỏng trả `false`.
- `IAuthService.LoginAsync`: email chuẩn hóa (trim, chữ thường); không có tài khoản / mật khẩu rỗng / sai → **cùng** thông báo `"Email hoặc mật khẩu không đúng."`; đúng nhưng bị khóa → thông báo khóa; đúng nhưng `Pending` → thông báo chờ cấp quyền. `GetActiveRoleAsync`: tên vai trò nếu đang hoạt động và **không** `Pending`, ngược lại `null`.
- `IAccountService`:
  - `CreateAsync`: họ tên không rỗng → email hợp lệ → mật khẩu ≥ 6 → vai trò hợp lệ (`Enum.IsDefined`) → email chưa dùng (không phân biệt hoa thường). Lưu email chuẩn hóa, mật khẩu băm, `IsActive = true`.
  - **`RegisterAsync(RegisterRequest(FullName, Email, Password))`**: như `CreateAsync` với vai trò `Pending`.
  - `UpdateAsync`: không tự đổi vai trò của mình; không hạ Admin cuối cùng đang hoạt động; giảng viên đang có phân công không đổi vai trò được. (Đổi `Pending` → vai khác chính là **cấp quyền**.)
  - `SetActiveAsync` (lũy đẳng; không tự khóa; không khóa Admin cuối), `ResetPasswordAsync`, `SearchAsync`, `GetByIdAsync`.
- `IAssignmentService`: `AssignAsync` (giảng viên tồn tại → là Giảng viên → đang hoạt động → môn tồn tại → môn đang mở → chưa phân công), `UnassignAsync`, **`CanAccessSubjectAsync`** (bị khóa/không có → false; Admin → true; Giảng viên → chỉ môn được phân công; vai khác → false).
- `ISubjectService`: `GetAllAsync`, `GetByLecturerAsync`, `GetDetailAsync` (giảng viên đã phân công + giảng viên có thể chọn).
- `ILanguageConfigService`: `GetForEditAsync` (NotFound/Forbidden), `UpdateAsync` (ngôn ngữ hợp lệ → môn tồn tại → **quyền** → môn đang mở; ghi `LanguageUpdatedAt/By`), `GetSpeechConfigAsync`.
- `QuestionAllocator.Allocate(pool, students, perStudent, rng)`: mỗi sinh viên đủ câu, không lặp trong bộ; hai sinh viên **liền nhau không chung câu** khi `pool ≥ 2 × perStudent`; dùng đều; ngân hàng nhỏ thì buộc dùng lại; dùng đúng `rng`. Gợi ý: xếp hạng câu theo (không thuộc bộ người trước) → (ít được giao nhất) → ngẫu nhiên, lấy đầu bảng.
- `IQuestionService`: `GetBySubjectAsync` (Forbidden nếu không có quyền), `AddAsync` (nội dung ≤ 1000, ý chính ≤ 1000, rỗng → null), `SetActiveAsync` (quyền theo môn của câu).
- `IExamService.CreateAsync`: tên ≤ 150 → giờ bắt đầu **không ở quá khứ** (lệch 1 phút) → các số trong khoảng như `CHECK` → danh sách sinh viên (mỗi dòng `MSSV; Họ tên`, dấu ngăn `;` `,` tab hoặc khoảng trắng, mã ≤ 30 không trùng, tên ≤ 100, 1..200 người) → môn tồn tại → quyền → môn mở → đủ câu đang dùng. Khung giờ người thứ *i* = `StartTime + i × SlotMinutes`. Lưu cả đồ thị một lần. Các hàm khác: `GetListAsync`, `GetSubjectsForExamAsync`, `GetDetailAsync` (thí sinh theo giờ), `SetOpenAsync`, `ReallocateAsync` (chỉ khi mọi thí sinh còn `Scheduled`).

### A3.3 Giao diện Razor Pages

Quy tắc chung: PageModel chỉ gọi interface Business; mọi form POST có antiforgery; Post-Redirect-Get với `TempData["Success"/"Error"]` hiện bằng `_Alerts`; lỗi `Validation` hiện trên form (giữ các ô đã nhập), `NotFound` → 404, `Forbidden` → trang Không có quyền; Bootstrap 5; tiêu đề `Tên trang - AIVES`; câu thông báo kiểm tra dữ liệu thống nhất: `Nhập email.` · `Email không hợp lệ.` · `Nhập mật khẩu.` · `Nhập họ tên.` · `Mật khẩu từ 6 đến 100 ký tự.` · `Nhập lại mật khẩu.` · `Mật khẩu nhập lại không khớp.`

| Trang | Ai vào | Người làm | Nội dung chính |
|---|---|---|---|
| `/Auth/Login` | mọi người | TV4 | Email, Mật khẩu, Ghi nhớ; link **Tạo tài khoản**; tài khoản mẫu bên dưới |
| `/Auth/Register` | mọi người | TV4 | Họ tên, Email, Mật khẩu, Nhập lại; thành công → về Login, *"Đã tạo tài khoản. Bạn đăng nhập được sau khi quản trị viên cấp vai trò."* |
| `/Auth/Logout` (POST), `/Auth/AccessDenied` | | TV4 | |
| `/` | đã đăng nhập | TV6 | Admin → `/Subjects`, Giảng viên → `/MySubjects` |
| `/Accounts`, `/Accounts/Create`, `/Accounts/Edit/{id}` | Admin | TV4 | lọc (có "Chờ cấp quyền"), huy hiệu vàng, vùng `account-list`/`accounts`; Create không có `Pending`; Edit = cấp vai trò, khung đặt lại mật khẩu |
| `/Subjects`, `/Subjects/Details/{id}` | Admin | TV5 | phân công/gỡ, khung ngôn ngữ, khung ngân hàng câu hỏi |
| `/MySubjects` | Giảng viên | TV5 | nút Đổi ngôn ngữ, Câu hỏi |
| `/Language/Edit?subjectId=` | Admin, Giảng viên | TV5 | Service quyết định quyền (điểm nhấn demo) |
| `/Exams` | Admin, Giảng viên | TV6 | vùng `exam-list`/`exams` |
| `/Exams/Create` | Admin, Giảng viên | TV6 | `datetime-local` có `min` = bây giờ, mặc định sáng mai 8:00; các ô số có mặc định 10/3/4/2/60; ô danh sách sinh viên |
| `/Exams/Details/{id}` | Admin, Giảng viên | TV6 | 4 ô thông tin, bảng lịch thi + bộ câu hỏi + trạng thái, Chọn lại (khi mọi người chưa thi), Đóng/Mở; vùng `exam-detail`/`exam-{id}`. Ass2 **không** có nút bắt đầu thi |
| `/Exams/Questions?subjectId=` | Admin, Giảng viên | TV5 | bảng câu hỏi Tắt/Bật, form Thêm |

### A3.4 SignalR (Ass2)

| Thành phần | Hợp đồng |
|---|---|
| Hub | `LiveHub`, đường dẫn `/hubs/live`, `[Authorize]`; không có hàm cho client gọi |
| Sự kiện | `changed(topic: string, message: string)` |
| Gửi từ | PageModel, **sau** khi Service trả thành công: `await _live.NotifyAsync(message, topics...)` (inject `IHubContext<LiveHub>`) |
| Nhận | `live.js`: vùng `<div id="..." data-live="topic">` được thay bằng bản mới tải; hiện thông báo |

| Topic | Gửi khi | Vùng nhận |
|---|---|---|
| `accounts` | có người đăng ký (Register) | `/Accounts`: `account-list` |
| `exams` | tạo phiên thi, đóng/mở | `/Exams`: `exam-list` |
| `exam-{id}` | đóng/mở, chọn lại bộ câu hỏi | `/Exams/Details/{id}`: `exam-detail` |

## A4. Kiểm thử

### A4.1 Test tự động `AIVES.Tests` (124, không cần SQL Server)

| Lớp test | Số | Người | Lớp test | Số | Người |
|---|---|---|---|---|---|
| `PasswordHasherTests` | 9 | TV3 | `SubjectServiceTests` | 4 | TV3 |
| `AuthServiceTests` | 9 | TV3 | `LanguageConfigServiceTests` | 6 | TV3 |
| `AccountServiceTests` | 30 | TV3 | `ExamServiceTests` | 41 | TV3 |
| `AssignmentServiceTests` | 10 | TV3 | `QuestionAllocatorTests` | 6 | TV5 |
| | | | `QuestionServiceTests` | 9 | TV5 |

Test dùng kho dữ liệu giả trong bộ nhớ (`AIVES.Tests/Support`) và bộ băm giả, nên TV3 và TV5 làm được từ ngày đầu.

### A4.2 Test SQL `AIVES.Tests.Data` (44, cần database test + biến `AIVES_TEST_CONNECTION`)

`SchemaTests` 24 (Tien: script khớp hợp đồng) · `AccountRepositoryTests` 6 · `SubjectRepositoryTests` 5 · `LecturerSubjectRepositoryTests` 2 · `QuestionRepositoryTests` 2 · `ExamRepositoryTests` 5 (TV2).

### A4.3 Checklist thử tay (người viết tick, người duyệt bấm lại)

**A. Đăng nhập (TV4, TV6)**
- [ ] Vào `/` chưa đăng nhập → về trang đăng nhập, URL có `ReturnUrl`.
- [ ] Để trống hai ô → `Nhập email.`, `Nhập mật khẩu.` (ngay trên trình duyệt). `abc` → `Email không hợp lệ.`
- [ ] Sai mật khẩu và email không tồn tại → **cùng** thông báo.
- [ ] `admin@fu.edu.vn` → trang Môn học, thanh điều hướng có "Tài khoản"; `an.nv@fu.edu.vn` → "Môn của tôi", không có "Tài khoản".
- [ ] `ReturnUrl=https://example.com` → **không** bị chuyển ra ngoài; `ReturnUrl=/Accounts` (admin) → vào đúng `/Accounts`.
- [ ] Đăng xuất, bấm Back rồi tải lại → vẫn phải đăng nhập. Giảng viên gõ `/Accounts` → Không có quyền.
- [ ] **Khóa có hiệu lực ngay:** hai trình duyệt; admin khóa giảng viên; giảng viên tải lại → bị đưa về đăng nhập.

**R. Tự đăng ký và cấp vai trò (TV4, TV3)**
- [ ] Trang đăng nhập có link "Tạo tài khoản"; đã đăng nhập mà mở `/Auth/Register` → về trang chính.
- [ ] Để trống / mật khẩu `123` / nhập lại khác → đúng các thông báo chuẩn.
- [ ] Đăng ký bằng email đã có (kể cả viết hoa) → báo trùng, giữ các ô đã nhập.
- [ ] Đăng ký hợp lệ → về trang đăng nhập với thông báo chờ cấp quyền.
- [ ] Đăng nhập tài khoản vừa tạo đúng mật khẩu → báo *đang chờ quản trị viên cấp vai trò*; sai mật khẩu → thông báo chung.
- [ ] `moi.dangky@fu.edu.vn / Lecturer@123` (mẫu) → cũng bị chặn như trên.
- [ ] Admin: Tài khoản → dòng mới có huy hiệu vàng "Chờ cấp quyền"; lọc "Chờ cấp quyền" chỉ ra các tài khoản đó.
- [ ] Trang Tạo tài khoản **không** có lựa chọn "Chờ cấp quyền".
- [ ] Admin Sửa → chọn Giảng viên → lưu → người đó đăng nhập được, vào "Môn của tôi".
- [ ] Admin đổi một giảng viên đang đăng nhập (không có phân công) về "Chờ cấp quyền" → giảng viên tải lại trang → bị đăng xuất.

**B. Tài khoản (TV4)**
- [ ] Danh sách đủ cột; tìm `an` lọc đúng; "Xóa lọc" trở lại đủ.
- [ ] Tạo hợp lệ `chau.lm@fu.edu.vn` → `Đã tạo tài khoản chau.lm@fu.edu.vn.`; tạo lại trùng → báo, giữ ô.
- [ ] Sửa họ tên → `Đã lưu thay đổi tài khoản.`; đổi sang email người khác → báo trùng.
- [ ] Admin tự đổi vai trò mình → bị chặn; hạ admin cuối → bị chặn; giảng viên có phân công đổi vai trò → bị chặn.
- [ ] Khóa/Mở khóa người khác đúng huy hiệu và thông báo; tự khóa mình → báo lỗi.
- [ ] Đặt lại mật khẩu < 6 → báo; hợp lệ → mật khẩu mới dùng được, cũ thì không. `/Accounts/Edit/9999` → 404.

**C. Môn học và phân công (TV5)**
- [ ] 5 môn; `PRN211` mờ + "Đã ngừng"; số giảng viên đúng.
- [ ] Chi tiết PRN222: an.nv có trong bảng; ô chọn **không** có an.nv, admin, tài khoản khóa, tài khoản Chờ cấp quyền.
- [ ] Bấm Phân công khi chưa chọn → `Chọn giảng viên cần phân công.`; phân công binh.tt vào SWP391 → thành công; phân công vào PRN211 → báo môn ngừng; Gỡ → `Đã gỡ phân công.`
- [ ] Giảng viên `/MySubjects` chỉ thấy môn mình; gõ `/Subjects` → Không có quyền.

**D. Ngôn ngữ (TV5)**
- [ ] Admin đổi STT môn sang English → về chi tiết môn, thông báo, danh sách hiện `en-US`; dòng "Lần sửa gần nhất ... bởi ...".
- [ ] **Điểm nhấn:** an.nv mở `/Language/Edit?subjectId=<SWP391>` → Không có quyền (GET và POST).

**E. Phiên thi và câu hỏi (TV5, TV6)**
- [ ] `/Exams` trống → "Chưa có phiên thi nào..."; trang Tạo: chỉ môn đang mở được phép; mặc định sáng mai 8:00; chọn ngày hôm qua → trình duyệt không cho.
- [ ] Khung thời gian 0 hoặc 121 → báo (tắt kiểm tra trình duyệt bằng DevTools: **Service vẫn chặn**).
- [ ] Danh sách `SE1` thiếu tên → báo dòng 1; hai dòng `SE1` và `se1` → báo trùng; trống → báo.
- [ ] Chọn 3 câu chính khi ngân hàng chỉ còn 2 câu đang dùng → báo thiếu câu.
- [ ] Tạo hợp lệ 3 sinh viên, 10 phút → khung 8:00-8:10, 8:10-8:20, 8:20-8:30; **hai người liền nhau không trùng câu**.
- [ ] Chọn lại bộ câu hỏi → bộ đổi, vẫn không trùng liền nhau. Đóng/Mở đổi huy hiệu.
- [ ] Giảng viên mở `/Exams/Details/<phiên môn khác>` → bị chặn.
- [ ] Ngân hàng câu hỏi: thêm (nội dung + `a;b`), để trống → báo, tắt câu → mờ + "Đã tắt"; phiên mới không chọn câu đã tắt; môn không được phân công → bị chặn.

**S. SignalR (Tien, TV4, TV6)**
- [ ] DevTools → Network → WS: trang Tài khoản/Phiên thi có kết nối `/hubs/live`; trang đăng nhập **không** có.
- [ ] Mở `/hubs/live/negotiate` khi chưa đăng nhập → bị từ chối (chuyển về đăng nhập).
- [ ] Trình duyệt 1 (admin) mở Tài khoản; trình duyệt 2 đăng ký tài khoản → trình duyệt 1 **không tải lại** mà hiện dòng mới + thông báo nhỏ.
- [ ] Trình duyệt 1 mở Phiên thi; trình duyệt 2 tạo phiên → trình duyệt 1 tự hiện phiên mới.
- [ ] Trình duyệt 1 mở Chi tiết phiên X; trình duyệt 2 đóng phiên X / chọn lại câu hỏi → trình duyệt 1 tự đổi huy hiệu / bộ câu hỏi; nút trên trang mới vẫn bấm được (token chống giả mạo còn đúng).
- [ ] Tắt mạng 10 giây rồi bật lại → tự kết nối lại (`withAutomaticReconnect`), sự kiện sau đó vẫn tới.
- [ ] Giảng viên (không phải admin) mở trang Phiên thi khi có người đăng ký → **không** hiện gì (trang không có vùng `accounts`).

---

# PHẦN B: ASS3 (MVC + Web API, chức năng 7 + 2 + 3, 3 lớp, SignalR)

**Đề:** một ứng dụng **ASP.NET Core MVC** chứa đủ chức năng 7 và 2 (viết lại từ Razor Pages sang MVC) và **chức năng 3: lõi phỏng vấn AI**, kèm **Web API** (Swagger) cho cả 3 chức năng, trong đó API chức năng 3 cho phép bắt đầu thi, nộp câu trả lời và nhận **câu hỏi xoáy do AI sinh ra**, đọc câu hỏi, xem biên bản. Chức năng 3: giám khảo AI đọc câu hỏi bằng giọng nói (TTS), sinh viên trả lời bằng giọng nói được chuyển thành văn bản gần thời gian thực (STT), AI tự sinh câu hỏi xoáy khi câu trả lời mơ hồ, thiếu ý hoặc mâu thuẫn; giới hạn thời gian trả lời mỗi câu và số lượt hỏi xoáy. Thêm vai trò **Sinh viên** (đăng nhập, vào phòng thi của chính mình), giọng đọc chạy cục bộ **VieNeu-TTS**, và SignalR để giảng viên **theo dõi trực tiếp** tiến độ thí sinh.

### Ass3 dùng đồng thời: Web API + VieNeu-TTS + AI (DeepSeek)

| Thành phần | Dùng ở đâu | Người làm |
|---|---|---|
| **Web API** (`/api/...`, Swagger) | cả 3 chức năng; riêng chức năng 3: `/api/interviews` (bắt đầu, nộp câu trả lời nhận câu hỏi xoáy, giọng đọc, biên bản) | TV4, TV5, TV6 |
| **VieNeu-TTS** (chạy cục bộ, `http://localhost:8000`) | đọc câu hỏi trong **phòng thi MVC** (`/Interview/Speak`) **và qua API** (`GET /api/interviews/turns/{id}/speech`); trang Giọng đọc (clone, chọn giọng theo môn); tự khởi động cùng ứng dụng | TV2 (TV6 phát âm thanh) |
| **AI DeepSeek** (khóa trong `user-secrets`) | quyết định và viết **câu hỏi xoáy** sau mỗi câu trả lời, cho cả phòng thi lẫn API; lỗi hoặc không có khóa thì dùng luật dự phòng | TV6 |

VieNeu-TTS không chạy thì phòng thi đọc bằng giọng trình duyệt và API giọng đọc trả **503**; buổi thi không bao giờ bị kẹt vì giọng đọc hay AI.

### Ghi lại các thay đổi của Ass3 (so với bản trước của file này)

| Thay đổi | Ảnh hưởng tới việc |
|---|---|
| Ass3 là **MVC + Web API** (không phải chỉ MVC): giữ đủ API cho chức năng 7, 2 và thêm API chức năng 3 | TV4 4.4, TV5 5.4, TV6 6.1 (Swagger, 401/403 cho `/api`), TV6 6.4 |
| Thêm `POST /api/auth/register` (tự đăng ký qua API, vai trò Chờ cấp quyền) | TV4 4.4 |
| Thêm `/api/interviews`: `my`, `{participantId}/start`, `turns/{turnId}/answer`, `turns/{turnId}/speech` (VieNeu), `{participantId}/transcript` | TV6 6.4 |
| `InterviewStateDto` có thêm `SessionId` (để báo SignalR `exam-{id}` khi thí sinh thi) | TV3 3.3 (đã có test), TV6 |
| Giao diện F7 và F2 viết lại bằng **MVC** (`AccountsController` có ô MSSV, `ExamsController` gồm cả ngân hàng câu hỏi); trang `Interview/Index`, `Interview/Session` cũ bỏ, vào phòng thi từ `/Exams/Details/{id}` | TV4 4.2, 4.3; TV6 6.3 |
| File `ass3.http` trong gói: 78 lời gọi có kết quả mong đợi (thêm mục 7 đăng ký, mục 8 phỏng vấn AI) | mọi người làm API |
| Database và SignalR của Tien đã có sẵn trong gói | TV2, TV4, TV6 dùng ngay |
| `VieNeuLauncher` chạy `uv run` (bỏ `--no-sync`) và cảnh báo khi VieNeu tự thoát; lý do: chuyển thư mục làm VieNeu chết ngay mà log không báo gì | TV2 2.5, checklist H |

## B1. Kế hoạch

### B1.1 Bảng việc

| Người | Việc | Test / kiểm tra |
|---|---|---|
| **Tien** | ~~1.1 script 01 + 02 và **03_ChucNang3.sql**~~ · ~~1.2 SignalR cho MVC~~ (**đã xong, có trong gói**) · 1.3 tích hợp, duyệt PR, demo | `SchemaTests` (32) · checklist S3 |
| **TV2** | 2.1 repository chức năng 7 + 2 (thêm 2 hàm sinh viên) · 2.2 `InterviewRepository`, `VoiceRepository`, DI · 2.3 cài VieNeu-TTS + `VieNeuTextToSpeech` · 2.4 `VoiceService` + trang Giọng đọc · 2.5 tự khởi động VieNeu | `*RepositoryTests` (31) · `VoiceServiceTests` (31) · checklist H |
| **TV3** | 3.1 service chức năng 7 (thêm luật Sinh viên) · 3.2 `ExamService` · 3.3 **`InterviewService`** | 10 + 9 + 34 + 11 + 4 + 8 + 41 + 32 test |
| **TV4** | 4.1 `AuthController` (Đăng nhập, Đăng ký...) + `HomeController` · 4.2 `AccountsController` (có MSSV) · 4.3 `ExamsController`: phiên thi + ngân hàng câu hỏi · 4.4 **API** `ApiControllerBase`, `/api/auth`, `/api/accounts` | checklist A, R3, B3, E3 · `ass3.http` mục 0, 1, 2, 7 |
| **TV5** | 5.1 `QuestionAllocator` + `QuestionService` · 5.2 `SubjectsController`, `MySubjectsController`, `LanguageController` · 5.3 trang **Bài thi của tôi** + view **Biên bản** · 5.4 **API** `/api/subjects`, `/api/exams`, câu hỏi | 6 + 9 test · checklist C, D, F · `ass3.http` mục 3 đến 6 |
| **TV6** | 6.1 `Program.cs` (MVC + API + Swagger) + DI Business + nền MVC + layout theo vai trò · 6.2 **AI hỏi xoáy** `FollowUpGenerator` · 6.3 **phòng thi**: `InterviewController`, view `Room`, `pcm-player.js`, báo SignalR · 6.4 **API chức năng 3** `/api/interviews` | `FollowUpGeneratorFallbackTests` (9) · checklist G · `ass3.http` mục 8 |

### B1.2 Ai chờ ai

```
Tien 1.1 (01, 02, 03) ──► TV2 2.1, 2.2 chạy Tests.Data
TV3 3.1 ──► TV3 3.2 ──► TV3 3.3 (InterviewService cần IFollowUpGenerator, IVoiceService: test đã có bản giả)
TV6 6.1 (Program.cs, AppController, _Layout) ──► TV4, TV5, TV2 2.4, TV6 6.3 làm trang
TV2 2.3 (VieNeu chạy) ──► TV2 2.4 ──► TV6 6.3 (đọc câu hỏi bằng VieNeu)
TV3 3.3 + TV6 6.2 + TV4 4.3 (nút Bắt đầu thi) ──► TV6 6.3 phòng thi: điểm nối cuối cùng
Tien 1.2 ──► TV4 4.2/4.3 (vùng live), TV6 6.3 (gửi exam-{id} khi thí sinh thi)
TV6 6.1 (Swagger, cookie trả 401/403 cho /api) + TV4 4.4 (ApiControllerBase) ──► TV5 5.4, TV6 6.4
```

### B1.3 Giai đoạn

| Giai đoạn | Việc | Sang giai đoạn sau khi |
|---|---|---|
| **A. Nền + chép lại phần cũ** (2 buổi) | (database và SignalR của Tien đã có sẵn) TV6 6.1; TV2 2.1; TV3 3.1, 3.2; TV5 5.1 (lấy từ Ass2 rồi sửa) | build sạch; **test chức năng 7 + 2 xanh** (132 test) |
| **B. Lõi chức năng 3** (3 buổi) | TV3 3.3; TV6 6.2; TV2 2.2, 2.3, 2.4 (phần service) | **204 test xanh**, 63 test SQL xanh; VieNeu chạy được bằng tay |
| **C. Giao diện MVC và API** (3 đến 4 buổi) | TV4 4.1 đến 4.4; TV5 5.2 đến 5.4; TV2 trang Giọng đọc + 2.5; TV6 6.3, 6.4; Tien 1.2 | checklist A, R3, B3, C, D, E3, F, G, H, S3 tick hết; **78 lời gọi `ass3.http` đúng** |
| **D. Tích hợp, demo** (1 đến 2 buổi) | kịch bản demo, sửa lỗi chéo, mini-task, tập bảo vệ | demo chạy trơn |

**Kịch bản demo Ass3:** mở `/swagger`, đăng nhập bằng `POST /api/auth/login`, tạo phiên thi bằng `POST /api/exams`, gọi `POST /api/interviews/{id}/start` rồi `.../answer` với câu trả lời mơ hồ để thấy **câu hỏi xoáy trả về trong JSON**. Sau đó demo giao diện: một bạn tự đăng ký → admin cấp vai trò **Sinh viên** kèm MSSV `SE180002` → giảng viên tạo phiên thi có `SE180002` → (trình duyệt giảng viên mở Chi tiết phiên thi) → sinh viên đăng nhập, **Bài thi của tôi → Vào phòng thi** → trang giảng viên **tự đổi** thí sinh sang "Đang thi" → AI đọc câu hỏi bằng giọng VieNeu, sinh viên trả lời mơ hồ bằng giọng nói → chữ hiện ngay khi nói → AI hỏi xoáy → để hết giờ một câu → thi xong → trang giảng viên tự đổi "Đã thi" → mở Biên bản. Thêm: chọn giọng riêng cho một môn, clone một giọng.

### B1.4 Định nghĩa xong Ass3

- [ ] `dotnet build` sạch; 204/204 test `AIVES.Tests`; 63/63 test `AIVES.Tests.Data` trên SQL Server thật.
- [ ] Checklist B4.3 tick đủ (phòng thi thử bằng **Chrome hoặc Edge** có micro).
- [ ] Chạy `ass3.http` từ trên xuống trên database mới: cả 78 lời gọi ra đúng **KẾT QUẢ MONG ĐỢI** (B4.4).
- [ ] Khóa API AI **không** nằm trong bất kỳ file nào của repo (`git log -p -S"sk-"` trống).
- [ ] Không còn `NotImplementedException`. Mỗi người làm mini-task và bảo vệ. Tag `ass3-final`.

## B2. Giao việc

### Tien: database, SignalR, tích hợp

> **Đã xong:** 1.1, 1.2 có sẵn trong gói (`Database/01..03`, `AIVES.Ass3.MVC/Infrastructure/LiveHub.cs`, `wwwroot/js/live.js`). TV6 gửi sự kiện `exam-{sessionId}` từ phòng thi và API phỏng vấn; TV4 gửi `accounts`, `exams`, `exam-{id}` từ Auth/Exams.

**1.1 Script** (1,5 buổi): chép `01`, `02` từ Ass2 rồi viết **`03_ChucNang3.sql`** (idempotent) theo **B3.1**: cột `Accounts.StudentCode` + chỉ mục unique có điều kiện, nới `CK_Accounts_Role` (thêm `'Student'`, giữ `'Pending'`), `Subjects.TtsVoice`, bảng `InterviewTurns`, `Voices`, sinh viên mẫu. Kiểm: `SchemaTests` (32).
**1.2 SignalR cho MVC** (1 buổi): đưa `LiveHub` + `live.js` sang project MVC (đổi namespace), `MapHub` do TV6 thêm. Thống nhất với TV6 topic `exam-{id}` gửi từ phòng thi (B3.4); kiểm cả luồng giảng viên theo dõi thí sinh.
**1.3 Tích hợp, demo** (suốt bài).

**Mini-task:** chỉ gửi sự kiện cho người có quyền xem phiên thi: dùng `Groups` (client gọi một hàm hub `Watch(examId)`, hub kiểm quyền bằng `IAssignmentService` rồi `Groups.AddToGroupAsync`). So sánh với cách gửi cho mọi người. **Câu hỏi bảo vệ:** (1) Vì sao `CK_Accounts_Role` phải nới bằng `DROP` rồi `ADD` và điều kiện `IF` của bạn đảm bảo chạy lại được thế nào? (2) Chỉ mục unique có điều kiện `WHERE StudentCode IS NOT NULL` giải quyết vấn đề gì mà `UNIQUE` thường không làm được? (3) Ai nhận được sự kiện `exam-{id}` và có rủi ro lộ dữ liệu không?

### TV2: DataAccess và giọng đọc cục bộ

**2.1** (1 buổi) chép 5 repository từ Ass2, thêm `AccountRepository.StudentCodeExistsAsync` và `ExamRepository.GetParticipantsByStudentCodeAsync` (kèm `Session.Subject`, sắp theo `SlotStart`, chỉ đọc).
**2.2** (1 buổi) `InterviewRepository` (**không lưu ngay**: `AddTurn` chỉ đánh dấu, `SaveAsync` lưu tất cả trong một giao dịch), `VoiceRepository` (`GetAllAsync` **không nạp** cột `Clip`; `RemoveAsync` xóa giọng và đặt `Subjects.TtsVoice = NULL` trong cùng một `SaveChanges`); thêm 2 dòng vào `AddDataAccessLayer`.
**2.3** (1,5 buổi) cài VieNeu-TTS (`git clone https://github.com/pnnbao97/VieNeu-TTS.git VieNeu-TTS`, `uv run python -m apps.openai_speech`, nghe ở `http://localhost:8000`; thêm `VieNeu-TTS/` vào `.gitignore`) và viết `Services/VieNeuTextToSpeech.cs` theo `ITextToSpeech`: `GET /health`, `GET /v1/voices`, `POST /v1/voices` (multipart, clone), `POST /v1/audio/speech` (PCM s16le 48 kHz, **streaming** bằng `HttpCompletionOption.ResponseHeadersRead`, không `Dispose` response trước khi luồng đọc xong). Mọi hàm **không ném lỗi** khi máy chủ tắt. Một `static HttpClient`.
**2.4** (2,5 buổi) `Services/VoiceService.cs` (Clone chỉ Admin: tên hợp lệ → mô tả → file ≤ 20 MB, đuôi `.wav .mp3 .flac .ogg .m4a` → tên chưa có → nạp vào VieNeu **trước** → rồi mới lưu DB; Delete; Overview gộp giọng có sẵn + giọng đã clone không trùng; `OpenSpeechAsync` nạp lại giọng clone khi VieNeu quên, rơi về giọng mặc định khi giọng hỏng). Trang **Giọng đọc** `VoicesController` (`Index`, `Preview`, `Clone`, `Delete`, `Assign`) + view.
**2.5** (1 buổi) `Infrastructure/VieNeuLauncher.cs` (`IHostedService`: VieNeu đang chạy thì dùng lại; không thì tìm thư mục `VieNeu-TTS` ngược lên các thư mục cha và chạy `uv run python -m apps.openai_speech`; thiếu `uv` chỉ cảnh báo log). **Hai điểm bắt buộc** (rút ra từ lỗi thật khi chạy thử): (a) **không** dùng `uv run --no-sync`: nếu thư mục `VieNeu-TTS` bị chuyển chỗ hoặc đổi máy, gói `vieneu` trong `.venv` vẫn trỏ về đường dẫn cũ và VieNeu chết ngay với `ModuleNotFoundError`; `uv run` tự sửa việc này và gần như tức thì khi môi trường đã đúng; (b) VieNeu **tự thoát** thì ghi `LogWarning` kèm mã thoát và dòng lỗi cuối (đầu ra thường chỉ ghi ở mức Debug nên lỗi bị giấu), trừ khi chính ứng dụng đang tắt + `ChildProcessJob.cs` (Windows Job Object `KILL_ON_JOB_CLOSE` để VieNeu chết theo khi ứng dụng bị ép tắt). Cấu hình `Tts` trong `appsettings.json`: `BaseUrl`, `Model`, `Voice`, `AutoStart`, `ServerPath`, `UseGpu`.

**Mini-task:** thêm `CancellationToken` hạn 10 giây cho `GetStatusAsync` và mô tả cách thử VieNeu "treo". **Câu hỏi bảo vệ:** (1) Vì sao `AddTurn` không lưu ngay? (2) VieNeu khởi động lại và quên giọng clone: code nào giúp vẫn đọc được? (3) Vì sao không `Dispose` `HttpResponseMessage` ngay trong `OpenSpeechAsync`? (4) `GetAllAsync` của giọng vì sao không nạp `Clip`?

### TV3: Business (luật) và máy trạng thái phỏng vấn

**3.1** (1,5 buổi) chép các service chức năng 7 từ Ass2 rồi thêm luật **Sinh viên**: tạo/sửa tài khoản vai `Student` thì **bắt buộc** mã sinh viên (trim, ≤ 30, không trùng tài khoản khác); vai khác thì mã lưu `null`. `CanAccessSubjectAsync`: sinh viên → `false`. `LanguageConfigService.UpdateAsync` thêm `TtsVoice` (≤ 64; `null` = giữ nguyên, rỗng = về mặc định, có tên = đổi). `Mapper` đưa thêm `StudentCode`, `TtsVoice` vào DTO. Test: `PasswordHasherTests` (10, có mẫu sinh viên), `AuthServiceTests` (9), `AccountServiceTests` (34), `AssignmentServiceTests` (11), `SubjectServiceTests` (4), `LanguageConfigServiceTests` (8).
**3.2** (0,5 buổi) chép `ExamService` từ Ass2. Test: `ExamServiceTests` (41).
**3.3** (3 buổi) **`InterviewService`**, việc khó và đáng giá nhất. Đặc tả ở **B3.2**. Vẽ sơ đồ trạng thái thí sinh (`Scheduled → InProgress → Completed`) và của một câu chính (có/không hỏi xoáy) ra giấy trước. Làm theo nhóm test: `Start_*` → `Access_*` → `Submit_*` → hỏi xoáy → giới hạn → hết giờ → `GetMyInterviews`, `Transcript`, `Speak`. Test: `InterviewServiceTests` (32).

**Mini-task:** viết thêm 5 test cho tình huống bộ test chưa phủ (ví dụ: sinh viên bị khóa giữa buổi thi). **Câu hỏi bảo vệ:** (1) Vì sao server tự kiểm hết giờ (`AnswerSeconds + 15`) mà không tin cờ `timedOut` của trình duyệt? (2) Vì sao `SubmitAnswerAsync` lưu một lần? (3) `SpeakAsync` vì sao chỉ đọc nội dung câu hỏi **đã lưu** mà không nhận văn bản tùy ý? (4) `SecondsLeft` được kẹp trong `[0, AnswerSeconds]` để làm gì?

### TV4: Đăng nhập, Tài khoản, Phiên thi (MVC)

**4.1** (1,5 buổi) `AuthController` (`Login`, `Register`, `Logout`, `AccessDenied`) + `ViewModels/LoginViewModel.cs` (`LoginViewModel`, `RegisterViewModel`) + views; Register gửi SignalR `accounts`. `HomeController.Index`: Sinh viên → `/MyExams`, còn lại → `/Exams`. Dưới form đăng nhập ghi thêm tài khoản sinh viên mẫu.
**4.2** (2 buổi) `AccountsController` (`Index`, `Create`, `Edit`, `SetActive`, `ResetPassword`) + `AccountViewModels` có thêm ô **Mã sinh viên (MSSV)** ở Tạo và Sửa (gợi ý: chỉ dùng khi vai trò là Sinh viên); danh sách hiện MSSV cạnh vai trò; vùng `account-list`/`accounts`.
**4.3** (3 buổi) `ExamsController` (`Index`, `Create`, `Details`, `SetOpen`, `Reallocate`, `Questions`, `AddQuestion`, `SetQuestionActive`) + `ViewModels/ExamViewModels.cs` + views: viết lại 4 trang Razor Pages của Ass2 bằng MVC. Trang Chi tiết thêm cột nút **Bắt đầu thi / Tiếp tục thi** (phiên mở, chưa thi xong) và **Biên bản** (đã bắt đầu) trỏ tới `InterviewController` của TV6. Gửi SignalR như Ass2.
- Học trước: so sánh PageModel và Controller (handler `OnPostXxx` ↔ action `[HttpPost]`), model binding với prefix (`NewQuestion.Content` → tham số `newQuestion`), `[ValidateNever]` cho danh sách chọn.

**4.4 Web API: nền + đăng nhập + tài khoản** (2 buổi, làm **sớm** vì TV5 và TV6 dùng lớp cha của bạn). `Controllers/Api/ApiControllerBase.cs`: `[ApiController]`, `[Authorize]`, `[Produces("application/json")]`, thuộc tính `CurrentUserId`, hàm `Respond(ServiceResult)` (thành công → 204) và `Respond<T>(ServiceResult<T>)` (thành công → 200 + dữ liệu); lỗi → 400 / 404 / 403 với thân `{ "error": "..." }`. `AuthApiController` (`/api/auth`: `login` đặt cookie, sai → 401; `logout`; `me`; **`register`** → 201 + báo SignalR `accounts`). `AccountsApiController` (`/api/accounts`, chỉ Admin). Hợp đồng ở **B3.5**, kiểm bằng `ass3.http` mục 0, 1, 2, 7.
- Học trước: `ControllerBase` khác `Controller` ở đâu, attribute routing, `[FromQuery]`/thân JSON, mã trạng thái REST (200/201/204/400/401/403/404), Swagger.

**Mini-task:** viết đoạn so sánh (nửa trang, đưa vào PR) giữa trang Tạo phiên thi bản Razor Pages (Ass2) và bản MVC (Ass3): file nào, dòng nào tương ứng, bản nào dễ test hơn. **Câu hỏi bảo vệ:** (1) MVC khác Razor Pages ở đâu khi một trang có nhiều nút POST? (2) Vì sao ô MSSV không đặt `[Required]` ở ViewModel mà để Service kiểm?

### TV5: Câu hỏi, Môn học, Bài thi của tôi, Biên bản

**5.1** (0,5 buổi) chép `QuestionAllocator`, `QuestionService` từ Ass2. Test: 6 + 9.
**5.2** (2 buổi) `SubjectsController` (`Index`, `Details`, `Assign`, `Unassign`), `MySubjectsController`, `LanguageController` (`Edit` GET/POST) + views; nút **Câu hỏi** / khung **Ngân hàng câu hỏi** trỏ tới `/Exams/Questions?subjectId=`.
**5.3** (2 buổi) `MyExamsController` (chỉ Sinh viên, gọi `IInterviewService.GetMyInterviewsAsync`) + view: bảng Phiên thi, Môn, Khung giờ, Trạng thái (Sẵn sàng / Đang thi / Đã thi / Phiên thi đã đóng), nút **Vào phòng thi / Tiếp tục thi**; chưa có phiên nào: *"Chưa có phiên thi nào có tên bạn..."*. View **`Views/Interview/Transcript.cshtml`** (model `TranscriptDto`): mỗi lượt một thẻ, nhãn Câu chính / **Hỏi xoáy** (thụt vào), **Hết giờ**, giờ hỏi, "Lý do hỏi thêm", câu trả lời (rỗng: "(không trả lời)", chưa trả lời: "(chưa trả lời)"); link quay về `/Exams/Details/{SessionId}`.

**5.4 Web API: môn học, phiên thi, câu hỏi** (2 buổi): `SubjectsApiController` (`/api/subjects`: danh sách theo vai trò, chi tiết, phân công/gỡ, ngôn ngữ GET/PUT có `ttsVoice`, `speech`) và `ExamsApiController` (`/api/exams`: danh sách, môn được tạo, tạo → 201 `{ "id" }`, chi tiết, `status`, `reallocate`; `/api/subjects/{id}/questions` GET/POST; `/api/questions/{id}/active`). Kế thừa `ApiControllerBase` của TV4. Kiểm bằng `ass3.http` mục 3 đến 6.

**Mini-task:** nút "Tải biên bản" xuất file `.txt` (một action trả `File(...)`). **Câu hỏi bảo vệ:** (1) Sinh viên A gõ tay địa chỉ phòng thi của sinh viên B: chặn ở đâu? (2) Vì sao `MyExams` lọc theo **mã sinh viên của tài khoản** chứ không theo tên?

### TV6: Khung ứng dụng, AI hỏi xoáy, phòng thi

**6.1** (1,5 buổi, **làm đầu tiên**):
- `AddBusinessLayer(this IServiceCollection, string connectionString, AiOptions? ai = null, TtsOptions? tts = null)`: thêm `AiOptions`, `TtsOptions` (Singleton), `ITextToSpeech`, `IFollowUpGenerator` (Singleton), `IInterviewService`, `IVoiceService` (Scoped) vào phần của Ass2.
- `Program.cs`: `AddControllersWithViews()`, `AddSignalR()`, `AddBusinessLayer(cs, Configuration.GetSection("Ai").Get<AiOptions>(), GetSection("Tts").Get<TtsOptions>())`, `AddHostedService<VieNeuLauncher>()`, cookie `.AIVES.Ass3` + `OnValidatePrincipal`, middleware đúng thứ tự, `MapControllerRoute`, `MapHub<LiveHub>(LiveHub.Path)`.
- Web API trong `Program.cs`: `AddJsonOptions` với `JsonStringEnumConverter` (enum ra chữ: `"Admin"`), `AddEndpointsApiExplorer` + `AddSwaggerGen` (gói `Swashbuckle.AspNetCore` đã có trong `.csproj`), `UseSwagger` + `UseSwaggerUI`; cookie: `OnRedirectToLogin` / `OnRedirectToAccessDenied` trả **401 / 403** cho đường dẫn bắt đầu bằng `/api` (trang web vẫn chuyển hướng như cũ).
- `AppController` (`CurrentUserId`, `FlashSuccess/Error`, `MapFailure`), `ClaimsPrincipalExtensions`, `EnumExtensions`; `_Layout` điều hướng theo vai trò (nhân sự có thêm link **API** tới `/swagger`): Sinh viên: *Bài thi của tôi*; Admin: *Môn học và phân công, Tài khoản, Phiên thi, Giọng đọc*; Giảng viên: *Môn của tôi, Phiên thi, Giọng đọc*; nhãn vai trò; nạp SignalR khi đã đăng nhập.
**6.2** (2,5 buổi) `Services/FollowUpGenerator.cs`: phần **luật dự phòng** (có test: câu trả lời cuối dưới 12 từ → hỏi giải thích rõ hơn; ngược lại tìm ý chính đầu tiên chưa nhắc tới, so khớp không phân biệt hoa thường **và dấu**, bỏ qua ý đã hỏi lại; đủ ý → không hỏi; tiếng Anh không có dấu tiếng Việt) và phần **DeepSeek** khi có `Ai:ApiKey` (`POST https://api.deepseek.com/chat/completions`, model `deepseek-chat`, `response_format: json_object`, khuôn `{"ask","question","reason"}`, `static HttpClient` 20 giây; **mọi lỗi** rơi về luật dự phòng). Khóa lưu bằng `dotnet user-secrets set "Ai:ApiKey" "<khóa>" --project AIVES.Ass3.MVC`. Test: `FollowUpGeneratorFallbackTests` (9).
**6.3** (4 buổi) **Phòng thi**: `InterviewController` (`Room`, `Transcript` GET; `Start`, `Answer`, `Speak` POST, token chống giả mạo qua tiêu đề `RequestVerificationToken`), view `Views/Interview/Room.cshtml` + JavaScript (Web Speech API: `SpeechSynthesis`, `SpeechRecognition`), `wwwroot/js/pcm-player.js` (phát PCM theo luồng bằng Web Audio). Sau mỗi `Start`/`Answer` thành công gửi SignalR `exam-{SessionId}` (`"<tên> đang thi."` / `"<tên> đã thi xong."`). Luồng ở **B3.3**.

**6.4 API chức năng 3** (1 buổi): `InterviewsApiController` (`/api/interviews`) gọi **cùng** `IInterviewService` với phòng thi MVC, nên mọi luật giữ nguyên: `GET my` (chỉ Sinh viên), `POST {participantId}/start`, `POST turns/{turnId}/answer` (thân `{ "answer", "timedOut" }`, trả trạng thái mới: **câu hỏi xoáy** `kind = "FollowUp"` kèm vị trí `followUpIndex/followUpMax`, hoặc câu chính kế, hoặc `finished`), `GET turns/{turnId}/speech` (`audio/pcm` hoặc **503**), `GET {participantId}/transcript`. Start/answer thành công thì gửi SignalR `exam-{sessionId}` như phòng thi. Kiểm bằng `ass3.http` mục 8.

**Mini-task:** so sánh 3 phiên bản system prompt trên 5 câu trả lời mẫu (bảng 3 × 5, chọn prompt tốt nhất kèm lý do, đưa vào PR). **Câu hỏi bảo vệ:** (1) Kể hành trình một câu trả lời bằng giọng nói tới khi ra câu hỏi xoáy. (2) Prompt injection là gì, hệ thống phòng thủ thế nào? (3) Vì sao phát âm thanh theo luồng tốt hơn đợi cả file, và điều gì khó nhất khi ghép các mẩu PCM? (4) AI sập giữa buổi thi thì sinh viên đang thi gặp gì?

## B3. Hợp đồng

### B3.1 Database

`01` và `02`: **giống hệt Ass2** (A3.1). `03_ChucNang3.sql` (idempotent):

- `Accounts.StudentCode` VARCHAR(30) NULL + `SET QUOTED_IDENTIFIER ON` + chỉ mục `UQ_Accounts_StudentCode` UNIQUE `WHERE StudentCode IS NOT NULL`.
- `CK_Accounts_Role` nới thành `'Admin','Lecturer','Student','Pending'` (drop rồi add, chỉ khi định nghĩa cũ thiếu `Student` hoặc `Pending`).
- `Subjects.TtsVoice` NVARCHAR(64) NULL (giọng riêng của môn, NULL = mặc định).
- `InterviewTurns`: `Id` `PK_InterviewTurns` (thứ tự lượt = thứ tự `Id`) · `ParticipantId` `FK_InterviewTurns_Participants` CASCADE · `QuestionId` `FK_InterviewTurns_Questions` (câu **chính** của lượt) · `Kind` VARCHAR(10) `CK_InterviewTurns_Kind` ∈ `'Main','FollowUp'` · `Content` NVARCHAR(1000) · `Reason` NVARCHAR(500) NULL · `Answer` NVARCHAR(MAX) NULL · `AskedAt` DATETIME2 (UTC) · `AnsweredAt` DATETIME2 NULL · `TimedOut` BIT `DF_InterviewTurns_TimedOut` = 0.
- `Voices`: `Id` `PK_Voices` · `Name` NVARCHAR(64) `UQ_Voices_Name` · `Description` NVARCHAR(64) NULL · `FileName` VARCHAR(100) · `Denoise` BIT `DF_Voices_Denoise` = 1 · `Clip` VARBINARY(MAX) · `CreatedBy` `FK_Voices_Accounts` · `CreatedAt` `DF_Voices_CreatedAt`.
- Sinh viên mẫu: `sv.a@fu.edu.vn` / `Student@123`, `Nguyễn Văn A`, vai `Student`, `StudentCode = 'SE180001'`, hash `100000.bhe8jteDTPLA1QMnp8+N/A==.vrsScOF3bxc5hi9vlXdNRj3fBVRaFv7+oI0GLYMN73U=`.

### B3.2 Tầng Business và DataAccess (phần thêm so với Ass2)

**Repository thêm:** `IAccountRepository.StudentCodeExistsAsync(code, excludeId)` · `IExamRepository.GetParticipantsByStudentCodeAsync(code)` · `IInterviewRepository`: `GetParticipantAsync` (nạp `Session.Subject`, `ParticipantQuestions.Question`, theo dõi), `GetTurnAsync`, `GetTurnsAsync` (sắp theo `Id`), `HasTurnsAsync`, `AddTurn` (void), `SaveAsync` · `IVoiceRepository`: `GetAllAsync` (không `Clip`), `GetByNameAsync`, `AddAsync`, `RemoveAsync`.

**DTO thêm/đổi:** `AccountDto.StudentCode`, `CreateAccountRequest/UpdateAccountRequest.StudentCode`, `LanguageConfigDto.TtsVoice`, `UpdateLanguageRequest.TtsVoice`, `SpeechConfig(SubjectCode, SttLocale, TtsLocale, TtsVoice)`, `InterviewStateDto` (có `SessionId` để báo SignalR), `TurnDto`, `TranscriptDto`, `MyInterviewDto`, `VoiceDtos`.

**`IInterviewService`** (TV3):
- **Quyền** (dùng chung): Sinh viên đang hoạt động chỉ vào lượt thi có `StudentCode` trùng mã của mình (không phân biệt hoa thường); Admin/Giảng viên qua `CanAccessSubjectAsync`; còn lại `Forbidden`.
- `StartAsync`: NotFound/Forbidden; chưa Completed thì phiên phải `Open`; chưa có lượt nào → cần có bộ câu hỏi, tạo lượt `Main` của câu `OrderNo` nhỏ nhất, thí sinh → `InProgress`; gọi lại không tạo thêm lượt.
- `SubmitAnswerAsync`: lượt tồn tại → quyền → phiên `Open` → lượt **chưa trả lời**; ghi câu trả lời (trim, cắt 4000 ký tự, `AnsweredAt`); `TimedOut` = cờ trình duyệt **hoặc** quá `AnswerSeconds + 15` giây; **hỏi xoáy** chỉ khi câu trả lời không rỗng **và** số lượt xoáy của câu chính < `MaxFollowUpsPerQuestion` **và** tổng lượt xoáy của thí sinh < `MaxFollowUps` **và** `IFollowUpGenerator` nói `Ask` có câu hỏi; không thì sang câu chính kế tiếp, hết câu → `Completed`; **lưu một lần**.
- `InterviewStateDto`: `Finished`, lượt đang mở, `MainIndex/MainTotal`, `FollowUpIndex/FollowUpMax`, `SecondsLeft = AnswerSeconds + 15 − đã trôi qua` kẹp `[0, AnswerSeconds]`, `SttLocale/TtsLocale`, `SessionId`.
- `GetMyInterviewsAsync` (chỉ Sinh viên có mã), `GetTranscriptAsync`, `SpeakAsync` (đọc **nội dung đã lưu** bằng giọng của môn; không có âm thanh → `Fail` để trình duyệt dùng giọng dự phòng).

**`IFollowUpGenerator.DecideAsync(FollowUpContext(Question, ExpectedPoints, Locale, Exchanges))` → `FollowUpDecision(Ask, Question, Reason)`** (TV6). **`ITextToSpeech`** (TV2, 4 hàm ở B2). **`IVoiceService`**: `GetOverviewAsync`, `CloneAsync`, `DeleteAsync`, `OpenSpeechAsync`, `DefaultVoice` (TV2).

### B3.3 Giao diện MVC

Quy tắc chung như A3.3 (Controller thay cho PageModel, lớp cha `AppController`).

| Controller / action | Ai vào | Người làm | Nội dung |
|---|---|---|---|
| `Auth`: `Login`, `Register`, `Logout`, `AccessDenied` | mọi người | TV4 | như Ass2 |
| `Home/Index` | đã đăng nhập | TV4 | Sinh viên → `/MyExams`; còn lại → `/Exams` |
| `Accounts`: `Index`, `Create`, `Edit`, `SetActive`, `ResetPassword` | Admin | TV4 | như Ass2 + ô **MSSV**; có vai Sinh viên |
| `Subjects`: `Index`, `Details`, `Assign`, `Unassign` | Admin | TV5 | như Ass2 |
| `MySubjects/Index` | Giảng viên | TV5 | như Ass2 |
| `Language/Edit` | Admin, Giảng viên | TV5 | như Ass2 |
| `Exams`: `Index`, `Create`, `Details`, `SetOpen`, `Reallocate`, `Questions`, `AddQuestion`, `SetQuestionActive` | Admin, Giảng viên | TV4 | như Ass2 + nút **Bắt đầu thi / Tiếp tục thi**, **Biên bản** ở Chi tiết |
| `MyExams/Index` | Sinh viên | TV5 | các lượt thi theo MSSV |
| `Interview/Transcript?participantId=` | Admin, Giảng viên | TV6 (action), TV5 (view) | biên bản |
| `Interview/Room?participantId=` | Sinh viên (của mình), Admin, Giảng viên | TV6 | phòng thi |
| `Interview/Start`, `Answer`, `Speak` (POST) | như trên | TV6 | xem dưới |
| `Voices`: `Index`, `Preview`, `Clone`, `Delete`, `Assign` | Admin, Giảng viên (Clone/Delete chỉ Admin) | TV2 | trạng thái VieNeu, danh sách giọng, nghe thử, giọng theo môn, clone |

**Ba điểm cuối JSON của phòng thi** (token qua tiêu đề `RequestVerificationToken`, dữ liệu dạng form):

| Action | Vào | Ra |
|---|---|---|
| `POST /Interview/Start` | `participantId` | 200 JSON `InterviewStateDto` (camelCase) |
| `POST /Interview/Answer` | `turnId`, `answer`, `timedOut` | 200 JSON trạng thái kế tiếp; lỗi 400/403/404 kèm `{ "error" }` |
| `POST /Interview/Speak` | `turnId` | 200 luồng `audio/pcm` (s16le, 48 kHz, mono); 503 nếu VieNeu chưa chạy |

**Luồng trình duyệt ở phòng thi:** (1) nút Bắt đầu/Tiếp tục (cần cú nhấp để được phát âm thanh, mở micro) → `Start`; (2) đọc câu hỏi: thử `Speak` + `pcm-player.js`, lỗi/503 thì `speechSynthesis` theo `ttsLocale`; (3) đọc xong mới mở `SpeechRecognition` (`sttLocale`, `continuous`, `interimResults`), chữ hiện ngay khi nói, tự bật lại khi trình duyệt dừng, giữ phần sinh viên sửa tay; (4) đếm ngược từ `secondsLeft` sau khi đọc xong, đỏ khi ≤ 10 giây, hết giờ tự nộp `timedOut = true`; (5) Nộp: dừng micro và âm thanh, khóa nút, `Answer`, lặp lại; lỗi thì mở lại nút; (6) `finished` → "Đã hoàn thành phần thi vấn đáp". Không có micro: vẫn gõ được.

### B3.4 SignalR (Ass3)

Hub, sự kiện, `live.js` **giống Ass2** (A3.4). Topic:

| Topic | Gửi khi | Ai gửi | Vùng nhận |
|---|---|---|---|
| `accounts` | có người đăng ký | `AuthController.Register` | `/Accounts`: `account-list` |
| `exams` | tạo, đóng/mở phiên | `ExamsController` | `/Exams`: `exam-list` |
| `exam-{id}` | đóng/mở, chọn lại câu hỏi | `ExamsController` | `/Exams/Details/{id}`: `exam-detail` |
| `exam-{id}` | thí sinh **bắt đầu thi, trả lời, thi xong** | `InterviewController.Start/Answer` (dùng `InterviewStateDto.SessionId`) | `/Exams/Details/{id}`: trạng thái thí sinh, nút Biên bản xuất hiện |

### B3.5 Web API (Swagger tại `/swagger`)

Đăng nhập bằng `POST /api/auth/login` (cookie), rồi gọi ngay trên trang Swagger. JSON camelCase, enum bằng chữ. Lỗi: `{ "error": "..." }`. Chưa đăng nhập → **401** (không chuyển hướng), không đủ vai trò hoặc Service báo `Forbidden` → **403**, `NotFound` → **404**, lỗi nghiệp vụ → **400**.

| Nhóm | Đường dẫn | Ai gọi | Người làm |
|---|---|---|---|
| Đăng nhập | `POST /api/auth/login` (200 `LoginResult` / 401), `POST /api/auth/logout` (204), `GET /api/auth/me`, `POST /api/auth/register` (201 `{id}`, ẩn danh) | mọi người | TV4 |
| Tài khoản | `GET /api/accounts?keyword=&role=`, `GET /api/accounts/{id}`, `POST /api/accounts` (201), `PUT /api/accounts/{id}` (cấp vai trò, `studentCode`), `PATCH /api/accounts/{id}/active` `{isActive}`, `POST /api/accounts/{id}/reset-password` `{newPassword}` | Admin | TV4 |
| Môn, phân công, ngôn ngữ | `GET /api/subjects`, `GET /api/subjects/{id}` (Admin), `POST /api/subjects/{id}/lecturers` `{lecturerId}`, `DELETE /api/subjects/{id}/lecturers/{lecturerId}`, `GET/PUT /api/subjects/{id}/language` `{sttLanguage, ttsLanguage, ttsVoice?}`, `GET /api/subjects/{id}/speech` | Admin, Giảng viên | TV5 |
| Phiên thi | `GET /api/exams`, `GET /api/exams/subjects`, `POST /api/exams` (201 `{id}`), `GET /api/exams/{id}`, `PATCH /api/exams/{id}/status` `{open}`, `POST /api/exams/{id}/reallocate` | Admin, Giảng viên | TV5 |
| Câu hỏi | `GET/POST /api/subjects/{id}/questions` `{content, expectedPoints?}`, `PATCH /api/questions/{id}/active` `{isActive}` | Admin, Giảng viên | TV5 |
| **Phỏng vấn AI** | `GET /api/interviews/my` (Sinh viên), `POST /api/interviews/{participantId}/start`, `POST /api/interviews/turns/{turnId}/answer` `{answer, timedOut}` → `InterviewStateDto` (câu hỏi xoáy hoặc câu kế), `GET /api/interviews/turns/{turnId}/speech` (`audio/pcm` / 503), `GET /api/interviews/{participantId}/transcript` | Sinh viên (của mình), Admin, Giảng viên (môn được phân công) | TV6 |

## B4. Kiểm thử

### B4.1 Test tự động `AIVES.Tests` (204)

| Lớp test | Số | Người | Lớp test | Số | Người |
|---|---|---|---|---|---|
| `PasswordHasherTests` | 10 | TV3 | `ExamServiceTests` | 41 | TV3 |
| `AuthServiceTests` | 9 | TV3 | `InterviewServiceTests` | 32 | TV3 |
| `AccountServiceTests` | 34 | TV3 | `QuestionAllocatorTests` | 6 | TV5 |
| `AssignmentServiceTests` | 11 | TV3 | `QuestionServiceTests` | 9 | TV5 |
| `SubjectServiceTests` | 4 | TV3 | `FollowUpGeneratorFallbackTests` | 9 | TV6 |
| `LanguageConfigServiceTests` | 8 | TV3 | `VoiceServiceTests` | 31 | TV2 |

### B4.2 Test SQL `AIVES.Tests.Data` (63)

`SchemaTests` 32 (Tien) · `AccountRepositoryTests` 8 · `SubjectRepositoryTests` 5 · `LecturerSubjectRepositoryTests` 2 · `QuestionRepositoryTests` 2 · `ExamRepositoryTests` 6 · `InterviewRepositoryTests` 4 · `VoiceRepositoryTests` 4 (TV2).

Phần gọi mạng (`VieNeuTextToSpeech`, phần DeepSeek của `FollowUpGenerator`) **không có test tự động**: kiểm bằng checklist G, H và một chương trình console nhỏ (không commit) đính ảnh vào PR.

### B4.3 Checklist thử tay

**A, C, D:** làm lại các mục A, C, D của Ass2 (A4.3) trên bản MVC. Thêm: đăng nhập `sv.a@fu.edu.vn` → vào "Bài thi của tôi", thanh điều hướng chỉ có mục đó; sinh viên gõ `/Accounts`, `/Exams`, `/Voices` → Không có quyền.

**R3. Đăng ký và cấp vai trò Sinh viên (TV4, TV3)**
- [ ] Làm lại toàn bộ mục R của Ass2.
- [ ] Admin cấp vai trò **Sinh viên** mà để trống MSSV → báo *"Sinh viên phải có mã sinh viên."*, không lưu.
- [ ] MSSV trùng với sinh viên khác (`SE180001`) → báo trùng.
- [ ] Cấp Sinh viên với `SE180002` → người đó đăng nhập được, vào "Bài thi của tôi"; danh sách tài khoản hiện `(SE180002)`.
- [ ] Đổi sinh viên đó sang Giảng viên → MSSV bị xóa.

**B3. Tài khoản (TV4):** làm lại mục B của Ass2; ô lọc vai trò có "Sinh viên"; trang Tạo có ô MSSV.

**E3. Phiên thi (TV4):** làm lại mục E của Ass2 trên bản MVC, thêm:
- [ ] Chi tiết phiên **đang mở**: thí sinh chưa thi có nút "Bắt đầu thi"; đang thi có "Tiếp tục thi" và "Biên bản"; đã thi chỉ có "Biên bản". Phiên **đã đóng**: không có nút bắt đầu.

**F. Bài thi của tôi và Biên bản (TV5)**
- [ ] `sv.a@fu.edu.vn` (có tên `SE180001` trong một phiên) → thấy đúng phiên của mình, "Sẵn sàng", nút "Vào phòng thi". Sinh viên chưa có tên trong phiên nào → thông báo trống.
- [ ] Biên bản sau khi thi: thẻ từng lượt; lượt hỏi xoáy thụt vào, có "Hỏi xoáy" và "Lý do hỏi thêm"; lượt hết giờ có "Hết giờ"; link quay lại đúng trang Chi tiết phiên thi.

**G. Phòng thi (TV6), Chrome hoặc Edge**
- [ ] Sinh viên mở phòng thi của mình → tên, mã, môn, nút "Bắt đầu phỏng vấn". Mở phòng thi của **người khác** → bị chặn.
- [ ] Bắt đầu → "Câu chính 1/N"; AI **đọc thành tiếng**; **sau khi đọc xong** đồng hồ mới chạy và micro mở.
- [ ] Nói → chữ hiện **ngay khi nói**; gõ sửa tay rồi nói tiếp → phần sửa không mất.
- [ ] Nộp → âm thanh dừng ngay, sang câu kế. Trả lời mơ hồ ("Cái đó dùng ModelState") → hỏi xoáy 1/2, mơ hồ tiếp → 2/2, rồi sang câu chính kế (không có lần 3).
- [ ] Đặt 10 giây/câu, để hết giờ → tự nộp; biên bản có "Hết giờ". Nhấp đúp Nộp → chỉ một lượt được ghi.
- [ ] Tải lại trang giữa chừng → "Tiếp tục" về đúng câu, đồng hồ còn đúng thời gian còn lại.
- [ ] Câu cuối → "Đã hoàn thành"; sinh viên có nút về Bài thi của tôi, giảng viên có Xem biên bản. Vào lại → không tạo thêm lượt.
- [ ] Đóng phiên rồi sinh viên nộp → báo phiên đã đóng. Từ chối micro → vẫn gõ và nộp được.
- [ ] Tắt VieNeu → đọc bằng giọng trình duyệt; bật VieNeu → `Speak` trả `audio/pcm`, byte đầu dưới 1 giây.
- [ ] Xóa `Ai:ApiKey` → vẫn hỏi xoáy bằng luật dự phòng, không lỗi. Môn có giọng riêng → đọc đúng giọng đó.

**H. Giọng đọc (TV2)**
- [ ] VieNeu tắt: "Chưa chạy" + hướng dẫn, không lỗi, nút Clone bị khóa. Bật: "Đang chạy", 48 kHz, 25 giọng.
- [ ] Nghe thử một giọng; bấm giọng khác giữa chừng → giọng trước dừng.
- [ ] Admin clone với file WAV 3 đến 8 giây → xuất hiện "Đã clone", nghe thử được; tên trùng `Mai Anh` → báo lỗi, **không** lưu DB; file `.exe`, > 20 MB, tên chứa `/` → báo lỗi.
- [ ] Giảng viên không thấy form Clone, không xóa được.
- [ ] Gán giọng clone cho môn → phòng thi môn đó đọc giọng clone; xóa giọng → môn về giọng mặc định.
- [ ] `taskkill /F /IM AIVES.Ass3.MVC.exe` → vài giây sau `http://localhost:8000/health` không trả lời.
- [ ] Chuyển thư mục dự án sang chỗ khác (hoặc máy khác) rồi chạy lại → VieNeu vẫn lên được (không bị `ModuleNotFoundError: vieneu`).
- [ ] Cố tình làm VieNeu lỗi (ví dụ đổi tên thư mục `apps`) → log có **cảnh báo** "VieNeu-TTS đã dừng (mã ...)" kèm dòng lỗi, phòng thi vẫn dùng giọng trình duyệt.

**S3. SignalR (Tien, TV4, TV6)**
- [ ] Làm lại mục S của Ass2 trên bản MVC.
- [ ] Trình duyệt 1: giảng viên mở Chi tiết phiên thi. Trình duyệt 2: sinh viên vào phòng thi, bấm Bắt đầu → trình duyệt 1 **tự** đổi thí sinh sang "Đang thi" và hiện nút "Biên bản", có thông báo *"... đang thi."*
- [ ] Sinh viên nộp câu cuối → trình duyệt 1 tự đổi "Đã thi", thông báo *"... đã thi xong."*
- [ ] Trang Chi tiết của **phiên khác** không bị làm mới.

### B4.4 Kiểm thử Web API bằng `ass3.http` (có trong gói)

Mở `ass3.http` bằng Visual Studio 2022 (17.12 trở lên, hỗ trợ biến `{{tên.response.body...}}`) hoặc VS Code + REST Client. Tạo database **mới** bằng 01 → 02 → 03, chạy ứng dụng, gửi **từ trên xuống**. Mỗi khối có dòng **KẾT QUẢ MONG ĐỢI**; 78 lời gọi chia theo người:

| Mục | Nội dung | Người |
|---|---|---|
| 0, 1 | chưa đăng nhập → 401, Swagger, đăng nhập (sai mật khẩu và sai email cùng một thông báo) | TV4, TV6 |
| 2 | tài khoản: tạo sinh viên, trùng MSSV, thiếu MSSV, tự khóa mình, đặt lại mật khẩu | TV4 |
| 3, 4 | môn học, phân công, ngôn ngữ, giọng; giảng viên sửa môn không được phân công → 403 | TV5 |
| 5, 6 | câu hỏi, tạo phiên thi (ngày quá khứ, số ngoài khoảng, dòng sai, môn không được phân công), đóng/mở, chọn lại; sinh viên gọi API nhân sự → 403 | TV5 |
| 7 | **tự đăng ký** → chờ cấp quyền → admin cấp vai trò Sinh viên (thiếu MSSV → 400) → đăng nhập được | TV4 |
| 8 | **phỏng vấn AI**: vào lượt thi người khác → 403; bắt đầu; trả lời mơ hồ → **câu hỏi xoáy 1/2, 2/2**, rồi sang câu chính kế (không có lượt 3); nộp lại → 400; bỏ trống + hết giờ → không hỏi xoáy; giọng đọc 200/503; biên bản của mình 200, của người khác 403; giảng viên xem biên bản 200 | TV6, TV3 |

Đã chạy thử trên bản tham chiếu với **DeepSeek thật và VieNeu-TTS thật**: cả 78 lời gọi đúng. Mục 8.9 trả về luồng `audio/pcm` khoảng 5,6 giây (VieNeu tắt thì 503). Câu hỏi xoáy do DeepSeek viết bám đúng nội dung câu hỏi chính, ví dụ câu chính về Dependency Injection, sinh viên trả lời "Cái đó dùng ModelState" thì AI hỏi *"Bạn có thể giải thích Dependency Injection là gì không? Và trong ASP.NET Core, làm thế nào để đăng ký một service với vòng đời scoped?"* với lý do *"Câu trả lời chưa đúng trọng tâm..."*. Không có khóa AI thì dùng luật dự phòng (câu hỏi mẫu "Bạn có thể giải thích rõ hơn...").

---

# Phụ lục

**Tài khoản mẫu**

| Email | Mật khẩu | Vai trò | Có ở |
|---|---|---|---|
| admin@fu.edu.vn | Admin@123 | Quản trị viên | Ass2, Ass3 |
| an.nv@fu.edu.vn | Lecturer@123 | Giảng viên (PRN222) | Ass2, Ass3 |
| binh.tt@fu.edu.vn | Lecturer@123 | Giảng viên (ENW492c) | Ass2, Ass3 |
| moi.dangky@fu.edu.vn | Lecturer@123 | Chờ cấp quyền | Ass2, Ass3 |
| sv.a@fu.edu.vn | Student@123 | Sinh viên `SE180001` | Ass3 |

**Lệnh hay dùng**

```
dotnet build
dotnet test AIVES.Tests
dotnet test AIVES.Tests --filter "FullyQualifiedName~InterviewServiceTests"
$env:AIVES_TEST_CONNECTION = "Server=.;Database=AIVESDb_Test;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test AIVES.Tests.Data
dotnet user-secrets set "Ai:ApiKey" "<khóa>" --project AIVES.Ass3.MVC
grep -rn NotImplementedException --include=*.cs .
```

**Cổng và cookie:** mỗi bài một tên cookie (`.AIVES.Ass2`, `.AIVES.Ass3`) nên chạy cùng lúc trên `localhost` không đè nhau. VieNeu-TTS nghe ở `http://localhost:8000`.
