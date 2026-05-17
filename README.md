# 🚀 ERP — Hệ thống Quản lý Kế toán

> **ERP code, chạy là thấy, đóng gói xịn sò.**  
> Full-stack ERP system cho doanh nghiệp bán sản phẩm, xây bằng .NET 8 Web API + React + SQLite.

---

## 🗂️ Cấu trúc dự án

```
coreNet/
├── ErpBackend/          # .NET 10 Web API
│   ├── Controllers/     # API endpoints
│   ├── Data/            # DbContext (SQLite + EF Core)
│   ├── Entities/        # Domain models
│   ├── Repositories/    # Repository pattern
│   ├── Dtos/            # Request/Response DTOs
│   ├── Migrations/      # EF Core migrations (auto-applied)
│   └── logs/            # Serilog daily log files (tự tạo)
│
├── ErpFrontend/         # React + Vite + Tailwind CSS v4
│   ├── src/api/         # Axios + JWT interceptor
│   ├── src/context/     # AuthContext
│   ├── src/components/  # Layout (Sidebar, Topbar)
│   └── src/pages/       # Login, Dashboard, Products, Invoice, Reports
│
└── docker-compose.yml   # Chạy cả stack bằng 1 lệnh
```

---

## ⚡ Cách Chạy Nhanh (Dev Mode)

### 1. Backend (.NET API)

```powershell
# PowerShell (Windows)
cd ErpBackend


```

> API chạy tại: **http://localhost:5013**  
> Database SQLite (`app.db`) và `logs/` tự động tạo khi lần đầu khởi động.

### 2. Frontend (React)

```powershell
# Mở terminal MỚI (không dùng terminal đang chạy backend)
cd ErpFrontend
npm install   # Chỉ cần chạy lần đầu
npm run dev
```

> Frontend chạy tại: **http://localhost:5173**

> ⚡ **Lưu ý PowerShell**: Không dùng `&&` để nối lệnh.  
> Dùng `;` hoặc mở 2 terminal riêng biệt chạy song song.

---

## 🐳 Chạy bằng Docker (Production Mode)

> Yêu cầu: Docker Desktop đang chạy

```bash
# Build và khởi động toàn bộ stack
docker-compose up --build

# Dừng
docker-compose down
```

| Service   | URL                        |
|-----------|----------------------------|
| Frontend  | http://localhost:3000      |
| API       | http://localhost:5013      |
| Swagger   | http://localhost:5013/swagger | 

---

## 📖 API Documentation

Swagger UI tự động sinh từ code, truy cập khi API đang chạy:

👉 **http://localhost:5013/swagger/index.html**

### Tổng quan Endpoints

| Module         | Method | Endpoint                              | Quyền              |
|----------------|--------|---------------------------------------|--------------------|
| **Auth**       | POST   | `/api/auth/login`                     | Public             |
|                | POST   | `/api/auth/register`                  | Admin              |
| **Products**   | GET    | `/api/products`                       | Public             |
|                | POST   | `/api/products`                       | Public             |
|                | DELETE | `/api/products/{id}`                  | Admin              |
| **Invoices**   | GET    | `/api/invoices`                       | Public             |
|                | POST   | `/api/invoices`                       | Sales, Admin       |
|                | POST   | `/api/invoices/{id}/payments`         | Any logged-in      |
| **Stock**      | POST   | `/api/stock/import`                   | Public             |
|                | GET    | `/api/stock/low-stock`                | Public             |
| **Debt**       | GET    | `/api/debt/overview`                  | Public             |
|                | GET    | `/api/debt/aging`                     | Public             |
| **Reports**    | GET    | `/api/reports/revenue`                | Accountant, Admin  |
|                | GET    | `/api/reports/profit`                 | Accountant, Admin  |
|                | GET    | `/api/reports/top-products`           | Accountant, Admin  |
|                | GET    | `/api/reports/export/excel`           | Accountant, Admin  |
| **Health**     | GET    | `/api/health`                         | Public             |

---

## 👤 Tài khoản mặc định (Seed Data)

| Username      | Password | Role        | Quyền đặc biệt                         |
|---------------|----------|-------------|----------------------------------------|
| `admin`       | `123`    | Admin       | Toàn quyền, xóa sản phẩm, tạo user    |
| `sales`       | `123`    | Sales       | Tạo hóa đơn, xem tồn kho              |
| `accountant`  | `123`    | Accountant  | Xem báo cáo, export Excel              |

> ⚠️ **Lưu ý Production**: Đổi mật khẩu và JWT Secret trước khi deploy!

---

## 🔐 Cách dùng JWT Authentication

### Bước 1: Lấy token
```bash
curl -X POST http://localhost:5013/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "123"}'
```

### Bước 2: Gắn token vào request
```bash
curl http://localhost:5013/api/reports/revenue \
  -H "Authorization: Bearer <token_ở_đây>"
```

---

## 📊 Tính năng nổi bật

- ✅ **Bán hàng**: Tạo hóa đơn, tự động sinh mã HD001/HD002..., trừ tồn kho tức thì
- ✅ **Công nợ**: Theo dõi thanh toán từng phần, phân tích nợ theo độ tuổi (30/60 ngày)
- ✅ **Kho hàng**: Nhập/xuất kho có ghi log đầy đủ, cảnh báo tồn kho thấp
- ✅ **Báo cáo**: Doanh thu, lợi nhuận, top sản phẩm bán chạy
- ✅ **Export Excel**: Tải báo cáo .xlsx bằng 1 click
- ✅ **Logging**: Serilog ghi log ra file hàng ngày (tự xoay vòng 7 ngày)
- ✅ **JWT Auth**: Phân quyền theo role (Admin/Sales/Accountant)
- ✅ **Docker**: Đóng gói toàn bộ stack bằng docker-compose

---

## 🛠️ Tech Stack

| Layer       | Công nghệ                                          |
|-------------|-----------------------------------------------------|
| Backend     | .NET 10 Web API, EF Core, SQLite, Serilog, EPPlus  |
| Frontend    | React 18, Vite, Tailwind CSS v4, Axios, Lucide      |
| Auth        | JWT Bearer Tokens                                   |
| Deployment  | Docker + Nginx (frontend), .NET runtime (backend)   |

---

## 🚀 Deploy lên Free Hosting

### API → Render.com
1. Push code lên GitHub
2. Vào [render.com](https://render.com) → New Web Service → Connect repo
3. Build Command: `dotnet publish -c Release -o out`
4. Start Command: `dotnet out/ErpBackend.dll`
5. Thêm Environment Variables: `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`

### Frontend → Vercel
1. Push `ErpFrontend/` lên GitHub
2. Vào [vercel.com](https://vercel.com) → Import → chọn repo
3. Framework: **Vite** (tự detect)
4. Cập nhật `src/api/axios.ts` → đổi `baseURL` sang URL của Render

---

*Built with ❤️ and ERP Coding energy — April 2026*
