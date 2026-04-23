Write-Host "🚀 Đang dọn dẹp và khởi động ERP.Vibe (Development Mode)..." -ForegroundColor Cyan

# Tắt các process cũ để tránh lỗi "Address already in use"
taskkill /F /IM ErpBackend.exe /T 2>$null
taskkill /F /IM dotnet.exe /T 2>$null

# Khởi động Backend trong một cửa sổ mới
Write-Host "1. Đang mở Backend (Port 5013)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd ErpBackend; dotnet run"

# Khởi động Frontend trong một cửa sổ mới
Write-Host "2. Đang mở Frontend (Port 5173)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd ErpFrontend; npm run dev"

Write-Host "-------------------------------------------" -ForegroundColor Cyan
Write-Host "✅ Xong! Hai cửa sổ terminal mới đã được mở." -ForegroundColor Green
Write-Host "👉 Backend: http://localhost:5013"
Write-Host "👉 Frontend: http://localhost:5173"
Write-Host "-------------------------------------------" -ForegroundColor Cyan
