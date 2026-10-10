/* =========================================================
   AIVES - Chức năng 2 (Quản lý kỳ thi & lịch thi) + Chức năng 3 (Lõi phỏng vấn AI)
   Chạy SAU AIVESDb.sql. Idempotent: chạy lại nhiều lần không mất dữ liệu.
   ========================================================= */
USE AIVESDb;
GO

/* Ngân hàng câu hỏi theo môn. ExpectedPoints = các ý chính (ngăn cách bằng dấu ; hoặc xuống dòng) để AI biết câu trả lời còn thiếu ý nào */
IF OBJECT_ID(N'Questions') IS NULL
CREATE TABLE Questions (
    Id             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Questions PRIMARY KEY,
    SubjectId      INT               NOT NULL,
    Content        NVARCHAR(1000)    NOT NULL,
    ExpectedPoints NVARCHAR(1000)    NULL,
    IsActive       BIT               NOT NULL CONSTRAINT DF_Questions_IsActive DEFAULT 1,
    CONSTRAINT FK_Questions_Subjects FOREIGN KEY (SubjectId) REFERENCES Subjects(Id) ON DELETE CASCADE
);
GO

/* Phiên thi vấn đáp: số câu chính, số câu đào sâu tối đa (mỗi thí sinh / mỗi câu), thời gian trả lời */
IF OBJECT_ID(N'ExamSessions') IS NULL
CREATE TABLE ExamSessions (
    Id                       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExamSessions PRIMARY KEY,
    SubjectId                INT               NOT NULL,
    Title                    NVARCHAR(150)     NOT NULL,
    StartTime                DATETIME2         NOT NULL,
    SlotMinutes              INT               NOT NULL CONSTRAINT CK_ExamSessions_Slot CHECK (SlotMinutes BETWEEN 1 AND 120),
    MainQuestionCount        INT               NOT NULL CONSTRAINT CK_ExamSessions_Main CHECK (MainQuestionCount BETWEEN 1 AND 20),
    MaxFollowUps             INT               NOT NULL CONSTRAINT CK_ExamSessions_FU CHECK (MaxFollowUps BETWEEN 0 AND 40),
    MaxFollowUpsPerQuestion  INT               NOT NULL CONSTRAINT CK_ExamSessions_FUQ CHECK (MaxFollowUpsPerQuestion BETWEEN 0 AND 5),
    AnswerSeconds            INT               NOT NULL CONSTRAINT CK_ExamSessions_Ans CHECK (AnswerSeconds BETWEEN 10 AND 600),
    Status                   VARCHAR(10)       NOT NULL CONSTRAINT CK_ExamSessions_Status CHECK (Status IN ('Open', 'Closed')),
    CreatedBy                INT               NOT NULL,
    CreatedAt                DATETIME2         NOT NULL CONSTRAINT DF_ExamSessions_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ExamSessions_Subjects FOREIGN KEY (SubjectId) REFERENCES Subjects(Id),
    CONSTRAINT FK_ExamSessions_Accounts FOREIGN KEY (CreatedBy) REFERENCES Accounts(Id)
);
GO

/* Danh sách sinh viên của phiên thi, mỗi người một khung giờ */
IF OBJECT_ID(N'ExamParticipants') IS NULL
CREATE TABLE ExamParticipants (
    Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExamParticipants PRIMARY KEY,
    SessionId    INT               NOT NULL,
    StudentCode  VARCHAR(30)       NOT NULL,
    FullName     NVARCHAR(100)     NOT NULL,
    SlotStart    DATETIME2         NOT NULL,
    SlotEnd      DATETIME2         NOT NULL,
    Status       VARCHAR(15)       NOT NULL CONSTRAINT CK_ExamParticipants_Status CHECK (Status IN ('Scheduled', 'InProgress', 'Completed')),
    CONSTRAINT UQ_ExamParticipants_Student UNIQUE (SessionId, StudentCode),
    CONSTRAINT FK_ExamParticipants_Sessions FOREIGN KEY (SessionId) REFERENCES ExamSessions(Id) ON DELETE CASCADE
);
GO

