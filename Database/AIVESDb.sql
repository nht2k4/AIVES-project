/* =========================================================
   AIVES - Nhóm 4 - Chức năng 7: Quản trị hệ thống
   Database First: chạy script này trong SSMS TRƯỚC khi chạy app
   ========================================================= */
USE master;
GO
IF DB_ID(N'AIVESDb') IS NOT NULL
BEGIN
    ALTER DATABASE AIVESDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE AIVESDb;
END
GO
CREATE DATABASE AIVESDb;
GO
USE AIVESDb;
GO

/* ---------- Bảng ---------- */
CREATE TABLE Accounts (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Accounts PRIMARY KEY,
    FullName      NVARCHAR(100)     NOT NULL,
    Email         VARCHAR(150)      NOT NULL CONSTRAINT UQ_Accounts_Email UNIQUE,
    PasswordHash  VARCHAR(256)      NOT NULL,
    Role          VARCHAR(20)       NOT NULL CONSTRAINT CK_Accounts_Role CHECK (Role IN ('Admin', 'Lecturer')),
    IsActive      BIT               NOT NULL,
    CreatedAt     DATETIME2         NOT NULL CONSTRAINT DF_Accounts_CreatedAt DEFAULT SYSUTCDATETIME()
);
GO

CREATE TABLE Subjects (
    Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Subjects PRIMARY KEY,
    Code               VARCHAR(20)       NOT NULL CONSTRAINT UQ_Subjects_Code UNIQUE,
    Name               NVARCHAR(150)     NOT NULL,
    IsActive           BIT               NOT NULL,
    SttLanguage        VARCHAR(10)       NOT NULL CONSTRAINT CK_Subjects_SttLanguage CHECK (SttLanguage IN ('vi-VN', 'en-US')),
    TtsLanguage        VARCHAR(10)       NOT NULL CONSTRAINT CK_Subjects_TtsLanguage CHECK (TtsLanguage IN ('vi-VN', 'en-US')),
    LanguageUpdatedAt  DATETIME2         NULL,
    LanguageUpdatedBy  VARCHAR(150)      NULL
);
GO

CREATE TABLE LecturerSubjects (
    LecturerId  INT        NOT NULL,
    SubjectId   INT        NOT NULL,
    AssignedAt  DATETIME2  NOT NULL CONSTRAINT DF_LecturerSubjects_AssignedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_LecturerSubjects PRIMARY KEY (LecturerId, SubjectId),
    CONSTRAINT FK_LecturerSubjects_Accounts FOREIGN KEY (LecturerId) REFERENCES Accounts(Id) ON DELETE CASCADE,
    CONSTRAINT FK_LecturerSubjects_Subjects FOREIGN KEY (SubjectId) REFERENCES Subjects(Id) ON DELETE CASCADE
);
GO

/* ---------- Dữ liệu mẫu ----------
   Mật khẩu đã băm PBKDF2-SHA256 (100.000 vòng), cùng định dạng với Pbkdf2PasswordHasher
   admin@fu.edu.vn   / Admin@123
   an.nv@fu.edu.vn   / Lecturer@123
   binh.tt@fu.edu.vn / Lecturer@123
*/
INSERT INTO Accounts (FullName, Email, PasswordHash, Role, IsActive) VALUES
(N'Quản trị hệ thống', 'admin@fu.edu.vn',   '100000.uSRX8Si9+0CW3kmVg4f2+A==.MZn9CqsOUDr/aDtD0lfZynz0ovwimrQOiyYlxU3CFOE=', 'Admin',    1),
(N'Nguyễn Văn An',     'an.nv@fu.edu.vn',   '100000.aVHHCR5cg5+0AU+lX4mHHA==.+hubtA5WNK07tC/mMFfOkFw9UC644hUi1xpeZR9+nMU=', 'Lecturer', 1),
(N'Trần Thị Bình',     'binh.tt@fu.edu.vn', '100000.aVHHCR5cg5+0AU+lX4mHHA==.+hubtA5WNK07tC/mMFfOkFw9UC644hUi1xpeZR9+nMU=', 'Lecturer', 1);
GO

INSERT INTO Subjects (Code, Name, IsActive, SttLanguage, TtsLanguage) VALUES
('PRN222',  N'Advanced Cross-Platform Application Programming With .NET', 1, 'vi-VN', 'vi-VN'),
('SWP391',  N'Software Development Project',                             1, 'vi-VN', 'vi-VN'),
('ENW492c', N'Writing Research Papers',                                  1, 'en-US', 'en-US'),
('SSG104',  N'Communication and In-Group Working Skills',                1, 'vi-VN', 'vi-VN'),
('PRN211',  N'Basic Cross-Platform Application Programming With .NET',   0, 'vi-VN', 'vi-VN');
GO

INSERT INTO LecturerSubjects (LecturerId, SubjectId)
SELECT a.Id, s.Id FROM Accounts a JOIN Subjects s ON a.Email = 'an.nv@fu.edu.vn' AND s.Code = 'PRN222'
UNION ALL
SELECT a.Id, s.Id FROM Accounts a JOIN Subjects s ON a.Email = 'binh.tt@fu.edu.vn' AND s.Code = 'ENW492c';
GO
