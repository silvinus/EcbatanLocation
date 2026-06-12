using EcbatanLocation.Domain.Entities;

namespace EcbatanLocation.Domain.Tests.Entities;

public class OwnerTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var owner = Owner.Create("Jean", "user-123");

        Assert.Equal("Jean", owner.Name);
        Assert.Equal("user-123", owner.UserId);
        Assert.NotEqual(Guid.Empty, owner.Id);
    }

    [Fact]
    public void Update_ChangesName()
    {
        var owner = Owner.Create("Jean", "user-123");

        owner.Update("Jean-Pierre");

        Assert.Equal("Jean-Pierre", owner.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_EmptyOrWhitespaceName_Throws(string name)
    {
        var owner = Owner.Create("Jean", "user-123");

        Assert.Throws<ArgumentException>(() => owner.Update(name));
    }

    [Fact]
    public void Update_NullName_Throws()
    {
        var owner = Owner.Create("Jean", "user-123");

        Assert.Throws<ArgumentNullException>(() => owner.Update(null!));
    }
}
