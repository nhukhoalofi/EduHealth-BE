# EduHealth API checkpoints (seeded)

## Base
- Base URL: `https://localhost:7012`
- All endpoints are prefixed with: `/api/v1`
- Auth header: `Authorization: Bearer {accessToken}`

## Seed accounts
- ADMIN
  - username: `admin`
  - password: `123456Aa@`
- NURSE
  - username: `nurse01`
  - password: `123456`
- STUDENT
  - username: `HS001`
  - password: `123456`

> Notes
> - `UsersController`, `MedicinesController`, `ExaminationsController` return `ApiResponseV2`.
> - `StudentsController`, `NotificationsController`, `AuthController` return `ApiResponse`.

---

## 1) Auth

### 1.1 Login
- `POST /api/v1/auth/login`
- Body
```json
{
  "identifier": "admin",
  "password": "123456Aa@"
}
```
- Response (200)
```json
{
  "success": true,
  "message": "Đăng nhập thành công.",
  "data": {
    "userId": 1,
    "username": "admin",
    "fullName": "System Admin",
    "role": "ADMIN",
    "avatar": null,
    "accessToken": "<jwt>",
    "expiresAt": "2026-..."
  }
}
```

### 1.2 Me
- `GET /api/v1/auth/me`
- Auth: any role

### 1.3 Request OTP (forgot password)
- `POST /api/v1/auth/forgot-password/request-otp`
- Body
```json
{
  "email": "hs001@eduhealth.local"
}
```
- Response
  - 200: `OTP đã được gửi.`
  - 400: `Email không tồn tại trong hệ thống hoặc tài khoản đã bị khóa.`

### 1.4 Verify OTP
- `POST /api/v1/auth/forgot-password/verify-otp`
- Body
```json
{
  "email": "hs001@eduhealth.local",
  "otp": "123456"
}
```

### 1.5 Reset password
- `POST /api/v1/auth/forgot-password/reset`
- Body
```json
{
  "email": "hs001@eduhealth.local",
  "resetToken": "<from verify otp>",
  "newPassword": "NewPass123@"
}
```

### 1.6 Change password
- `POST /api/v1/auth/change-password`
- Auth: any role
- Body
```json
{
  "oldPassword": "123456",
  "newPassword": "NewPass123@"
}
```

---

## 2) Users (ADMIN only)

### 2.1 List
- `GET /api/v1/users?page=1&pageSize=20&keyword=&role=&status=`

### 2.2 Create
- `POST /api/v1/users`
- Body
```json
{
  "username": "user_test_01",
  "fullName": "User Test 01",
  "email": "user_test_01@eduhealth.local",
  "phone": "0900000099",
  "role": "NURSE",
  "password": "123456"
}
```

### 2.3 Detail
- `GET /api/v1/users/{code}` (example: `USR001`)

### 2.4 Update
- `PATCH /api/v1/users/{code}`
- Body
```json
{
  "fullName": "User Test 01 Updated",
  "email": "user_test_01@eduhealth.local",
  "phone": "0900000099",
  "avatar": null
}
```

### 2.5 Update status
- `PATCH /api/v1/users/{code}/status`
- Body
```json
{
  "status": "ACTIVE",
  "isActive": true
}
```

### 2.6 Reset password
- `POST /api/v1/users/{code}/reset-password`
- Body
```json
{
  "newPassword": "123456"
}
```

---

## 3) Students

### 3.1 List (ADMIN, NURSE)
- `GET /api/v1/students?page=1&pageSize=10&keyword=&classId=&gender=`

### 3.2 Create (ADMIN)
- `POST /api/v1/students`
- Body
```json
{
  "userId": 0,
  "classId": 1,
  "fullName": "Student New",
  "dateOfBirth": "2016-09-12",
  "currentHeight": 130,
  "currentWeight": 30.1,
  "guardian": "Phụ huynh",
  "phone": "0900000101",
  "medicalHistoryNotes": ""
}
```

### 3.3 Detail (ADMIN, NURSE)
- `GET /api/v1/students/{id:int}`

### 3.4 Update (NURSE)
- `PATCH /api/v1/students/{id:int}`

### 3.5 Delete (ADMIN)
- `DELETE /api/v1/students/{id:int}`

### 3.6 Import (NURSE)
- `POST /api/v1/students/import`
- `multipart/form-data`
  - `file`: excel/csv (the exact format depends on your `StudentImportRequestDto`)

---

## 4) Medicines

### 4.1 List (NURSE, ADMIN)
- `GET /api/v1/medicines?page=1&pageSize=20&keyword=&status=`

### 4.2 Create (NURSE)
- `POST /api/v1/medicines`
- Body
```json
{
  "name": "Ibuprofen 200mg",
  "activeIngredient": "Ibuprofen",
  "unit": "VIEN",
  "packaging": "Hộp 10 vỉ",
  "warningThreshold": 20,
  "stockQuantity": 100,
  "note": "Giảm đau",
  "status": "ACTIVE"
}
```

### 4.3 Detail (NURSE, ADMIN)
- `GET /api/v1/medicines/{code}` (example: `MED001`)

### 4.4 Update (NURSE)
- `PATCH /api/v1/medicines/{code}`

### 4.5 Update status (NURSE, ADMIN)
- `PATCH /api/v1/medicines/{code}/status`

### 4.6 Stock in (NURSE)
- `POST /api/v1/medicines/{code}/stock-in`
- Body
```json
{
  "quantity": 10,
  "reason": "Nhập kho",
  "expiryDate": "2027-12-31",
  "batchNumber": "BATCH-001",
  "note": "Seed test"
}
```

### 4.7 Dispose (NURSE)
- `POST /api/v1/medicines/{code}/dispose`

### 4.8 Movements (NURSE, ADMIN)
- `GET /api/v1/medicines/{code}/movements?page=1&pageSize=20&type=&fromDate=&toDate=`

### 4.9 Alerts (NURSE, ADMIN)
- `GET /api/v1/medicines/alerts?type=LOW_STOCK`

---

## 5) Examinations

### 5.1 List (NURSE, ADMIN)
- `GET /api/v1/examinations?page=1&pageSize=20&keyword=&studentCode=&classCode=&fromDate=&toDate=`

### 5.2 Create (NURSE)
- `POST /api/v1/examinations`
- Body
```json
{
  "studentCode": "STD001",
  "visitDate": "2026-04-04T00:00:00Z",
  "symptoms": "Sốt nhẹ",
  "diseaseCode": "DIS001",
  "diagnosis": "Cảm cúm",
  "treatment": "Nghỉ ngơi",
  "note": "Test create examination",
  "prescriptions": [
    { "medicineCode": "MED001", "quantity": 2, "usageIns": "1 viên/lần, 2 lần/ngày" }
  ]
}
```

### 5.3 Detail (NURSE, ADMIN)
- `GET /api/v1/examinations/{code}` (example: `VIS001`)

---

## 6) Notifications (NURSE)

### 6.1 Preview recipients
- `POST /api/v1/notifications/recipients/preview`
- Body
```json
{
  "userIds": [1,2],
  "classId": null
}
```

### 6.2 Create notification
- `POST /api/v1/notifications`
- Body
```json
{
  "title": "Thông báo test",
  "content": "Nội dung test",
  "type": "GENERAL",
  "recipientUserIds": [1,2],
  "classId": null,
  "diseaseId": null,
  "vaccinationId": null
}
```

### 6.3 Mark read
- `PATCH /api/v1/notifications/{notificationId:int}/read`
- Auth: any logged-in user
