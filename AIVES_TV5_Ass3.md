# AIVES (Nhóm 4): Việc của TV5 ở Ass3

Lọc từ [AIVES_PhanCong_Ass2_Ass3.md](AIVES_PhanCong_Ass2_Ass3.md), phần B (Ass3: MVC + Web API, chức năng 7 + 2 + 3). File gốc vẫn là bản chính thức; khi hai file khác nhau thì theo file gốc.

**Phần của TV5:** Câu hỏi, Môn học, Bài thi của tôi, Biên bản, và Web API cho môn học, phiên thi, câu hỏi.

## 1. Tóm tắt

| Việc | Nội dung | Thời lượng | Giai đoạn | Test / kiểm tra |
|---|---|---|---|---|
| **5.1** | `QuestionAllocator` + `QuestionService` (chép từ Ass2) | 0,5 buổi | A. Nền + chép lại phần cũ | `QuestionAllocatorTests` (6), `QuestionServiceTests` (9) |
| **5.2** | `SubjectsController`, `MySubjectsController`, `LanguageController` + views | 2 buổi | C. Giao diện MVC và API | checklist C, D |
| **5.3** | `MyExamsController` (Bài thi của tôi) + view `Interview/Transcript` (Biên bản) | 2 buổi | C | checklist F |
| **5.4** | Web API `/api/subjects`, `/api/exams`, câu hỏi | 2 buổi | C | `ass3.http` mục 3 đến 6 |

- **Người duyệt chéo:** TV3 (TV3 ↔ TV5).
- **Phụ thuộc vào người khác:**
  - TV6 6.1 (`Program.cs`, `AppController`, `_Layout`) xong thì mới làm trang (5.2, 5.3).
  - TV6 6.1 (Swagger, cookie trả 401/403 cho `/api`) và TV4 4.4 (`ApiControllerBase`) xong thì mới làm 5.4.
  - 5.3 dùng `IInterviewService.GetMyInterviewsAsync` của TV3 3.3.
- **Người khác chờ TV5:** TV6 6.3 (phòng thi) cần 5.3, vì trang **Bài thi của tôi** là lối vào phòng thi của sinh viên.

## 2. Chi tiết từng việc

### 5.1 `QuestionAllocator` + `QuestionService` (0,5 buổi)

Chép `QuestionAllocator`, `QuestionService` từ Ass2. Test: `QuestionAllocatorTests` (6) + `QuestionServiceTests` (9).

Đặc tả (giữ như Ass2, A3.2):
- `QuestionAllocator.Allocate(pool, students, perStudent, rng)`: mỗi sinh viên đủ câu, không lặp câu trong bộ; hai sinh viên **liền nhau không chung câu** khi `pool ≥ 2 × perStudent`; dùng các câu đều nhau; ngân hàng nhỏ thì buộc dùng lại; dùng đúng `rng`. Gợi ý: xếp hạng câu theo (không thuộc bộ của người trước) → (ít được giao nhất) → ngẫu nhiên, rồi lấy đầu bảng.
- `IQuestionService`: `GetBySubjectAsync` (trả `Forbidden` nếu không có quyền), `AddAsync` (nội dung ≤ 1000, ý chính ≤ 1000, rỗng → `null`), `SetActiveAsync` (quyền theo môn của câu hỏi).
- Luật chung của Service: kiểm theo thứ tự, dừng ở lỗi đầu tiên, không để thay đổi nửa chừng; trả `ServiceResult` (`Validation` / `NotFound` / `Forbidden`), không ném exception cho lỗi nghiệp vụ.

### 5.2 Môn học, Môn của tôi, Ngôn ngữ (MVC, 2 buổi)

- `SubjectsController`: `Index`, `Details`, `Assign`, `Unassign`. Chỉ Admin. Nội dung như Ass2: phân công/gỡ giảng viên, khung ngôn ngữ, khung ngân hàng câu hỏi.
- `MySubjectsController` (`Index`): chỉ Giảng viên. Như Ass2: nút **Đổi ngôn ngữ**, nút **Câu hỏi**.
- `LanguageController`: `Edit` GET/POST. Admin và Giảng viên vào được, còn **Service quyết định quyền**. Như Ass2, thêm khung **Chỉnh tay cho phòng thi**:
  - ô nhiều dòng **từ điển thuật ngữ** (mỗi dòng `cách nghe sai => cách viết đúng`, ví dụ `ra dơ pây => Razor Pages`);
  - ô số **thời gian chờ AI**, từ 2 đến 30 giây;
  - ô chọn **"Cho phép gửi câu trả lời tới AI bên ngoài"** kèm ghi chú quyền riêng tư (hệ thống không ghi âm, chỉ lưu văn bản).
- Nút **Câu hỏi** và khung **Ngân hàng câu hỏi** trỏ tới `/Exams/Questions?subjectId=` (trang đó ở Ass3 thuộc `ExamsController` của TV4).

