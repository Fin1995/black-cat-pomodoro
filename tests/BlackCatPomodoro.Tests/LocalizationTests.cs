using Xunit;

namespace BlackCatPomodoro.Tests
{
    public class LocalizationTests
    {
        [Fact]
        public void AllKeys_HaveEnglishAndChineseValues()
        {
            foreach (string key in Localization.AllKeys)
            {
                Assert.True(Localization.HasKey(key, AppLanguage.English), "Missing English key: " + key);
                Assert.True(Localization.HasKey(key, AppLanguage.Chinese), "Missing Chinese key: " + key);

                string english = Localization.T(key, AppLanguage.English);
                string chinese = Localization.T(key, AppLanguage.Chinese);

                Assert.False(string.IsNullOrWhiteSpace(english), "Empty English value: " + key);
                Assert.False(string.IsNullOrWhiteSpace(chinese), "Empty Chinese value: " + key);
            }
        }

        [Fact]
        public void ParseLanguage_DefaultsToEnglish()
        {
            Assert.Equal(AppLanguage.English, Localization.ParseLanguage(null));
            Assert.Equal(AppLanguage.English, Localization.ParseLanguage(string.Empty));
            Assert.Equal(AppLanguage.English, Localization.ParseLanguage("en"));
            Assert.Equal(AppLanguage.Chinese, Localization.ParseLanguage("zh"));
            Assert.Equal(AppLanguage.Chinese, Localization.ParseLanguage("zh-CN"));
        }

        [Fact]
        public void AppTitle_IsLocalized()
        {
            Assert.Equal("Black Cat Pomodoro", Localization.T("AppTitle", AppLanguage.English));
            Assert.Equal("黑猫番茄钟", Localization.T("AppTitle", AppLanguage.Chinese));
        }

        [Fact]
        public void IsDefaultSample_RecognizesLegacyAndCurrentSamples()
        {
            var legacy = new PomodoroTask
            {
                Name = "[示例] 学习编程",
                Notes = "这是一个示例待办，可以删除或修改"
            };

            var chinese = new PomodoroTask
            {
                Name = "示例：学习编程",
                Notes = "这是一个示例待办，可以删除或修改。"
            };

            var english = new PomodoroTask
            {
                Name = "Sample: Learn programming",
                Notes = "This is a sample task. You can edit or delete it."
            };

            var edited = new PomodoroTask
            {
                Name = "My own task",
                Notes = "This is a sample task. You can edit or delete it."
            };

            Assert.True(Localization.IsDefaultSample(legacy));
            Assert.True(Localization.IsDefaultSample(chinese));
            Assert.True(Localization.IsDefaultSample(english));
            Assert.False(Localization.IsDefaultSample(edited));
        }
    }
}
