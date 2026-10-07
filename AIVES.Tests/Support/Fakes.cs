using AIVES.Business.Interfaces;

namespace AIVES.Tests.Support;

// Băm "giả" nhưng đủ để test: cùng mật khẩu thì cùng chuỗi, khác mật khẩu thì khác chuỗi, và chuỗi băm khác mật khẩu gốc.
public class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "fake:" + new string(password.Reverse().ToArray());

    public bool Verify(string password, string passwordHash) => Hash(password) == passwordHash;
}