Quy tắc giao diện chung (A3.3, Controller thay cho PageModel, lớp cha `AppController`):
- Controller chỉ gọi interface Business, không dùng `AppDbContext` hay Entity, không có luật nghiệp vụ.
- Mọi form POST có antiforgery; dùng Post-Redirect-Get với `TempData["Success"/"Error"]`, hiện bằng `_Alerts`.
- Lỗi `Validation` hiện trên form (giữ các ô đã nhập), `NotFound` → 404, `Forbidden` → trang Không có quyền.
- Bootstrap 5; tiêu đề `Tên trang - AIVES`.

### 5.3 Bài thi của tôi + Biên bản (2 buổi)

**`MyExamsController`** (chỉ Sinh viên), gọi `IInterviewService.GetMyInterviewsAsync`, kèm view:
- Bảng gồm các cột: Phiên thi, Môn, Khung giờ, Trạng thái (**Sẵn sàng** / **Đang thi** / **Đã thi** / **Phiên thi đã đóng**).
- Nút **Vào phòng thi** / **Tiếp tục thi**, trỏ tới `Interview/Room?participantId=` (phòng thi do TV6 làm).
- Khi chưa có phiên nào: *"Chưa có phiên thi nào có tên bạn..."*.

**View `Views/Interview/Transcript.cshtml`** (model `TranscriptDto`; action do TV6 làm):
- Mỗi lượt là một thẻ, có nhãn **Câu chính** hoặc **Hỏi xoáy** (lượt hỏi xoáy thụt vào), nhãn **Hết giờ**, giờ hỏi, "Lý do hỏi thêm", và câu trả lời.
- Câu trả lời rỗng hiện "(không trả lời)"; lượt chưa trả lời hiện "(chưa trả lời)".
- Link quay về `/Exams/Details/{SessionId}`.

### 5.4 Web API: môn học, phiên thi, câu hỏi (2 buổi)

Kế thừa `ApiControllerBase` của TV4 (`[ApiController]`, `[Authorize]`, `CurrentUserId`, `Respond(...)`). Kiểm bằng `ass3.http` mục 3 đến 6.

**`SubjectsApiController`** (`/api/subjects`), dành cho Admin và Giảng viên:

| Đường dẫn | Ghi chú |
|---|---|
| `GET /api/subjects` | danh sách theo vai trò |
| `GET /api/subjects/{id}` | chi tiết (Admin) |
| `POST /api/subjects/{id}/lecturers` `{lecturerId}` | phân công |
| `DELETE /api/subjects/{id}/lecturers/{lecturerId}` | gỡ phân công |
| `GET/PUT /api/subjects/{id}/language` `{sttLanguage, ttsLanguage, ttsVoice?, sttTerms?, aiTimeoutSeconds?, useExternalAi?}` | ngôn ngữ, giọng, chỉnh tay STT/AI |
| `GET /api/subjects/{id}/speech` | cấu hình giọng nói của môn |

**`ExamsApiController`** (`/api/exams` + câu hỏi), dành cho Admin và Giảng viên:

| Đường dẫn | Ghi chú |
|---|---|
| `GET /api/exams` | danh sách |
| `GET /api/exams/subjects` | các môn được tạo phiên thi |
| `POST /api/exams` | tạo → **201** `{ "id" }` |
| `GET /api/exams/{id}` | chi tiết |
| `PATCH /api/exams/{id}/status` `{open}` | đóng/mở |
| `POST /api/exams/{id}/reallocate` | chọn lại bộ câu hỏi |
| `GET/POST /api/subjects/{id}/questions` `{content, expectedPoints?}` | câu hỏi của môn |
| `PATCH /api/questions/{id}/active` `{isActive}` | tắt/bật câu hỏi |

Quy ước API chung (B3.5):
- JSON camelCase, enum trả bằng chữ.
- Lỗi trả thân `{ "error": "..." }`.
- Chưa đăng nhập → **401** (không chuyển hướng); không đủ vai trò hoặc Service báo `Forbidden` → **403**; `NotFound` → **404**; lỗi nghiệp vụ → **400**.
- Thành công không có dữ liệu → **204**, có dữ liệu → **200**.

## 3. Kiểm thử phải qua

### Test tự động
```
dotnet test AIVES.Tests --filter "FullyQualifiedName~QuestionAllocatorTests"
dotnet test AIVES.Tests --filter "FullyQualifiedName~QuestionServiceTests"
```

### Checklist thử tay (người viết tick, TV3 bấm lại)

Chung cho C, D (Ass3): làm lại trên bản MVC, thêm:
- [ ] Đăng nhập `sv.a@fu.edu.vn` → vào "Bài thi của tôi", thanh điều hướng chỉ có mục đó; sinh viên gõ `/Accounts`, `/Exams`, `/Voices` → Không có quyền.

