using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Models;
using AIVES.Business.Security;
using AIVES.Business.Services;
using AIVES.Tests.Support;
using Xunit;

namespace AIVES.Tests.Business;

// ====== mã hóa mật khẩu, đăng nhập, quản lý tài khoản ======

public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_IsNotThePlainPassword()
    {
        Assert.DoesNotContain("Secret@1", _hasher.Hash("Secret@1"));
    }

    [Fact]
    public void Hash_SamePasswordTwice_GivesDifferentHashes_BecauseOfSalt()
    {
        Assert.NotEqual(_hasher.Hash("Secret@1"), _hasher.Hash("Secret@1"));
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Secret@1");
        Assert.True(_hasher.Verify("Secret@1", hash));
        Assert.False(_hasher.Verify("secret@1", hash));
        Assert.False(_hasher.Verify("", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("khong-phai-hash")]
    [InlineData("a.b")]
    public void Verify_MalformedHash_ReturnsFalse_InsteadOfThrowing(string badHash)
    {
        Assert.False(_hasher.Verify("Secret@1", badHash));
    }

    // Hợp đồng định dạng: "{số vòng lặp}.{salt base64}.{key base64}", PBKDF2-SHA256, 100.000 vòng, salt 16 byte, key 32 byte.
    // Các tài khoản mẫu trong script database (admin@fu.edu.vn...) được băm theo đúng định dạng này,
    // nên hasher của bạn phải đọc được chúng thì mới đăng nhập được.
    [Theory]
    [InlineData("Admin@123", "100000.uSRX8Si9+0CW3kmVg4f2+A==.MZn9CqsOUDr/aDtD0lfZynz0ovwimrQOiyYlxU3CFOE=")]
    [InlineData("Lecturer@123", "100000.aVHHCR5cg5+0AU+lX4mHHA==.+hubtA5WNK07tC/mMFfOkFw9UC644hUi1xpeZR9+nMU=")]
    [InlineData("Student@123", "100000.bhe8jteDTPLA1QMnp8+N/A==.vrsScOF3bxc5hi9vlXdNRj3fBVRaFv7+oI0GLYMN73U=")]
    public void Verify_ReadsTheSeedHashesUsedInTheDatabaseScripts(string password, string seedHash)
    {
        Assert.True(_hasher.Verify(password, seedHash));
        Assert.False(_hasher.Verify(password + "x", seedHash));
    }

    [Fact]
    public void Hash_UsesTheAgreedFormat_IterationsDotSaltDotKey()
    {
        var parts = _hasher.Hash("Secret@1").Split('.');

        Assert.Equal(3, parts.Length);
        Assert.Equal("100000", parts[0]);
        Assert.Equal(16, Convert.FromBase64String(parts[1]).Length);
        Assert.Equal(32, Convert.FromBase64String(parts[2]).Length);
    }
}

public class AuthServiceTests
{
    private readonly TestDb _db = new();
    private AuthService Service() => new(_db.AccountRepo, _db.Hasher);

    [Fact]
    public async Task Login_Correct_ReturnsUserWithRole()
    {
        var lecturer = _db.AddAccount(Roles.Lecturer, "an@fu.edu.vn", password: "Lecturer@1", name: "Nguyễn An");

        var result = await Service().LoginAsync("an@fu.edu.vn", "Lecturer@1");

        Assert.True(result.Succeeded);
        Assert.Equal(lecturer.Id, result.Data!.Id);
        Assert.Equal(AppRole.Lecturer, result.Data.Role);
        Assert.Equal("Nguyễn An", result.Data.FullName);
    }

    [Fact]
    public async Task Login_EmailIsCaseInsensitiveAndTrimmed()
    {
        _db.AddAccount(Roles.Admin, "admin@fu.edu.vn", password: "Admin@123");

        var result = await Service().LoginAsync("  ADMIN@FU.EDU.VN ", "Admin@123");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Login_WrongPassword_And_UnknownEmail_GiveTheSameMessage()
    {
        // Không để lộ email nào tồn tại trong hệ thống
        _db.AddAccount(Roles.Admin, "admin@fu.edu.vn", password: "Admin@123");

        var wrongPassword = await Service().LoginAsync("admin@fu.edu.vn", "sai-mat-khau");
        var unknownEmail = await Service().LoginAsync("khong-co@fu.edu.vn", "Admin@123");

        Assert.False(wrongPassword.Succeeded);
        Assert.False(unknownEmail.Succeeded);
        Assert.Equal(wrongPassword.Error, unknownEmail.Error);
    }

    [Fact]
    public async Task Login_LockedAccount_IsRejectedEvenWithCorrectPassword()
    {
        _db.AddAccount(Roles.Lecturer, "an@fu.edu.vn", active: false, password: "Lecturer@1");

        var result = await Service().LoginAsync("an@fu.edu.vn", "Lecturer@1");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Login_EmptyPassword_IsRejected()
    {
        _db.AddAccount(Roles.Admin, "admin@fu.edu.vn", password: "Admin@123");
        Assert.False((await Service().LoginAsync("admin@fu.edu.vn", "")).Succeeded);
    }

    [Fact]
    public async Task GetActiveRole_ActiveAccount_ReturnsRoleName()
    {
        var admin = _db.AddAccount(Roles.Admin);
        Assert.Equal("Admin", await Service().GetActiveRoleAsync(admin.Id));
    }

    [Fact]
    public async Task GetActiveRole_LockedOrMissingAccount_ReturnsNull()
    {
        var locked = _db.AddAccount(Roles.Lecturer, active: false);
        Assert.Null(await Service().GetActiveRoleAsync(locked.Id));
        Assert.Null(await Service().GetActiveRoleAsync(9999));
    }

    // ---- Tự đăng ký: tài khoản Pending chưa đăng nhập được cho tới khi quản trị viên cấp vai trò
    [Fact]
    public async Task Login_PendingAccount_IsRejected_WithAClearMessage_AfterPasswordCheck()
    {
        _db.AddAccount(Roles.Pending, "moi@fu.edu.vn", password: "Secret@1");

        var right = await Service().LoginAsync("moi@fu.edu.vn", "Secret@1");
        var wrong = await Service().LoginAsync("moi@fu.edu.vn", "sai-mat-khau");

        Assert.False(right.Succeeded);
        Assert.Contains("cấp vai trò", right.Error);
        // sai mật khẩu vẫn nhận thông báo chung, không lộ là tài khoản đang chờ
        Assert.DoesNotContain("cấp vai trò", wrong.Error);
    }

    [Fact]
    public async Task GetActiveRole_PendingAccount_ReturnsNull()
    {
        var pending = _db.AddAccount(Roles.Pending);
        Assert.Null(await Service().GetActiveRoleAsync(pending.Id));
    }
}

public class AccountServiceTests
{
    private readonly TestDb _db = new();
    private AccountService Service() => new(_db.AccountRepo, _db.LecturerSubjectRepo, _db.Hasher);

    // ---- Tạo tài khoản
    [Fact]
    public async Task Create_Valid_StoresNormalizedAccountWithHashedPassword()
    {
        var result = await Service().CreateAsync(new CreateAccountRequest("  Nguyễn An  ", "  An@FU.edu.vn ", "secret1", AppRole.Lecturer));

        Assert.True(result.Succeeded);
        var saved = _db.Accounts.Single();
        Assert.Equal(result.Data, saved.Id);
        Assert.Equal("Nguyễn An", saved.FullName);
        Assert.Equal("an@fu.edu.vn", saved.Email);
        Assert.Equal("Lecturer", saved.Role);
        Assert.True(saved.IsActive);
        Assert.NotEqual("secret1", saved.PasswordHash);
        Assert.True(_db.Hasher.Verify("secret1", saved.PasswordHash));
    }

    [Theory]
    [InlineData("", "an@fu.edu.vn", "secret1")]      // họ tên trống
    [InlineData("An", "khong-phai-email", "secret1")] // email sai
    [InlineData("An", "an@fu.edu.vn", "12345")]       // mật khẩu < 6 ký tự
    [InlineData("An", "an@fu.edu.vn", "")]
    public async Task Create_InvalidInput_IsRejected_AndNothingIsSaved(string name, string email, string password)
    {
        var result = await Service().CreateAsync(new CreateAccountRequest(name, email, password, AppRole.Lecturer));

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Empty(_db.Accounts);
    }

    [Fact]
    public async Task Create_UndefinedRole_IsRejected()
    {
        var result = await Service().CreateAsync(new CreateAccountRequest("An", "an@fu.edu.vn", "secret1", (AppRole)99));
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Create_DuplicateEmail_IsRejected_EvenWithDifferentCase()
    {
        _db.AddAccount(Roles.Lecturer, "an@fu.edu.vn");

        var result = await Service().CreateAsync(new CreateAccountRequest("Khác", "AN@fu.edu.vn", "secret1", AppRole.Lecturer));

        Assert.False(result.Succeeded);
        Assert.Single(_db.Accounts);
    }

    [Fact]
    public async Task Create_Student_RequiresStudentCode()
    {
        var result = await Service().CreateAsync(new CreateAccountRequest("SV", "sv@fu.edu.vn", "secret1", AppRole.Student, null));
        Assert.False(result.Succeeded);

        var blank = await Service().CreateAsync(new CreateAccountRequest("SV", "sv@fu.edu.vn", "secret1", AppRole.Student, "   "));
        Assert.False(blank.Succeeded);
    }

    [Fact]
    public async Task Create_Student_StoresTrimmedCode_AndRejectsDuplicateCode()
    {
        var first = await Service().CreateAsync(new CreateAccountRequest("SV1", "sv1@fu.edu.vn", "secret1", AppRole.Student, " SE180001 "));
        Assert.True(first.Succeeded);
        Assert.Equal("SE180001", _db.Accounts.Single().StudentCode);

        var duplicate = await Service().CreateAsync(new CreateAccountRequest("SV2", "sv2@fu.edu.vn", "secret1", AppRole.Student, "se180001"));
        Assert.False(duplicate.Succeeded);
        Assert.Single(_db.Accounts);
    }

    [Fact]
    public async Task Create_NonStudent_IgnoresStudentCode()
    {
        await Service().CreateAsync(new CreateAccountRequest("GV", "gv@fu.edu.vn", "secret1", AppRole.Lecturer, "SE1"));
        Assert.Null(_db.Accounts.Single().StudentCode);
    }

    // ---- Sửa tài khoản
    // ---- Tự đăng ký
    [Fact]
    public async Task Register_CreatesActivePendingAccount_WithHashedPassword()
    {
        var result = await Service().RegisterAsync(new RegisterRequest("  Lê Mới  ", " Moi@FU.edu.vn ", "secret1"));

        Assert.True(result.Succeeded);
        var saved = _db.Accounts.Single();
        Assert.Equal(("Lê Mới", "moi@fu.edu.vn", "Pending"), (saved.FullName, saved.Email, saved.Role));
        Assert.True(saved.IsActive);
        Assert.True(_db.Hasher.Verify("secret1", saved.PasswordHash));
    }

    [Theory]
    [InlineData("", "moi@fu.edu.vn", "secret1")]
    [InlineData("An", "khong-phai-email", "secret1")]
    [InlineData("An", "moi@fu.edu.vn", "123")]
    public async Task Register_InvalidInput_IsRejected(string name, string email, string password)
    {
        Assert.False((await Service().RegisterAsync(new RegisterRequest(name, email, password))).Succeeded);
        Assert.Empty(_db.Accounts);
    }

    [Fact]
    public async Task Register_DuplicateEmail_IsRejected()
    {
        _db.AddAccount(Roles.Lecturer, "an@fu.edu.vn");
        var result = await Service().RegisterAsync(new RegisterRequest("An", "AN@fu.edu.vn", "secret1"));
        Assert.False(result.Succeeded);
        Assert.Single(_db.Accounts);
    }

    [Fact]
    public async Task Register_ThenAdminGrantsRole_AccountCanLogIn()
    {
        var admin = _db.AddAccount(Roles.Admin);
        var id = (await Service().RegisterAsync(new RegisterRequest("Lê Mới", "moi@fu.edu.vn", "secret1"))).Data;
        var auth = new AuthService(_db.AccountRepo, _db.Hasher);
        Assert.False((await auth.LoginAsync("moi@fu.edu.vn", "secret1")).Succeeded);

        var grant = await Service().UpdateAsync(admin.Id, new UpdateAccountRequest(id, "Lê Mới", "moi@fu.edu.vn", AppRole.Lecturer));

        Assert.True(grant.Succeeded);
        var login = await auth.LoginAsync("moi@fu.edu.vn", "secret1");
        Assert.True(login.Succeeded);
        Assert.Equal(AppRole.Lecturer, login.Data!.Role);
    }

    [Fact]
    public async Task Update_ChangesNameAndEmail()
    {
        var admin = _db.AddAccount(Roles.Admin);
        var lecturer = _db.AddAccount(Roles.Lecturer);

        var result = await Service().UpdateAsync(admin.Id, new UpdateAccountRequest(lecturer.Id, "Tên mới", "MOI@fu.edu.vn", AppRole.Lecturer));

        Assert.True(result.Succeeded);
        Assert.Equal("Tên mới", lecturer.FullName);
        Assert.Equal("moi@fu.edu.vn", lecturer.Email);
    }

    [Fact]
    public async Task Update_UnknownAccount_IsNotFound()
    {
        var result = await Service().UpdateAsync(1, new UpdateAccountRequest(999, "A", "a@fu.edu.vn", AppRole.Lecturer));
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Update_EmailUsedByAnotherAccount_IsRejected_ButKeepingOwnEmailIsFine()
    {
        var admin = _db.AddAccount(Roles.Admin);
        var a = _db.AddAccount(Roles.Lecturer, "a@fu.edu.vn");
        _db.AddAccount(Roles.Lecturer, "b@fu.edu.vn");

        var clash = await Service().UpdateAsync(admin.Id, new UpdateAccountRequest(a.Id, "A", "b@fu.edu.vn", AppRole.Lecturer));
        var same = await Service().UpdateAsync(admin.Id, new UpdateAccountRequest(a.Id, "A mới", "a@fu.edu.vn", AppRole.Lecturer));

        Assert.False(clash.Succeeded);
        Assert.True(same.Succeeded);
    }

    [Fact]
    public async Task Update_CannotChangeYourOwnRole()
    {
        var admin = _db.AddAccount(Roles.Admin);
        _db.AddAccount(Roles.Admin); // có admin khác nên không dính luật "admin cuối cùng"

        var result = await Service().UpdateAsync(admin.Id, new UpdateAccountRequest(admin.Id, "A", admin.Email, AppRole.Lecturer));

        Assert.False(result.Succeeded);
        Assert.Equal("Admin", admin.Role);
    }

    [Fact]
    public async Task Update_CannotDemoteTheLastActiveAdmin()
    {
        var onlyAdmin = _db.AddAccount(Roles.Admin);
        var other = _db.AddAccount(Roles.Lecturer);

        var result = await Service().UpdateAsync(other.Id, new UpdateAccountRequest(onlyAdmin.Id, "A", onlyAdmin.Email, AppRole.Lecturer));

        Assert.False(result.Succeeded);
        Assert.Equal("Admin", onlyAdmin.Role);
    }

    [Fact]
    public async Task Update_CanDemoteAnAdmin_WhenAnotherActiveAdminExists()
    {
        var a = _db.AddAccount(Roles.Admin);
        var b = _db.AddAccount(Roles.Admin);

        var result = await Service().UpdateAsync(a.Id, new UpdateAccountRequest(b.Id, "B", b.Email, AppRole.Lecturer));

        Assert.True(result.Succeeded);
        Assert.Equal("Lecturer", b.Role);
    }

    [Fact]
    public async Task Update_LecturerWithAssignments_CannotChangeRole()
    {
        var admin = _db.AddAccount(Roles.Admin);
        var lecturer = _db.AddAccount(Roles.Lecturer);
        _db.Assign(lecturer, _db.AddSubject("PRN222"));

        var result = await Service().UpdateAsync(admin.Id, new UpdateAccountRequest(lecturer.Id, "GV", lecturer.Email, AppRole.Admin));

        Assert.False(result.Succeeded);
        Assert.Equal("Lecturer", lecturer.Role);
    }

    [Fact]
    public async Task Update_ToStudent_NeedsCode_AndLeavingStudentClearsIt()
    {
        var admin = _db.AddAccount(Roles.Admin);
        var lecturer = _db.AddAccount(Roles.Lecturer);

        var noCode = await Service().UpdateAsync(admin.Id, new UpdateAccountRequest(lecturer.Id, "X", lecturer.Email, AppRole.Student));
        Assert.False(noCode.Succeeded);

        var withCode = await Service().UpdateAsync(admin.Id, new UpdateAccountRequest(lecturer.Id, "X", lecturer.Email, AppRole.Student, "SE9"));
        Assert.True(withCode.Succeeded);
        Assert.Equal("SE9", lecturer.StudentCode);

        var back = await Service().UpdateAsync(admin.Id, new UpdateAccountRequest(lecturer.Id, "X", lecturer.Email, AppRole.Lecturer, "SE9"));
        Assert.True(back.Succeeded);
        Assert.Null(lecturer.StudentCode);
    }

    // ---- Khóa / mở khóa
    [Fact]
    public async Task SetActive_CannotLockYourself()
    {
        var admin = _db.AddAccount(Roles.Admin);
        _db.AddAccount(Roles.Admin);

        var result = await Service().SetActiveAsync(admin.Id, admin.Id, false);

        Assert.False(result.Succeeded);
        Assert.True(admin.IsActive);
    }

    [Fact]
    public async Task SetActive_CannotLockTheLastActiveAdmin()
    {
        var onlyAdmin = _db.AddAccount(Roles.Admin);
        var lecturer = _db.AddAccount(Roles.Lecturer);

        var result = await Service().SetActiveAsync(lecturer.Id, onlyAdmin.Id, false);

        Assert.False(result.Succeeded);
        Assert.True(onlyAdmin.IsActive);
    }

    [Fact]
    public async Task SetActive_LockThenUnlock_AnotherAccount()
    {
        var admin = _db.AddAccount(Roles.Admin);
        var lecturer = _db.AddAccount(Roles.Lecturer);

        Assert.True((await Service().SetActiveAsync(admin.Id, lecturer.Id, false)).Succeeded);
        Assert.False(lecturer.IsActive);

        Assert.True((await Service().SetActiveAsync(admin.Id, lecturer.Id, true)).Succeeded);
        Assert.True(lecturer.IsActive);
    }

    [Fact]
    public async Task SetActive_SameState_IsHarmless()
    {
        var admin = _db.AddAccount(Roles.Admin);
        var lecturer = _db.AddAccount(Roles.Lecturer);
        Assert.True((await Service().SetActiveAsync(admin.Id, lecturer.Id, true)).Succeeded);
    }

    [Fact]
    public async Task SetActive_UnknownAccount_IsNotFound()
    {
        Assert.Equal(ServiceErrorType.NotFound, (await Service().SetActiveAsync(1, 999, false)).ErrorType);
    }

    // ---- Đặt lại mật khẩu
    [Fact]
    public async Task ResetPassword_Valid_ReplacesTheHash()
    {
        var lecturer = _db.AddAccount(Roles.Lecturer, password: "OldPass@1");

        var result = await Service().ResetPasswordAsync(lecturer.Id, "NewPass@1");

        Assert.True(result.Succeeded);
        Assert.True(_db.Hasher.Verify("NewPass@1", lecturer.PasswordHash));
        Assert.False(_db.Hasher.Verify("OldPass@1", lecturer.PasswordHash));
    }

    [Fact]
    public async Task ResetPassword_TooShort_IsRejected_AndOldPasswordStillWorks()
    {
        var lecturer = _db.AddAccount(Roles.Lecturer, password: "OldPass@1");

        var result = await Service().ResetPasswordAsync(lecturer.Id, "123");

        Assert.False(result.Succeeded);
        Assert.True(_db.Hasher.Verify("OldPass@1", lecturer.PasswordHash));
    }

    [Fact]
    public async Task ResetPassword_UnknownAccount_IsNotFound()
    {
        Assert.Equal(ServiceErrorType.NotFound, (await Service().ResetPasswordAsync(999, "NewPass@1")).ErrorType);
    }

    // ---- Tìm kiếm
    [Fact]
    public async Task Search_FiltersByKeywordAndRole()
    {
        _db.AddAccount(Roles.Admin, "admin@fu.edu.vn", name: "Quản trị");
        _db.AddAccount(Roles.Lecturer, "an@fu.edu.vn", name: "Nguyễn An");
        _db.AddAccount(Roles.Lecturer, "binh@fu.edu.vn", name: "Trần Bình");

        Assert.Equal(3, (await Service().SearchAsync(null, null)).Count);
        Assert.Equal(2, (await Service().SearchAsync(null, AppRole.Lecturer)).Count);
        Assert.Single(await Service().SearchAsync("binh", null));
        Assert.Single(await Service().SearchAsync("NGUYỄN", AppRole.Lecturer));
        Assert.Empty(await Service().SearchAsync("binh", AppRole.Admin));
    }

    [Fact]
    public async Task GetById_ReturnsDto_OrNotFound()
    {
        var student = _db.AddAccount(Roles.Student, studentCode: "SE1");

        var ok = await Service().GetByIdAsync(student.Id);
        Assert.True(ok.Succeeded);
        Assert.Equal(AppRole.Student, ok.Data!.Role);
        Assert.Equal("SE1", ok.Data.StudentCode);

        Assert.Equal(ServiceErrorType.NotFound, (await Service().GetByIdAsync(999)).ErrorType);
    }
}
