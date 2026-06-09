# EduHealth-BE

He thong quan ly suc khoe hoc sinh.

## Cau hinh ung dung

Sau khi clone hoac pull source code, tao file cau hinh local tu file mau:

```powershell
Copy-Item EduHealth/appsettings.example.json EduHealth/appsettings.json
```

Sau do mo `EduHealth/appsettings.json` va thay cac gia tri `YOUR_*` bang thong tin cua ban.

Khong commit `appsettings.json` hoac `appsettings.Development.json` vi cac file nay co the chua mat khau, API key va cac thong tin nhay cam. Khi them mot muc cau hinh moi, hay cap nhat ca `appsettings.example.json` nhung chi dung gia tri mau.
