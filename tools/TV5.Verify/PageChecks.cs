using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Xunit;

namespace TV5.Verify;

public class PageChecks
{
    [Fact]
    public async Task Pages_RenderAndPerformAuthorizedFlows_WithExplicitTestDependencies()
    {
        var data = new PreviewData();
        await using var app = PreviewHost.Build("http://127.0.0.1:0", data);
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri(address) };
        client.DefaultRequestHeaders.Add("X-TV5-Test-User", data.AdminId.ToString());
        var index = await Get(client, "/Subjects");
        Assert.Contains("PRN211", index);
        Assert.Contains("Đã ngừng", WebUtility.HtmlDecode(index));
        var details = await Get(client, $"/Subjects/Details/{data.PrnId}");
        Assert.Contains("an.nv@fu.edu.vn", details);
        var options = Regex.Match(details, "<select[\\s\\S]*?</select>").Value;
        Assert.Contains("binh.tt@fu.edu.vn", options);
        Assert.DoesNotContain("an.nv@fu.edu.vn", options);
        Assert.DoesNotContain("admin@fu.edu.vn", options);
        Assert.DoesNotContain("locked@fu.edu.vn", options);
        Assert.DoesNotContain("pending@fu.edu.vn", options);
        Assert.Contains("/Exams/Questions?subjectId=", details);

        var blank = await Post(client, $"/Subjects/Details/{data.PrnId}?handler=Assign", details, ("LecturerId", ""));
        Assert.Equal(HttpStatusCode.OK, blank.StatusCode);
        Assert.Contains("Chọn giảng viên cần phân công.", WebUtility.HtmlDecode(await blank.Content.ReadAsStringAsync()));
        var swp = await Get(client, $"/Subjects/Details/{data.SwpId}");
        var assigned = await Post(client, $"/Subjects/Details/{data.SwpId}?handler=Assign", swp, ("LecturerId", data.BinhId.ToString()));
        Assert.Equal(HttpStatusCode.Redirect, assigned.StatusCode);
        Assert.Contains(data.Db.LecturerSubjects, a => a.SubjectId == data.SwpId && a.LecturerId == data.BinhId);
        swp = await Get(client, $"/Subjects/Details/{data.SwpId}");
        var removed = await Post(client, $"/Subjects/Details/{data.SwpId}?handler=Unassign", swp, ("lecturerId", data.BinhId.ToString()));
        Assert.Equal(HttpStatusCode.Redirect, removed.StatusCode);
        Assert.DoesNotContain(data.Db.LecturerSubjects, a => a.SubjectId == data.SwpId && a.LecturerId == data.BinhId);
        Assert.Contains("Đã gỡ phân công.", WebUtility.HtmlDecode(await Get(client, $"/Subjects/Details/{data.SwpId}")));
        var stopped = await Get(client, $"/Subjects/Details/{data.StoppedId}");
        var stoppedPost = await Post(client, $"/Subjects/Details/{data.StoppedId}?handler=Assign", stopped, ("LecturerId", data.BinhId.ToString()));
        Assert.Equal(HttpStatusCode.OK, stoppedPost.StatusCode);
        Assert.Contains("ngừng", WebUtility.HtmlDecode(await stoppedPost.Content.ReadAsStringAsync()));

        var language = await Get(client, $"/Language/Edit?subjectId={data.PrnId}");
        var saved = await Post(client, $"/Language/Edit?subjectId={data.PrnId}", language, ("SttLanguage", "2"), ("TtsLanguage", "1"));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.Equal($"/Subjects/Details/{data.PrnId}", saved.Headers.Location!.OriginalString);
        Assert.Equal("en-US", data.Db.Subjects.Single(s => s.Id == data.PrnId).SttLanguage);
        Assert.Contains("Lần sửa gần nhất", WebUtility.HtmlDecode(await Get(client, $"/Language/Edit?subjectId={data.PrnId}")));
        var badLanguage = await Post(client, $"/Language/Edit?subjectId={data.PrnId}", language, ("SttLanguage", "99"), ("TtsLanguage", "2"));
        Assert.Equal(HttpStatusCode.OK, badLanguage.StatusCode);
        Assert.Equal("en-US", data.Db.Subjects.Single(s => s.Id == data.PrnId).SttLanguage);
        var ttsSelect = Regex.Match(await badLanguage.Content.ReadAsStringAsync(), "<select[^>]*id=\"TtsLanguage\"[\\s\\S]*?</select>").Value;
        Assert.Matches("<option(?=[^>]*value=\"2\")(?=[^>]*selected)[^>]*>", ttsSelect);

