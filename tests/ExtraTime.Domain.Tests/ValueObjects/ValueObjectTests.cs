using ExtraTime.Domain.Enums;
using ExtraTime.Domain.ValueObjects;

namespace ExtraTime.Domain.Tests.ValueObjects;

public sealed class ValueObjectTests
{
    #region Email Tests

    [Test]
    public async Task Email_ValidEmail_CreatesEmail()
    {
        // Arrange
        var validEmail = "test@example.com";

        // Act
        var email = new Email(validEmail);

        // Assert
        await Assert.That(email.Value).IsEqualTo(validEmail.ToLowerInvariant());
    }

    [Test]
    public async Task Email_InvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        var invalidEmail = "invalid-email";

        // Act & Assert
        await Assert.That(() => new Email(invalidEmail))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Email_EmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.That(() => new Email(""))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Email_Null_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.That(() => new Email(null!))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Email_Whitespace_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.That(() => new Email("   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Email_ImplicitConversion_ReturnsString()
    {
        // Arrange
        var email = new Email("test@example.com");

        // Act
        string emailString = email;

        // Assert
        await Assert.That(emailString).IsEqualTo("test@example.com");
    }

    [Test]
    public async Task Email_ExplicitConversion_CreatesEmail()
    {
        // Act
        var email = (Email)"test@example.com";

        // Assert
        await Assert.That(email.Value).IsEqualTo("test@example.com");
    }

    [Test]
    public async Task Email_Equality_SameValue_ReturnsTrue()
    {
        // Arrange
        var email1 = new Email("test@example.com");
        var email2 = new Email("test@example.com");

        // Assert
        await Assert.That(email1).IsEqualTo(email2);
    }

    [Test]
    public async Task Email_Equality_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var email1 = new Email("test1@example.com");
        var email2 = new Email("test2@example.com");

        // Assert
        await Assert.That(email1).IsNotEqualTo(email2);
    }

    [Test]
    public async Task Email_CaseNormalization_ConvertsToLowercase()
    {
        // Arrange
        var mixedCaseEmail = "Test@Example.COM";

        // Act
        var email = new Email(mixedCaseEmail);

        // Assert
        await Assert.That(email.Value).IsEqualTo("test@example.com");
    }

    #endregion

    #region Username Tests

    [Test]
    public async Task Username_ValidUsername_CreatesUsername()
    {
        // Arrange
        var validUsername = "testuser";

        // Act
        var username = new Username(validUsername);

        // Assert
        await Assert.That(username.Value).IsEqualTo(validUsername);
    }

    [Test]
    public async Task Username_TooShort_ThrowsArgumentException()
    {
        // Arrange
        var shortUsername = "ab"; // Less than 3 characters

        // Act & Assert
        await Assert.That(() => new Username(shortUsername))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Username_TooLong_ThrowsArgumentException()
    {
        // Arrange
        var longUsername = new string('a', 51); // More than 50 characters

        // Act & Assert
        await Assert.That(() => new Username(longUsername))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Username_MinimumLength_CreatesUsername()
    {
        // Arrange
        var minUsername = "abc"; // Exactly 3 characters

        // Act
        var username = new Username(minUsername);

        // Assert
        await Assert.That(username.Value).IsEqualTo(minUsername);
    }

    [Test]
    public async Task Username_MaximumLength_CreatesUsername()
    {
        // Arrange
        var maxUsername = new string('a', 50); // Exactly 50 characters

        // Act
        var username = new Username(maxUsername);

        // Assert
        await Assert.That(username.Value).IsEqualTo(maxUsername);
    }

    [Test]
    public async Task Username_Empty_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.That(() => new Username(""))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Username_Whitespace_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.That(() => new Username("   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Username_ImplicitConversion_ReturnsString()
    {
        // Arrange
        var username = new Username("testuser");

        // Act
        string usernameString = username;

        // Assert
        await Assert.That(usernameString).IsEqualTo("testuser");
    }

    [Test]
    public async Task Username_Equality_SameValue_ReturnsTrue()
    {
        // Arrange
        var username1 = new Username("testuser");
        var username2 = new Username("testuser");

        // Assert
        await Assert.That(username1).IsEqualTo(username2);
    }

    #endregion

    #region InviteCode Tests

    [Test]
    public async Task InviteCode_ValidCode_CreatesInviteCode()
    {
        // Arrange
        var validCode = "ABC123";

        // Act
        var inviteCode = new InviteCode(validCode);

        // Assert
        await Assert.That(inviteCode.Value).IsEqualTo(validCode.ToUpperInvariant());
    }

    [Test]
    public async Task InviteCode_ConvertsToUppercase()
    {
        // Arrange
        var mixedCaseCode = "abc123";

        // Act
        var inviteCode = new InviteCode(mixedCaseCode);

        // Assert
        await Assert.That(inviteCode.Value).IsEqualTo("ABC123");
    }

    [Test]
    public async Task InviteCode_TooShort_ThrowsArgumentException()
    {
        // Arrange
        var shortCode = "ABC12"; // Less than 6 characters

        // Act & Assert
        await Assert.That(() => new InviteCode(shortCode))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task InviteCode_MinimumLength_CreatesCode()
    {
        // Arrange
        var minCode = "ABCDEF"; // Exactly 6 characters

        // Act
        var inviteCode = new InviteCode(minCode);

        // Assert
        await Assert.That(inviteCode.Value).IsEqualTo(minCode);
    }

    [Test]
    public async Task InviteCode_Empty_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.That(() => new InviteCode(""))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task InviteCode_Whitespace_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.That(() => new InviteCode("   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task InviteCode_ImplicitConversion_ReturnsString()
    {
        // Arrange
        var inviteCode = new InviteCode("ABC123");

        // Act
        string codeString = inviteCode;

        // Assert
        await Assert.That(codeString).IsEqualTo("ABC123");
    }

    [Test]
    public async Task InviteCode_Equality_SameValue_ReturnsTrue()
    {
        // Arrange
        var code1 = new InviteCode("ABC123");
        var code2 = new InviteCode("abc123"); // Case insensitive

        // Assert
        await Assert.That(code1).IsEqualTo(code2);
    }

    #endregion

    #region MatchScore Tests

    [Test]
    public async Task MatchScore_ValidScores_CreatesMatchScore()
    {
        // Arrange
        var homeScore = new Score(2);
        var awayScore = new Score(1);

        // Act
        var matchScore = new MatchScore(homeScore, awayScore);

        // Assert
        await Assert.That(matchScore.Home.Value).IsEqualTo(2);
        await Assert.That(matchScore.Away.Value).IsEqualTo(1);
    }

    [Test]
    public async Task MatchScore_ToString_ReturnsFormattedString()
    {
        // Arrange
        var matchScore = new MatchScore(new Score(2), new Score(1));

        // Act
        var result = matchScore.ToString();

        // Assert
        await Assert.That(result).IsEqualTo("2-1");
    }

    [Test]
    public async Task MatchScore_ZeroZero_ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var matchScore = new MatchScore(new Score(0), new Score(0));

        // Act
        var result = matchScore.ToString();

        // Assert
        await Assert.That(result).IsEqualTo("0-0");
    }

    #endregion

    #region Score Tests

    [Test]
    public async Task Score_ValidScore_CreatesScore()
    {
        // Arrange
        var scoreValue = 3;

        // Act
        var score = new Score(scoreValue);

        // Assert
        await Assert.That(score.Value).IsEqualTo(scoreValue);
    }

    [Test]
    public async Task Score_Zero_CreatesScore()
    {
        // Act
        var score = new Score(0);

        // Assert
        await Assert.That(score.Value).IsEqualTo(0);
    }

    [Test]
    public async Task Score_Negative_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.That(() => new Score(-1))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Score_ImplicitConversion_ReturnsInt()
    {
        // Arrange
        var score = new Score(5);

        // Act
        int intValue = score;

        // Assert
        await Assert.That(intValue).IsEqualTo(5);
    }

    [Test]
    public async Task Score_ExplicitConversion_CreatesScore()
    {
        // Act
        var score = (Score)5;

        // Assert
        await Assert.That(score.Value).IsEqualTo(5);
    }

    [Test]
    public async Task Score_Equality_SameValue_ReturnsTrue()
    {
        // Arrange
        var score1 = new Score(3);
        var score2 = new Score(3);

        // Assert
        await Assert.That(score1).IsEqualTo(score2);
    }

    #endregion

    #region BettingDeadline Tests

    [Test]
    public async Task BettingDeadline_ValidMinutes_CreatesDeadline()
    {
        // Arrange
        var minutes = 15;

        // Act
        var deadline = new BettingDeadline(minutes);

        // Assert
        await Assert.That(deadline.MinutesBeforeMatch).IsEqualTo(minutes);
    }

    [Test]
    public async Task BettingDeadline_ZeroMinutes_CreatesDeadline()
    {
        // Act
        var deadline = new BettingDeadline(0);

        // Assert
        await Assert.That(deadline.MinutesBeforeMatch).IsEqualTo(0);
    }

    [Test]
    public async Task BettingDeadline_NegativeMinutes_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.That(() => new BettingDeadline(-5))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task BettingDeadline_IsMatchOpen_BeforeDeadline_ReturnsTrue()
    {
        // Arrange
        var deadline = new BettingDeadline(15);
        var matchStartTime = DateTime.UtcNow.AddHours(1);
        var currentTime = DateTime.UtcNow;

        // Act
        var isOpen = deadline.IsMatchOpen(matchStartTime, currentTime);

        // Assert
        await Assert.That(isOpen).IsTrue();
    }

    [Test]
    public async Task BettingDeadline_IsMatchOpen_AfterDeadline_ReturnsFalse()
    {
        // Arrange
        var deadline = new BettingDeadline(15);
        var matchStartTime = DateTime.UtcNow.AddMinutes(10);
        var currentTime = DateTime.UtcNow;

        // Act
        var isOpen = deadline.IsMatchOpen(matchStartTime, currentTime);

        // Assert
        await Assert.That(isOpen).IsFalse();
    }

    [Test]
    public async Task BettingDeadline_ImplicitConversion_ReturnsInt()
    {
        // Arrange
        var deadline = new BettingDeadline(30);

        // Act
        int minutes = deadline;

        // Assert
        await Assert.That(minutes).IsEqualTo(30);
    }

    [Test]
    public async Task BettingDeadline_ToString_ReturnsFormattedString()
    {
        // Arrange
        var deadline = new BettingDeadline(15);

        // Act
        var result = deadline.ToString();

        // Assert
        await Assert.That(result).IsEqualTo("15 minutes");
    }

    #endregion

    #region StatsAnalystConfig Tests

    [Test]
    public async Task StatsAnalystConfig_DefaultValues_CreatesConfig()
    {
        // Act
        var config = new StatsAnalystConfig();

        // Assert
        await Assert.That(config.FormWeight).IsEqualTo(0.35);
        await Assert.That(config.HomeAdvantageWeight).IsEqualTo(0.25);
        await Assert.That(config.GoalTrendWeight).IsEqualTo(0.25);
        await Assert.That(config.StreakWeight).IsEqualTo(0.15);
        await Assert.That(config.MatchesAnalyzed).IsEqualTo(5);
        await Assert.That(config.HighStakesBoost).IsTrue();
        await Assert.That(config.Style).IsEqualTo(PredictionStyle.Moderate);
    }

    [Test]
    public async Task StatsAnalystConfig_BalancedPreset_ReturnsDefaultConfig()
    {
        // Act
        var config = StatsAnalystConfig.Balanced;

        // Assert
        await Assert.That(config.FormWeight).IsEqualTo(0.35);
        await Assert.That(config.Style).IsEqualTo(PredictionStyle.Moderate);
    }

    [Test]
    public async Task StatsAnalystConfig_FormFocusedPreset_ReturnsCorrectWeights()
    {
        // Act
        var config = StatsAnalystConfig.FormFocused;

        // Assert
        await Assert.That(config.FormWeight).IsEqualTo(0.60);
        await Assert.That(config.HomeAdvantageWeight).IsEqualTo(0.15);
    }

    [Test]
    public async Task StatsAnalystConfig_HomeAdvantagePreset_ReturnsCorrectWeights()
    {
        // Act
        var config = StatsAnalystConfig.HomeAdvantage;

        // Assert
        await Assert.That(config.HomeAdvantageWeight).IsEqualTo(0.50);
        await Assert.That(config.FormWeight).IsEqualTo(0.20);
    }

    [Test]
    public async Task StatsAnalystConfig_GoalFocusedPreset_ReturnsBoldStyle()
    {
        // Act
        var config = StatsAnalystConfig.GoalFocused;

        // Assert
        await Assert.That(config.Style).IsEqualTo(PredictionStyle.Bold);
        await Assert.That(config.GoalTrendWeight).IsEqualTo(0.50);
    }

    [Test]
    public async Task StatsAnalystConfig_ConservativePreset_ReturnsConservativeStyle()
    {
        // Act
        var config = StatsAnalystConfig.Conservative;

        // Assert
        await Assert.That(config.Style).IsEqualTo(PredictionStyle.Conservative);
        await Assert.That(config.RandomVariance).IsEqualTo(0.05);
    }

    [Test]
    public async Task StatsAnalystConfig_ChaoticPreset_ReturnsHighVariance()
    {
        // Act
        var config = StatsAnalystConfig.Chaotic;

        // Assert
        await Assert.That(config.RandomVariance).IsEqualTo(0.30);
        await Assert.That(config.Style).IsEqualTo(PredictionStyle.Bold);
    }

    [Test]
    public async Task StatsAnalystConfig_MinGoals_Conservative_ReturnsZero()
    {
        // Arrange
        var config = StatsAnalystConfig.Conservative;

        // Assert
        await Assert.That(config.MinGoals).IsEqualTo(0);
    }

    [Test]
    public async Task StatsAnalystConfig_MaxGoals_Bold_ReturnsFive()
    {
        // Arrange
        var config = StatsAnalystConfig.Chaotic;

        // Assert
        await Assert.That(config.MaxGoals).IsEqualTo(5);
    }

    [Test]
    public async Task StatsAnalystConfig_ToJson_ReturnsJsonString()
    {
        // Arrange
        var config = new StatsAnalystConfig { FormWeight = 0.5 };

        // Act
        var json = config.ToJson();

        // Assert
        await Assert.That(json).Contains("FormWeight");
        await Assert.That(json).Contains("0.5");
    }

    [Test]
    public async Task StatsAnalystConfig_FromJson_ReturnsConfig()
    {
        // Arrange
        var config = new StatsAnalystConfig { FormWeight = 0.6 };
        var json = config.ToJson();

        // Act
        var restored = StatsAnalystConfig.FromJson(json);

        // Assert
        await Assert.That(restored.FormWeight).IsEqualTo(0.6);
    }

    [Test]
    public async Task StatsAnalystConfig_FromJson_Null_ReturnsDefault()
    {
        // Act
        var config = StatsAnalystConfig.FromJson(null);

        // Assert
        await Assert.That(config.FormWeight).IsEqualTo(0.35); // Default value
    }

    [Test]
    public async Task StatsAnalystConfig_FromJson_Empty_ReturnsDefault()
    {
        // Act
        var config = StatsAnalystConfig.FromJson("");

        // Assert
        await Assert.That(config.FormWeight).IsEqualTo(0.35); // Default value
    }

    [Test]
    public async Task StatsAnalystConfig_FromJson_InvalidJson_ReturnsDefault()
    {
        // Act
        var config = StatsAnalystConfig.FromJson("{invalid json}");

        // Assert
        await Assert.That(config.FormWeight).IsEqualTo(0.35); // Default value
    }

    #endregion
}
