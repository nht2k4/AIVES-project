# Tiến độ Ass2 của TV5 — ngày 10/10/2026

Project làm việc chính: `AIVES-project`. Nhánh tính năng: `TV5-ass2-questions-subjects-language`; nhánh đích để hợp nhất: `assignment-2`. Phạm vi: chỉ phần Ass2 trong `AIVES_CongViec_TV5.md`; tài liệu đối chiếu: `AIVES_TV5_DoiChieu_ProjectMau.md`.

Đã viết xong các việc 5.1, 5.2 và 5.3. Các phần này đã được kiểm tra riêng, dùng thành phần giả lập thay cho những phần phụ thuộc chưa hoàn thành. **Toàn bộ bài chưa đủ điều kiện nghiệm thu vì một số phần bắt buộc của các thành viên khác vẫn chưa xong.**

- Chia câu hỏi: cả **6 trường hợp kiểm thử gốc đều đạt**.
- Xử lý câu hỏi và kiểm tra các trang: **23 trường hợp kiểm thử bổ sung đều đạt**, dùng mã nguồn thật của TV5 và thành phần giả lập thay cho các phần phụ thuộc chưa hoàn thành.
- Biên dịch toàn bộ solution: **0 cảnh báo, 0 lỗi**.
- Kiểm thử gốc của dịch vụ câu hỏi: **9 trường hợp chưa đạt do bị chặn** ở phần kiểm quyền chưa hoàn thành của TV3.
- Toàn bộ bộ kiểm thử Business gốc: **6 đạt, 121 không đạt**; các trường hợp không đạt nằm ở những dịch vụ chưa hoàn thành của TV3.
- Kiểm thử SQL: **cả 22 trường hợp được phát hiện đều bị bỏ qua**, vì chưa cấu hình kết nối tới cơ sở dữ liệu kiểm thử.
- Kiểm tra bằng trình duyệt: trang chi tiết môn, lưu ngôn ngữ, thêm/tắt/bật câu hỏi và danh sách môn của giảng viên hoạt động trong **bản xem thử tạm thời**. Kết quả này chưa chứng minh ứng dụng thật đã được tích hợp hoàn chỉnh hoặc hoạt động với cơ sở dữ liệu thật.

## Các phần cần phối hợp để hoàn thành

| Thành viên | Công việc còn cần hoàn thành |
|---|---|
| TV3 | Kiểm quyền theo môn, phân công/gỡ phân công, dịch vụ môn học/ngôn ngữ và tích hợp dịch vụ phiên thi |
| TV2 | Các lớp truy cập cơ sở dữ liệu (repository) và đăng ký chúng để ứng dụng sử dụng |
| TV6 | Nền ứng dụng, đăng ký dịch vụ, kiểm tra vai trò/cookie đăng nhập, bố cục chung và các trang phiên thi |
| TV4 | Trang đăng nhập và trang Không có quyền (AccessDenied), nối với chức năng xác thực hoạt động được |
| Tien | Thống nhất việc bổ sung chức năng sửa câu hỏi vào interface dùng chung; mini-task này đang được hoãn đúng theo yêu cầu của bạn |
| TV3 / Tien | Duyệt chéo, kiểm tra danh sách nghiệm thu trên ứng dụng đã tích hợp, duyệt PR và hợp nhất mã nguồn |

Các ô nghiệm thu trong danh sách công việc gốc chưa được đánh dấu hoàn thành chỉ dựa trên kết quả kiểm tra riêng. Không thay đổi mã nguồn ứng dụng thuộc phần việc của các thành viên khác hoặc các hợp đồng dùng chung.

Xem [báo cáo bàn giao đầy đủ](TV5_ASS2_HANDOFF.md) để biết các lệnh kiểm tra, file phụ thuộc, bằng chứng, ảnh giao diện, độ phức tạp thuật toán và hai ví dụ dùng khi bảo vệ. Xem [danh sách các bước thực hiện](TV5_ASS2_PLAN.md) để theo dõi những bước đã làm.
