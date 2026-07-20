# PortScanner (WinForms)

Ứng dụng quét cổng TCP đa luồng, build bằng `vbc.exe` trực tiếp (không cần Visual Studio), tương thích .NET Framework 4.x / VB 2012.

## Cấu trúc file

- `PortScanner.vb` — lớp logic quét (đa host, ping, đo thời gian phản hồi, event-based).
- `ServicePorts.vb` — bảng tra tên dịch vụ theo cổng + danh sách preset "cổng phổ biến".
- `ScanModels.vb` — class `OpenPortResult` dùng chung cho ListView và xuất file.
- `PortScannerForm.vb` — giao diện WinForms, control tạo hoàn toàn bằng code.
- `Program.vb` — điểm vào `Sub Main`.
- `setup_libs_PortScanner.bat` / `build_PortScanner.bat` — kiểm tra môi trường & biên dịch.

## Cách build

```
setup_libs_PortScanner.bat
build_PortScanner.bat
```

File chạy: `bin\PortScanner.exe`

## Tính năng mới thêm

1. **Nhận diện dịch vụ theo cổng + preset dải cổng**
   - `ServicePorts.vb` chứa bảng ~90 cổng thường gặp (HTTP, SSH, FTP, MySQL, RDP, Redis, MongoDB...).
   - ComboBox "Dải cổng" có 4 lựa chọn: Tùy chỉnh, Cổng phổ biến (~90 cổng), Well-known (1-1024), Toàn bộ (1-65535).
   - Khi chọn "Cổng phổ biến", `PortScanner.PortsList` được gán danh sách cổng rời rạc thay vì quét liên tục min..max.

2. **Xuất kết quả ra file + ListView riêng cho cổng mở**
   - `ListView` hiển thị: Host, Cổng, Dịch vụ, Thời gian phản hồi (ms), Tiêu đề Web, Banner rút gọn.
   - Nút "Xuất kết quả (CSV/TXT)": CSV xuất đầy đủ dữ liệu có escape đúng chuẩn (dấu phẩy, dấu nháy kép);
     TXT xuất toàn bộ log màu dạng văn bản thuần.

3. **Ping kiểm tra host sống trước khi quét + đo thời gian phản hồi**
   - Trước khi quét cổng, mỗi host được ping (timeout tối đa 2s). Kết quả hiển thị màu cyan/cam trong log.
   - **Quan trọng:** ping không phản hồi KHÔNG chặn việc quét cổng tiếp theo, vì nhiều hệ thống chặn ICMP
     nhưng vẫn mở cổng TCP bình thường (đây là hành vi giống các scanner thật, tương tự cờ `-Pn` của nmap).
   - Mỗi cổng mở đều đo thời gian kết nối bằng `Stopwatch`, hiển thị cột "Phản hồi (ms)".

4. **Quét nhiều host / dải IP cùng lúc**
   - Ô nhập Host hỗ trợ danh sách phân cách bởi dấu phẩy: `host1, host2, 192.168.1.1-192.168.1.20`.
   - Dải IP dạng đầy đủ (`192.168.1.1-192.168.1.20`) hoặc rút gọn octet cuối (`192.168.1.1-50`).
   - Giới hạn an toàn 1024 địa chỉ/dải để tránh treo máy khi nhập nhầm dải quá lớn.
   - Toàn bộ cặp (host, port) được gộp vào 1 hàng đợi dùng chung (`ConcurrentQueue`) cho các luồng lấy việc.

## Các sửa lỗi từ bản console gốc (vẫn giữ nguyên từ lần chuyển đổi trước)

1. Hàm lấy cổng kế tiếp gốc không kiểm tra `max` → quét lố ra ngoài khoảng. Nay dùng hàng đợi rõ ràng, dừng đúng khi hết việc.
2. `BannerGrab` gốc ghi sai `"Connection: Closernrn"` (chữ "rn" thay vì xuống dòng thật) → đã sửa thành `vbCrLf`.
3. `CheckPort` dùng `Using` đảm bảo giải phóng `TcpClient` dù thành công hay lỗi.

## Giao diện

- Nhập Host/IP (hỗ trợ nhiều host, dải IP), chọn dải cổng, timeout, số luồng, bật/tắt ping.
- Nút **Bắt đầu quét** / **Dừng**.
- Thanh tiến trình theo tổng số cặp (host, port) sẽ quét.
- `ListView` liệt kê từng cổng mở kèm dịch vụ/thời gian phản hồi/tiêu đề web/banner.
- Nút **Xuất kết quả** xuất CSV hoặc TXT.
- Khung log màu: cyan = ping, xanh lá = cổng mở, vàng = banner, tím = có thể có dịch vụ, đỏ = lỗi.

## Ghi chú tương thích VB 2012 / .NET FW 4.x

- Không dùng string interpolation (`$"..."`) hay multi-line string literal.
- Dùng object initializer `New Button() With {...}` (hỗ trợ từ VB 2010).
- Dùng `System.Collections.Concurrent.ConcurrentQueue` (có sẵn từ .NET 4.0).
- Không dùng `Async`/`Await` — vẫn theo mô hình `Thread` + `BeginConnect`/`EndConnect`.
