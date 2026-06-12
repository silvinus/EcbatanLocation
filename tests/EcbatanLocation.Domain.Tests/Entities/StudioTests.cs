using EcbatanLocation.Domain.Entities;

namespace EcbatanLocation.Domain.Tests.Entities;

public class StudioTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var studio = Studio.Create("Villa", 6, true, true, 1);

        Assert.Equal("Villa", studio.Name);
        Assert.Equal(6, studio.Capacity);
        Assert.True(studio.HasKitchen);
        Assert.True(studio.RentableAlone);
        Assert.False(studio.Unavailable);
        Assert.Equal(1, studio.DisplayOrder);
        Assert.NotEqual(Guid.Empty, studio.Id);
    }

    [Fact]
    public void Create_WithUnavailable_SetsFlag()
    {
        var studio = Studio.Create("Villa", 6, true, true, 1, unavailable: true);

        Assert.True(studio.Unavailable);
    }

    [Fact]
    public void Update_ChangesAllProperties()
    {
        var studio = Studio.Create("Villa", 6, true, true, 1);

        studio.Update("Maison", 8, false, false, true);

        Assert.Equal("Maison", studio.Name);
        Assert.Equal(8, studio.Capacity);
        Assert.False(studio.HasKitchen);
        Assert.False(studio.RentableAlone);
        Assert.True(studio.Unavailable);
    }

    [Fact]
    public void Update_CanToggleUnavailable()
    {
        var studio = Studio.Create("Villa", 6, true, true, 1);
        Assert.False(studio.Unavailable);

        studio.Update("Villa", 6, true, true, true);
        Assert.True(studio.Unavailable);

        studio.Update("Villa", 6, true, true, false);
        Assert.False(studio.Unavailable);
    }
}
