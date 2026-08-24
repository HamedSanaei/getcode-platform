using GetCode.Application.Identity;
using GetCode.Infrastructure.Identity;
using Xunit;

namespace GetCode.UnitTests;

public sealed class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_is_salted_and_never_repeats()
    {
        var first = _hasher.Hash("correct-horse-Battery7");
        var second = _hasher.Hash("correct-horse-Battery7");

        Assert.NotEqual(first, second);
        Assert.StartsWith("PBKDF2$", first);
        Assert.Equal(3, first.Count(c => c == '$'));
    }

    [Fact]
    public void Verify_accepts_correct_password_only()
    {
        var hash = _hasher.Hash("correct-horse-Battery7");

        Assert.True(_hasher.Verify("correct-horse-Battery7", hash));
        Assert.False(_hasher.Verify("wrong-password-X9!", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("PBKDF2$abc$c2FsdA==$ZGl nZXQ=")]
    [InlineData("BCRYPT$210000$c2FsdA==$ZGlnZXN0")]
    public void Malformed_or_foreign_hashes_verify_false(string stored)
    {
        Assert.False(_hasher.Verify("anything", stored));
    }

    [Fact]
    public void Lower_iteration_hash_is_flagged_for_rehash()
    {
        // Hand-crafted legacy-format hash with fewer iterations than current policy.
        var legacy = "PBKDF2$100000$MTYieWJ5dGVzMTYieWJ5dGU=$" + Convert.ToBase64String(new byte[64]);
        Assert.True(_hasher.NeedsRehash(legacy));

        var current = _hasher.Hash("correct-horse-Battery7");
        Assert.False(_hasher.NeedsRehash(current));
    }
}