/* Bộ câu hỏi chính hệ thống chọn cho từng thí sinh */
IF OBJECT_ID(N'ParticipantQuestions') IS NULL
CREATE TABLE ParticipantQuestions (
    ParticipantId INT NOT NULL,
    QuestionId    INT NOT NULL,
    OrderNo       INT NOT NULL,
    CONSTRAINT PK_ParticipantQuestions PRIMARY KEY (ParticipantId, QuestionId),
    CONSTRAINT FK_ParticipantQuestions_Participants FOREIGN KEY (ParticipantId) REFERENCES ExamParticipants(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ParticipantQuestions_Questions FOREIGN KEY (QuestionId) REFERENCES Questions(Id)
);
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

/* ---------- Dữ liệu mẫu: câu hỏi cho PRN222 và ENW492c (chỉ thêm khi bảng còn trống) ---------- */
IF NOT EXISTS (SELECT 1 FROM Questions)
BEGIN
    DECLARE @prn INT = (SELECT Id FROM Subjects WHERE Code = 'PRN222');
    DECLARE @enw INT = (SELECT Id FROM Subjects WHERE Code = 'ENW492c');

    INSERT INTO Questions (SubjectId, Content, ExpectedPoints) VALUES
    (@prn, N'Hãy giải thích kiến trúc 3 lớp (3-layer) và vai trò của từng lớp.',            N'Presentation;Business Logic;Data Access;phụ thuộc một chiều'),
    (@prn, N'Razor Pages khác gì so với ASP.NET Core MVC? Khi nào nên chọn Razor Pages?',     N'PageModel;mỗi trang một handler;không cần Controller;routing theo thư mục'),
    (@prn, N'Dependency Injection là gì và ASP.NET Core đăng ký service như thế nào?',        N'AddScoped;AddSingleton;AddTransient;constructor injection'),
    (@prn, N'Entity Framework Core hoạt động theo hướng Database First như thế nào?',        N'Scaffold-DbContext;DbContext;Entity;DbSet'),
    (@prn, N'Giải thích cơ chế Cookie Authentication và phân quyền theo vai trò.',            N'Claims;SignIn;Cookie;Authorize Roles'),
    (@prn, N'Repository pattern dùng để làm gì? Nó nằm ở lớp nào trong kiến trúc 3 lớp?',    N'tách truy cập dữ liệu;Data Access Layer;interface;dễ kiểm thử'),
    (@prn, N'Async/await trong ASP.NET Core giúp ích gì cho hiệu năng của web server?',       N'không chặn thread;thread pool;I/O bound;Task'),
    (@prn, N'Model validation hoạt động ra sao trong Razor Pages? Cho ví dụ.',                N'Data Annotations;ModelState.IsValid;asp-validation-for'),
    (@enw, N'What are the main parts of a research paper and what does each one do?',         N'abstract;introduction;methodology;results;conclusion'),
    (@enw, N'How do you avoid plagiarism when you use other authors'' ideas?',                N'citation;paraphrase;quotation;reference list'),
    (@enw, N'Explain what a thesis statement is and why it matters.',                         N'main argument;clear;arguable;guides the paper'),
    (@enw, N'What is the difference between primary and secondary sources?',                  N'original data;analysis of others;examples');
END
GO

/* ---------- Bổ sung 40 câu hỏi: Siêu dễ và Dễ (tiếng Việt) ----------
   Câu hỏi kiến thức cơ bản dùng để thử luồng vấn đáp.
   Thêm cùng bộ 40 câu vào mỗi môn PRN222 và ENW492c đang tồn tại.
   Mức độ chia bằng chú thích; không thay đổi cấu trúc bảng.
   Chạy lại không thêm trùng theo SubjectId + Content.
   ---------- */
/* Siêu dễ: 20 câu cho mỗi môn */
INSERT INTO Questions (SubjectId, Content, ExpectedPoints)
SELECT s.Id, q.Content, q.ExpectedPoints
FROM Subjects AS s
CROSS JOIN (VALUES
    (N'1 + 1 bằng mấy?', N'2'),
    (N'2 + 1 bằng mấy?', N'3'),
    (N'2 - 1 bằng mấy?', N'1'),
    (N'3 - 1 bằng mấy?', N'2'),
    (N'0 + 1 bằng mấy?', N'1'),
    (N'1 + 0 bằng mấy?', N'1'),
    (N'2 + 2 bằng mấy?', N'4'),
    (N'3 + 1 bằng mấy?', N'4'),
    (N'4 - 1 bằng mấy?', N'3'),
    (N'5 - 1 bằng mấy?', N'4'),
    (N'Số nào đứng ngay sau số 1?', N'2'),
    (N'Số nào đứng ngay trước số 3?', N'2'),
    (N'Giữa số 1 và số 3 là số nào?', N'2'),
    (N'Số 2 và số 5, số nào lớn hơn?', N'5'),
    (N'Số 1 và số 4, số nào nhỏ hơn?', N'1'),
    (N'Một bàn tay thường có mấy ngón tay?', N'5 ngón tay'),
    (N'Một tuần có mấy ngày?', N'7 ngày'),
    (N'Hình tam giác có mấy cạnh?', N'3 cạnh'),
    (N'Hình vuông có mấy cạnh?', N'4 cạnh'),
    (N'Em có 1 cái kẹo, được cho thêm 1 cái. Em có tất cả mấy cái kẹo?', N'2 cái kẹo')
) AS q(Content, ExpectedPoints)
WHERE s.Code IN ('PRN222', 'ENW492c')
  AND NOT EXISTS (
      SELECT 1 FROM Questions AS existing
      WHERE existing.SubjectId = s.Id AND existing.Content = q.Content
  );
GO

/* Dễ: 20 câu cho mỗi môn */
INSERT INTO Questions (SubjectId, Content, ExpectedPoints)
SELECT s.Id, q.Content, q.ExpectedPoints
FROM Subjects AS s
CROSS JOIN (VALUES
    (N'5 + 3 bằng mấy?', N'8'),
    (N'7 + 2 bằng mấy?', N'9'),
    (N'10 - 4 bằng mấy?', N'6'),
    (N'8 - 3 bằng mấy?', N'5'),
    (N'2 nhân 3 bằng mấy?', N'6'),
    (N'3 nhân 3 bằng mấy?', N'9'),
    (N'6 chia 2 bằng mấy?', N'3'),
    (N'8 chia 4 bằng mấy?', N'2'),
    (N'10 + 5 bằng mấy?', N'15'),
    (N'20 - 10 bằng mấy?', N'10'),
    (N'Số nào đứng ngay sau số 19?', N'20'),
    (N'Số nào đứng ngay trước số 10?', N'9'),
    (N'Số 4 là số chẵn hay số lẻ?', N'Số chẵn'),
    (N'Số 7 là số chẵn hay số lẻ?', N'Số lẻ'),
    (N'Một giờ có mấy phút?', N'60 phút'),
    (N'Một phút có mấy giây?', N'60 giây'),
    (N'Một năm có mấy tháng?', N'12 tháng'),
    (N'Em có 5 cái kẹo, cho bạn 2 cái. Em còn lại mấy cái kẹo?', N'3 cái kẹo'),
    (N'Có 3 hộp bút, mỗi hộp có 2 cây bút. Có tất cả mấy cây bút?', N'6 cây bút'),
    (N'Chia đều 10 cái bánh cho 2 bạn. Mỗi bạn được mấy cái bánh?', N'5 cái bánh')
) AS q(Content, ExpectedPoints)
WHERE s.Code IN ('PRN222', 'ENW492c')
  AND NOT EXISTS (
      SELECT 1 FROM Questions AS existing
      WHERE existing.SubjectId = s.Id AND existing.Content = q.Content
  );
GO

