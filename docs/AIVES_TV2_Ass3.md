# TV2 · Assignment 3

Áp dụng lần lượt script `Database/01_ChucNang7.sql`, `02_ChucNang2.sql` và `03_ChucNang3.sql`. Để chạy test repository, đặt `AIVES_TEST_CONNECTION` trỏ tới một database SQL Server dành riêng cho test và chạy `dotnet test AIVES.Tests.Data`.

## Chạy VieNeu-TTS

Cài `uv`, rồi lấy mã nguồn VieNeu vào thư mục dự án.

```powershell
git clone https://github.com/pnnbao97/VieNeu-TTS.git VieNeu-TTS
```

Ứng dụng tự khởi động VieNeu khi `Tts:AutoStart` bật. Có thể chạy thủ công từ thư mục `VieNeu-TTS` bằng `uv run python -m apps.openai_speech`. Mặc định dùng CPU và cổng 8000. Lần đầu VieNeu có thể cần tải mô hình. Đặt `Tts:UseGpu` thành `true` nếu đã có môi trường CUDA phù hợp.

Admin và giảng viên mở trang **Giọng đọc** để nghe thử và gán giọng cho môn. Admin có thêm thao tác clone và xóa giọng. Xóa giọng bỏ mẫu thu trong SQL và đưa các môn đang dùng giọng đó về mặc định. VieNeu giữ bản đã nạp trong bộ nhớ cho tới khi tiến trình VieNeu khởi động lại.

## Mini-task · Giới hạn thời gian kiểm tra trạng thái

Đặt `Tts:AutoStart=false` và trỏ `Tts:BaseUrl` tới một máy chủ thử nghiệm nhận yêu cầu `GET /health` nhưng giữ kết nối lâu hơn 10 giây. Khi mở trang **Giọng đọc**, yêu cầu kiểm tra trạng thái phải hết hạn sau khoảng 10 giây. Trang hiển thị **Chưa chạy**, vẫn liệt kê được giọng clone trong SQL và khóa form clone. Khi endpoint trả HTTP 200 với `status: "ok"`, tải lại trang để kiểm tra trạng thái chuyển sang **Đang chạy**.