        var bankUrl = $"/Exams/Questions?subjectId={data.PrnId}";
        var bank = await Get(client, bankUrl);
        Assert.Contains("Đã tắt", WebUtility.HtmlDecode(bank));
        var empty = await Post(client, bankUrl + "&handler=Add", bank, ("Content", ""), ("ExpectedPoints", "a;b"));
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        var emptyHtml = await empty.Content.ReadAsStringAsync();
        Assert.Contains("Nhập nội dung câu hỏi.", WebUtility.HtmlDecode(emptyHtml));
        Assert.Contains("a;b", emptyHtml);
        var added = await Post(client, bankUrl + "&handler=Add", bank, ("Content", "<script>alert(1)</script>"), ("ExpectedPoints", "a;b"));
        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);
        var q = data.Db.Questions.Last();
        Assert.Equal("a;b", q.ExpectedPoints);
        var freshBank = await Get(client, bankUrl);
        Assert.DoesNotContain("<script>alert(1)</script>", freshBank);
        Assert.Contains("&lt;script&gt;", freshBank);
        var off = await Post(client, bankUrl + "&handler=SetActive", freshBank, ("questionId", q.Id.ToString()), ("isActive", "false"));
        Assert.Equal(HttpStatusCode.Redirect, off.StatusCode);
        Assert.False(q.IsActive);
        var on = await Post(client, bankUrl + "&handler=SetActive", freshBank, ("questionId", q.Id.ToString()), ("isActive", "true"));
        Assert.Equal(HttpStatusCode.Redirect, on.StatusCode);
        Assert.True(q.IsActive);
        var otherQuestion = data.Db.Questions.Single(x => x.SubjectId == data.SwpId);
        var tampered = await Post(client, bankUrl + "&handler=SetActive", freshBank, ("questionId", otherQuestion.Id.ToString()), ("isActive", "false"));
        Assert.Equal(HttpStatusCode.NotFound, tampered.StatusCode);
        Assert.True(otherQuestion.IsActive);
        var missingQuestion = await Post(client, bankUrl + "&handler=SetActive", freshBank, ("questionId", "999"), ("isActive", "false"));
        Assert.Equal(HttpStatusCode.NotFound, missingQuestion.StatusCode);
        var malformedToggle = await Post(client, bankUrl + "&handler=SetActive", freshBank, ("questionId", q.Id.ToString()), ("isActive", "invalid"));
        Assert.Equal(HttpStatusCode.OK, malformedToggle.StatusCode);
        Assert.True(q.IsActive);
        var incompleteToggle = await Post(client, bankUrl + "&handler=SetActive", freshBank, ("questionId", q.Id.ToString()));
        Assert.Equal(HttpStatusCode.OK, incompleteToggle.StatusCode);
        Assert.True(q.IsActive);
        var noToken = await client.PostAsync(bankUrl + "&handler=Add", new FormUrlEncodedContent(new[] { new KeyValuePair<string,string>("Content", "Bad") }));
        Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);

        client.DefaultRequestHeaders.Remove("X-TV5-Test-User");
        client.DefaultRequestHeaders.Add("X-TV5-Test-User", data.AnId.ToString());
        var mine = await Get(client, "/MySubjects");
        Assert.Contains("PRN222", mine);
        Assert.DoesNotContain("SWP391", mine);
        var blockedSubjects = await client.GetAsync("/Subjects");
        Assert.Equal("/Auth/AccessDenied", blockedSubjects.Headers.Location!.OriginalString);
        var deniedLanguage = await client.GetAsync($"/Language/Edit?subjectId={data.SwpId}");
        Assert.Equal("/Auth/AccessDenied", deniedLanguage.Headers.Location!.OriginalString);
        var allowedLanguage = await Get(client, $"/Language/Edit?subjectId={data.PrnId}");
        var deniedPost = await Post(client, $"/Language/Edit?subjectId={data.SwpId}", allowedLanguage, ("SttLanguage", "2"), ("TtsLanguage", "2"));
        Assert.Equal("/Auth/AccessDenied", deniedPost.Headers.Location!.OriginalString);
        var deniedBank = await client.GetAsync($"/Exams/Questions?subjectId={data.SwpId}");
        Assert.Equal("/Auth/AccessDenied", deniedBank.Headers.Location!.OriginalString);
        var anBank = await Get(client, bankUrl);
        var deniedBankPost = await Post(client, $"/Exams/Questions?subjectId={data.SwpId}&handler=Add", anBank, ("Content", "Denied"));
        Assert.Equal("/Auth/AccessDenied", deniedBankPost.Headers.Location!.OriginalString);
        Assert.DoesNotContain(data.Db.Questions, x => x.Content == "Denied");
        var allowedSave = await Post(client, $"/Language/Edit?subjectId={data.PrnId}", allowedLanguage, ("SttLanguage", "1"), ("TtsLanguage", "2"));
        Assert.Equal("/MySubjects", allowedSave.Headers.Location!.OriginalString);

        client.DefaultRequestHeaders.Remove("X-TV5-Test-User");
        client.DefaultRequestHeaders.Add("X-TV5-Test-User", data.AdminId.ToString());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Subjects/Details/999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Language/Edit?subjectId=999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Exams/Questions?subjectId=999")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-TV5-Test-User");
        Assert.Equal("/Auth/Login", (await client.GetAsync("/Subjects")).Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("/Subjects")]
    [InlineData("/Subjects/Details/6")]
    [InlineData("/MySubjects")]
    [InlineData("/Language/Edit?subjectId=6")]
    [InlineData("/Exams/Questions?subjectId=6")]
    public async Task AnonymousLockedAndPendingUsers_CannotOpenTV5Pages(string path)
    {
        var data = new PreviewData();
        await using var app = PreviewHost.Build("http://127.0.0.1:0", data);
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri(address) };
        Assert.Equal("/Auth/Login", (await client.GetAsync(path)).Headers.Location!.OriginalString);
        client.DefaultRequestHeaders.Add("X-TV5-Test-User", data.LockedId.ToString());
        Assert.Equal("/Auth/Login", (await client.GetAsync(path)).Headers.Location!.OriginalString);
        client.DefaultRequestHeaders.Remove("X-TV5-Test-User");
        client.DefaultRequestHeaders.Add("X-TV5-Test-User", data.PendingId.ToString());
        Assert.Equal("/Auth/AccessDenied", (await client.GetAsync(path)).Headers.Location!.OriginalString);
    }

    private static async Task<string> Get(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"GET {path}: {response.StatusCode}\n{html}");
        return html;
    }
    private static Task<HttpResponseMessage> Post(HttpClient client, string path, string html, params (string key, string value)[] fields)
    {
        var token = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        Assert.NotEmpty(token);
        return client.PostAsync(path, new FormUrlEncodedContent(fields.Select(x => new KeyValuePair<string,string>(x.key, x.value))
            .Append(new("__RequestVerificationToken", token))));
    }
}
