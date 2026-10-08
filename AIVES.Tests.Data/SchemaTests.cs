using AIVES.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace AIVES.Tests.Data;

// ====== Tien: database do bạn viết phải KHỚP với các class Entity trong AIVES.DataAccess ======
// Test này không cần repository của TV2, chỉ cần database đã tạo bằng script của bạn và biến môi trường AIVES_TEST_CONNECTION.
// Mỗi lỗi in ra đủ để bạn biết sửa gì (thiếu bảng nào, cột nào, sai kiểu gì, cho phép NULL hay không).
public class SchemaTests
{
    private record Col(string Type, bool Nullable, int? MaxLength);

    private static async Task<Dictionary<string, Dictionary<string, Col>>> ReadColumnsAsync(AppDbContext ctx)
    {
        var result = new Dictionary<string, Dictionary<string, Col>>(StringComparer.OrdinalIgnoreCase);
        var conn = ctx.Database.GetDbConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var table = reader.GetString(0);
            if (!result.TryGetValue(table, out var columns)) result[table] = columns = new(StringComparer.OrdinalIgnoreCase);
            columns[reader.GetString(1)] = new Col(reader.GetString(2), reader.GetString(3) == "YES", reader.IsDBNull(4) ? null : reader.GetInt32(4));
        }
        return result;
    }

    private static string ExpectedSqlType(IProperty p)
    {
        var clr = Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType;
        if (clr == typeof(string)) return p.IsUnicode() == false ? "varchar" : "nvarchar";
        if (clr == typeof(int)) return "int";
        if (clr == typeof(bool)) return "bit";
        if (clr == typeof(DateTime)) return "datetime2";
        if (clr == typeof(byte[])) return "varbinary";
        return clr.Name;
    }

    [SqlFact]
    public async Task EveryEntity_HasAMatchingTable_WithMatchingColumns()
    {
        await using var ctx = Sql.NewContext();
        var db = await ReadColumnsAsync(ctx);
        var problems = new List<string>();

        foreach (var entity in ctx.Model.GetEntityTypes())
        {
            var table = entity.GetTableName()!;
            if (!db.TryGetValue(table, out var columns))
            {
                problems.Add($"THIẾU BẢNG {table} (entity {entity.ClrType.Name})");
                continue;
            }

            var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
            foreach (var p in entity.GetProperties())
            {
                var name = p.GetColumnName(store)!;
                if (!columns.TryGetValue(name, out var col))
                {
                    problems.Add($"{table}: THIẾU CỘT {name}");
                    continue;
                }

                var expectedType = ExpectedSqlType(p);
                if (!string.Equals(col.Type, expectedType, StringComparison.OrdinalIgnoreCase))
                    problems.Add($"{table}.{name}: kiểu là {col.Type}, entity cần {expectedType}");

                if (col.Nullable != p.IsNullable)
                    problems.Add($"{table}.{name}: database {(col.Nullable ? "cho phép NULL" : "NOT NULL")}, entity cần {(p.IsNullable ? "cho phép NULL" : "NOT NULL")}");

                var max = p.GetMaxLength();
                if (max is not null && col.Type is "varchar" or "nvarchar" && col.MaxLength != max)
                    problems.Add($"{table}.{name}: độ dài là {col.MaxLength}, entity cần {max}");
            }
        }

        Assert.True(problems.Count == 0, "Database chưa khớp với entity:\n - " + string.Join("\n - ", problems));
    }

    // Tên ràng buộc theo quy ước: PK_, UQ_, FK_, CK_, DF_ + Bảng + Cột. Tên rõ ràng giúp đọc lỗi database dễ hơn nhiều.
    public static IEnumerable<object[]> ExpectedConstraints() => new[]
    {
        "PK_Accounts", "UQ_Accounts_Email", "CK_Accounts_Role", "UQ_Accounts_StudentCode",
        "PK_Subjects", "UQ_Subjects_Code", "CK_Subjects_SttLanguage", "CK_Subjects_TtsLanguage",
        "CK_Subjects_AiTimeoutSeconds", "DF_Subjects_AiTimeoutSeconds", "DF_Subjects_UseExternalAi",
        "PK_LecturerSubjects", "FK_LecturerSubjects_Accounts", "FK_LecturerSubjects_Subjects",
        "PK_Questions", "FK_Questions_Subjects",
        "PK_ExamSessions", "CK_ExamSessions_Status", "FK_ExamSessions_Subjects", "FK_ExamSessions_Accounts",
        "PK_ExamParticipants", "UQ_ExamParticipants_Student", "CK_ExamParticipants_Status", "FK_ExamParticipants_Sessions",
        "PK_ParticipantQuestions", "FK_ParticipantQuestions_Participants", "FK_ParticipantQuestions_Questions",
        "PK_InterviewTurns", "CK_InterviewTurns_Kind", "FK_InterviewTurns_Participants", "FK_InterviewTurns_Questions",
        "PK_Voices", "UQ_Voices_Name", "FK_Voices_Accounts"
    }.Select(n => new object[] { n });

    [SqlTheory]
    [MemberData(nameof(ExpectedConstraints))]
    public async Task Database_HasTheNamedConstraint(string name)
    {
        await using var ctx = Sql.NewContext();
        var conn = ctx.Database.GetDbConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT (SELECT COUNT(*) FROM sys.objects WHERE name = @n) + (SELECT COUNT(*) FROM sys.indexes WHERE name = @n)";
        var parameter = cmd.CreateParameter();
        parameter.ParameterName = "@n";
        parameter.Value = name;
        cmd.Parameters.Add(parameter);

        Assert.True((int)(await cmd.ExecuteScalarAsync())! > 0, $"Database chưa có ràng buộc/chỉ mục tên {name}");
    }
}
