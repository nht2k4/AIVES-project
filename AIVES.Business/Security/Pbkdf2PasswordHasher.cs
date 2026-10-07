using AIVES.Business.Interfaces;

namespace AIVES.Business.Security;

// TV3 - Ass2 - việc 3.1. Xem AIVES_PhanCong_Ass2_Ass3.md.
// Gợi ý: Rfc2898DeriveBytes.Pbkdf2 có sẵn trong .NET, không cần thư viện ngoài.
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => throw new NotImplementedException("TV3: băm mật khẩu bằng PBKDF2 + salt ngẫu nhiên");

    public bool Verify(string password, string passwordHash) => throw new NotImplementedException("TV3: so sánh mật khẩu với chuỗi đã băm");
}
