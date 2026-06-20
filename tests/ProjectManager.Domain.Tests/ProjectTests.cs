using FluentAssertions;

namespace ProjectManager.Domain.Tests;

public class ProjectTests
{
    [Fact]
    public void Create_WithValidValues_TrimsAndAssigns()
    {
        var p = Project.Create("prj1", "  Name  ", " ABC ", " Cust ");

        p.Id.Should().Be("prj1");
        p.Name.Should().Be("Name");
        p.Abbreviation.Should().Be("ABC");
        p.Customer.Should().Be("Cust");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankName_Throws(string? name)
    {
        var act = () => Project.Create("prj1", name!, "ABC", "Cust");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithBlankId_Throws()
    {
        var act = () => Project.Create("  ", "Name", "ABC", "Cust");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ChangesMutableFields_KeepsId()
    {
        var p = Project.Create("prj1", "Old", "OLD", "OldCust");
        p.Update("New", "NEW", "NewCust");

        p.Id.Should().Be("prj1");
        p.Name.Should().Be("New");
        p.Abbreviation.Should().Be("NEW");
        p.Customer.Should().Be("NewCust");
    }
}