**C. Môn học và phân công**
- [ ] Có 5 môn; `PRN211` hiện mờ + "Đã ngừng"; số giảng viên đúng.
- [ ] Chi tiết PRN222: an.nv có trong bảng; ô chọn **không** có an.nv, admin, tài khoản bị khóa, tài khoản Chờ cấp quyền.
- [ ] Bấm Phân công khi chưa chọn → `Chọn giảng viên cần phân công.`; phân công binh.tt vào SWP391 → thành công; phân công vào PRN211 → báo môn đã ngừng; Gỡ → `Đã gỡ phân công.`
- [ ] Giảng viên vào `/MySubjects` chỉ thấy môn của mình; gõ `/Subjects` → Không có quyền.

**D. Ngôn ngữ**
- [ ] Admin đổi STT của môn sang English → về chi tiết môn, có thông báo, danh sách hiện `en-US`; có dòng "Lần sửa gần nhất ... bởi ...".
- [ ] **Điểm nhấn:** an.nv mở `/Language/Edit?subjectId=<SWP391>` → Không có quyền (cả GET lẫn POST).

**F. Bài thi của tôi và Biên bản**
- [ ] `sv.a@fu.edu.vn` (có tên `SE180001` trong một phiên) → thấy đúng phiên của mình, "Sẵn sàng", nút "Vào phòng thi". Sinh viên chưa có tên trong phiên nào → hiện thông báo trống.
- [ ] Biên bản sau khi thi: mỗi lượt một thẻ; lượt hỏi xoáy thụt vào, có "Hỏi xoáy" và "Lý do hỏi thêm"; lượt hết giờ có "Hết giờ"; link quay lại đúng trang Chi tiết phiên thi.

Liên quan tới khung **Chỉnh tay** ở trang Ngôn ngữ (checklist G của TV6 sẽ thử):
- [ ] Môn PRN222 có dòng `ra dơ pây => Razor Pages`; nói/gõ "ra dơ pây" → biên bản lưu "Razor Pages".
- [ ] Đặt thời gian chờ AI 2 giây và rút mạng → phòng thi vẫn hỏi tiếp sau khoảng 2 giây bằng luật dự phòng.
- [ ] Bỏ chọn "Cho phép gửi câu trả lời tới AI bên ngoài" → câu hỏi xoáy là câu của luật dự phòng (không gọi DeepSeek).

### `ass3.http` (mở bằng VS 2022 17.12+ hoặc VS Code + REST Client; tạo database **mới** bằng 01 → 02 → 03, gửi **từ trên xuống**)

| Mục | Nội dung |
|---|---|
| 3, 4 | môn học, phân công, ngôn ngữ, giọng; **chỉnh tay STT/AI** (3.10 đến 3.12: từ điển thuật ngữ, thời gian chờ AI ngoài 2..30 → 400, dòng từ điển sai → 400); giảng viên sửa môn không được phân công → 403 |
| 5, 6 | câu hỏi, tạo phiên thi (**thí sinh chưa có tài khoản → 400**, ngày quá khứ, số ngoài khoảng, dòng sai, môn không được phân công), đóng/mở, chọn lại; sinh viên gọi API dành cho nhân sự → 403 |

Mỗi khối phải ra đúng dòng **KẾT QUẢ MONG ĐỢI**.

## 4. Mini-task và câu hỏi bảo vệ

**Mini-task:** nút **"Tải biên bản"** xuất file `.txt` (một action trả `File(...)`).

**Câu hỏi bảo vệ:**
1. Sinh viên A gõ tay địa chỉ phòng thi của sinh viên B: chặn ở đâu?
2. Vì sao `MyExams` lọc theo **mã sinh viên của tài khoản** chứ không theo tên?

## 5. "Xong" một việc nghĩa là

- [ ] Test của việc đó xanh (hoặc các mục checklist tương ứng đã tick nếu là giao diện).
- [ ] Không còn `NotImplementedException` trong file của mình; build không có cảnh báo mới.
- [ ] PR được TV3 duyệt và đã merge; TV3 chạy lại được.
- [ ] Trả lời được câu hỏi bảo vệ của việc đó.

**Quy trình Git:** một việc = một nhánh (ví dụ `tv5/5.4-subjects-api`) = một PR, dưới khoảng 400 dòng. Mô tả PR ghi: test nào đã xanh (dán kết quả), mục checklist nào đã tick, ảnh chụp với việc giao diện. Không sửa file hợp đồng (Entities, `AppDbContext`, Interfaces, DTOs, Models, test); không commit `bin/`, `obj/`, `.vs/`, `VieNeu-TTS/`, khóa API.

## 6. Tài khoản mẫu để thử

| Email | Mật khẩu | Vai trò |
|---|---|---|
| admin@fu.edu.vn | Admin@123 | Quản trị viên |
| an.nv@fu.edu.vn | Lecturer@123 | Giảng viên (PRN222) |
| binh.tt@fu.edu.vn | Lecturer@123 | Giảng viên (ENW492c) |
| moi.dangky@fu.edu.vn | Lecturer@123 | Chờ cấp quyền |
| sv.a@fu.edu.vn | Student@123 | Sinh viên `SE180001` |
