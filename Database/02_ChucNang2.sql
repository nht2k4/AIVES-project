/* =========================================================
   AIVES - 02: Chức năng 2 (Quản lý kỳ thi & lịch thi). Dùng cho Ass2, Ass3.
   Chạy SAU 01_ChucNang7.sql. Idempotent: chạy lại nhiều lần không mất dữ liệu.
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
