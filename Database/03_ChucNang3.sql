/* =========================================================
   AIVES - 03: Chức năng 3 (Lõi phỏng vấn AI) + vai trò Sinh viên + giọng đọc. Chỉ Ass3.
   Chạy SAU 01 và 02. Idempotent.
   ========================================================= */
USE AIVESDb;
GO

/* Biên bản phỏng vấn: mỗi dòng là một lượt hỏi (câu chính hoặc câu hỏi xoáy) và câu trả lời đã chuyển thành văn bản */
IF OBJECT_ID(N'InterviewTurns') IS NULL
CREATE TABLE InterviewTurns (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InterviewTurns PRIMARY KEY,
    ParticipantId INT               NOT NULL,
    QuestionId    INT               NOT NULL,
    Kind          VARCHAR(10)       NOT NULL CONSTRAINT CK_InterviewTurns_Kind CHECK (Kind IN ('Main', 'FollowUp')),
    Content       NVARCHAR(1000)    NOT NULL,
    Reason        NVARCHAR(500)     NULL,
    Answer        NVARCHAR(MAX)     NULL,
    AskedAt       DATETIME2         NOT NULL,
    AnsweredAt    DATETIME2         NULL,
    TimedOut      BIT               NOT NULL CONSTRAINT DF_InterviewTurns_TimedOut DEFAULT 0,
    CONSTRAINT FK_InterviewTurns_Participants FOREIGN KEY (ParticipantId) REFERENCES ExamParticipants(Id) ON DELETE CASCADE,
    CONSTRAINT FK_InterviewTurns_Questions FOREIGN KEY (QuestionId) REFERENCES Questions(Id)
);
GO

/* =========================================================
   Vai trò Sinh viên: tài khoản có StudentCode khớp với MSSV trong danh sách phiên thi
   ========================================================= */
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('Accounts', 'StudentCode') IS NULL
    ALTER TABLE Accounts ADD StudentCode VARCHAR(30) NULL;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Accounts_Role' AND (definition NOT LIKE '%Student%' OR definition NOT LIKE '%Pending%'))
BEGIN
    ALTER TABLE Accounts DROP CONSTRAINT CK_Accounts_Role;
    ALTER TABLE Accounts ADD CONSTRAINT CK_Accounts_Role CHECK (Role IN ('Admin', 'Lecturer', 'Student', 'Pending'));
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Accounts_StudentCode')
    CREATE UNIQUE INDEX UQ_Accounts_StudentCode ON Accounts (StudentCode) WHERE StudentCode IS NOT NULL;
GO

/* Tài khoản sinh viên mẫu: sv.a@fu.edu.vn / Student@123, MSSV SE180001 */
IF NOT EXISTS (SELECT 1 FROM Accounts WHERE Email = 'sv.a@fu.edu.vn')
    INSERT INTO Accounts (FullName, Email, PasswordHash, Role, IsActive, StudentCode)
    VALUES (N'Nguyễn Văn A', 'sv.a@fu.edu.vn', '100000.bhe8jteDTPLA1QMnp8+N/A==.vrsScOF3bxc5hi9vlXdNRj3fBVRaFv7+oI0GLYMN73U=', 'Student', 1, 'SE180001');
GO

/* =========================================================
   Giọng đọc VieNeu-TTS: giọng riêng theo môn + thư viện giọng đã clone
   (mẫu thu được lưu ở đây làm bản dự phòng: nếu VieNeu mất giọng, ví dụ cài lại hoặc đổi máy, ứng dụng tự nạp lại từ đây)
   ========================================================= */
IF COL_LENGTH('Subjects', 'TtsVoice') IS NULL
    ALTER TABLE Subjects ADD TtsVoice NVARCHAR(64) NULL;
GO

/* =========================================================
   Thông số STT/AI chỉnh tay theo môn (trang Ngôn ngữ STT/TTS của chức năng 7):
   - SttTerms: từ điển thuật ngữ, mỗi dòng "cách STT hay nghe sai => cách viết đúng" (độ chính xác STT tiếng Việt cho thuật ngữ chuyên ngành)
   - AiTimeoutSeconds: thời gian chờ AI tối đa, quá thì dùng luật dự phòng (giữ nhịp vấn đáp)
   - UseExternalAi: 0 = không gửi câu trả lời của sinh viên ra dịch vụ AI bên ngoài (quyền riêng tư)
   ========================================================= */
IF COL_LENGTH('Subjects', 'SttTerms') IS NULL
    ALTER TABLE Subjects ADD SttTerms NVARCHAR(2000) NULL;
GO

IF COL_LENGTH('Subjects', 'AiTimeoutSeconds') IS NULL
    ALTER TABLE Subjects ADD AiTimeoutSeconds INT NOT NULL
        CONSTRAINT DF_Subjects_AiTimeoutSeconds DEFAULT 8
        CONSTRAINT CK_Subjects_AiTimeoutSeconds CHECK (AiTimeoutSeconds BETWEEN 2 AND 30);
GO

IF COL_LENGTH('Subjects', 'UseExternalAi') IS NULL
    ALTER TABLE Subjects ADD UseExternalAi BIT NOT NULL CONSTRAINT DF_Subjects_UseExternalAi DEFAULT 1;
GO

/* Từ điển mẫu cho PRN222 (chỉ đặt khi môn chưa có từ điển) */
UPDATE Subjects SET SttTerms = N'ra dơ pây => Razor Pages
rây dơ pây => Razor Pages
đi ai => DI
ây pi ai => API
en ti ti phờ rêm uốc => Entity Framework
ét kiu en => SQL'
WHERE Code = 'PRN222' AND SttTerms IS NULL;
GO

IF OBJECT_ID(N'Voices') IS NULL
CREATE TABLE Voices (
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Voices PRIMARY KEY,
    Name        NVARCHAR(64)      NOT NULL CONSTRAINT UQ_Voices_Name UNIQUE,
    Description NVARCHAR(64)      NULL,
    FileName    VARCHAR(100)      NOT NULL,
    Denoise     BIT               NOT NULL CONSTRAINT DF_Voices_Denoise DEFAULT 1,
    Clip        VARBINARY(MAX)    NOT NULL,
    CreatedBy   INT               NOT NULL,
    CreatedAt   DATETIME2         NOT NULL CONSTRAINT DF_Voices_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Voices_Accounts FOREIGN KEY (CreatedBy) REFERENCES Accounts(Id)
);
GO
