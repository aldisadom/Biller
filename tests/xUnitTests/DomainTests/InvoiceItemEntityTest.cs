using Domain.Entities;
using FluentAssertions;

namespace xUnitTests.DomainTests;

public class InvoiceItemEntityTest
{
    [Fact]
    public void New_DefaultValues_AreEmpty()
    {
        //Act
        var entity = new InvoiceItemEntity();

        //Assert
        entity.Name.Should().BeEmpty();
        entity.Comments.Should().BeEmpty();
    }

    [Fact]
    public void Properties_GivenValues_ReturnSameValues()
    {
        //Arrange
        var id = Guid.NewGuid();

        //Act
        var entity = new InvoiceItemEntity
        {
            Id = id,
            Name = "Service",
            Price = 12.5m,
            Quantity = 2,
            TotalPrice = 25m,
            Comments = "note",
        };

        //Assert
        entity.Id.Should().Be(id);
        entity.Name.Should().Be("Service");
        entity.Price.Should().Be(12.5m);
        entity.Quantity.Should().Be(2);
        entity.TotalPrice.Should().Be(25m);
        entity.Comments.Should().Be("note");
    }
}
