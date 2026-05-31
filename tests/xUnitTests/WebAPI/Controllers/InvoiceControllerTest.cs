using Application.Interfaces;
using Application.MappingProfiles;
using Application.Models;
using Contracts.Enums;
using Contracts.Requests.Customer;
using Contracts.Requests.Invoice;
using Contracts.Requests.Seller;
using Contracts.Responses;
using Contracts.Responses.Invoice;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebAPI.Controllers;

namespace xUnitTests.WebAPI.Controllers;

public class InvoiceControllerTest
{
    private readonly Mock<IInvoiceService> _invoiceServiceMock;
    private readonly Mock<IValidator<InvoiceAddRequest>> _validatorAddMock;
    private readonly Mock<IValidator<InvoiceGenerateRequest>> _validatorGenerateMock;
    private readonly Mock<IValidator<InvoiceUpdateRequest>> _validatorUpdateMock;
    private readonly InvoiceController _invoiceController;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();

    public InvoiceControllerTest()
    {
        _invoiceServiceMock = new Mock<IInvoiceService>(MockBehavior.Strict);
        _validatorAddMock = new Mock<IValidator<InvoiceAddRequest>>(MockBehavior.Strict);
        _validatorGenerateMock = new Mock<IValidator<InvoiceGenerateRequest>>(MockBehavior.Strict);
        _validatorUpdateMock = new Mock<IValidator<InvoiceUpdateRequest>>(MockBehavior.Strict);

        _invoiceController = new InvoiceController(
            _invoiceServiceMock.Object,
            new Mock<ILogger<InvoiceController>>().Object,
            _validatorAddMock.Object,
            _validatorGenerateMock.Object,
            _validatorUpdateMock.Object);
    }

    private InvoiceModel CreateInvoice() => new()
    {
        Id = Guid.NewGuid(),
        User = new UserModel { Id = _userId },
        Seller = new SellerModel { Id = _sellerId },
        Customer = new CustomerModel { Id = _customerId },
        Items = [new InvoiceItemModel { Id = Guid.NewGuid() }]
    };

    [Fact]
    public async Task Get_GivenValidId_ReturnsOkWithInvoiceResponse()
    {
        // Arrange
        var invoice = CreateInvoice();

        _invoiceServiceMock.Setup(s => s.Get(invoice.Id))
            .ReturnsAsync(invoice);

        // Act
        var result = await _invoiceController.Get(invoice.Id);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(invoice.ToResponse());

        _invoiceServiceMock.Verify(s => s.Get(invoice.Id), Times.Once());
        _invoiceServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_GivenQuery_ReturnsOkWithInvoiceListResponse()
    {
        // Arrange
        var invoices = new List<InvoiceModel> { CreateInvoice(), CreateInvoice() };
        var query = new InvoiceGetRequest { UserId = _userId };

        _invoiceServiceMock.Setup(s => s.Get(query))
            .ReturnsAsync(invoices);

        // Act
        var result = await _invoiceController.Get(query);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new InvoiceListResponse
            {
                Invoices = invoices.Select(i => i.ToResponse()).ToList()
            });

        _invoiceServiceMock.Verify(s => s.Get(query), Times.Once());
        _invoiceServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Add_GivenValidRequest_ReturnsCreatedWithAddResponse()
    {
        // Arrange
        var newId = Guid.NewGuid();
        var request = new InvoiceAddRequest
        {
            SellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid()
        };

        _validatorAddMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult());
        _invoiceServiceMock.Setup(s => s.Add(It.Is<InvoiceModel>(m =>
                m.Seller!.Id == request.SellerId &&
                m.Customer!.Id == request.CustomerId)))
            .ReturnsAsync(newId);

        // Act
        var result = await _invoiceController.Add(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>()
            .Which.Value.Should().BeEquivalentTo(new AddResponse { Id = newId });

        _validatorAddMock.Verify(v => v.Validate(request), Times.Once());
        _validatorAddMock.VerifyNoOtherCalls();
        _invoiceServiceMock.Verify(s => s.Add(It.Is<InvoiceModel>(m =>
                m.Seller!.Id == request.SellerId &&
                m.Customer!.Id == request.CustomerId)), Times.Once());
        _invoiceServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_GivenValidRequest_ReturnsNoContent()
    {
        // Arrange
        var request = new InvoiceUpdateRequest
        {
            Id = Guid.NewGuid(),
            Seller = new SellerUpdateRequest(),
            Customer = new CustomerUpdateRequest()
        };

        _validatorUpdateMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult());
        _invoiceServiceMock.Setup(s => s.Update(It.Is<InvoiceModel>(m =>
                m.Id == request.Id &&
                m.Seller!.Id == request.Seller.Id &&
                m.Customer!.Id == request.Customer.Id)))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _invoiceController.Update(request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _validatorUpdateMock.Verify(v => v.Validate(request), Times.Once());
        _validatorUpdateMock.VerifyNoOtherCalls();
        _invoiceServiceMock.Verify(s => s.Update(It.Is<InvoiceModel>(m =>
                m.Id == request.Id &&
                m.Seller!.Id == request.Seller.Id &&
                m.Customer!.Id == request.Customer.Id)), Times.Once());
        _invoiceServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateStatus_GivenRequest_ReturnsNoContent()
    {
        // Arrange
        var request = new InvoiceUpdateStatusRequest
        {
            Id = Guid.NewGuid(),
            Status = InvoiceStatus.Payed
        };

        _invoiceServiceMock.Setup(s => s.UpdateStatus(request))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _invoiceController.UpdateStatus(request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _invoiceServiceMock.Verify(s => s.UpdateStatus(request), Times.Once());
        _invoiceServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_GivenValidId_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        _invoiceServiceMock.Setup(s => s.Delete(id))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _invoiceController.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _invoiceServiceMock.Verify(s => s.Delete(id), Times.Once());
        _invoiceServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GeneratePDF_GivenValidRequest_ReturnsFileStreamResult()
    {
        // Arrange
        var memoryStream = new MemoryStream("pdf-content"u8.ToArray());
        var fileName = "invoice.pdf";
        var request = new InvoiceGenerateRequest
        {
            Id = Guid.NewGuid(),
            LanguageCode = Language.LT,
            DocumentType = DocumentType.Invoice
        };

        _validatorGenerateMock.Setup(v => v.Validate(request))
            .Returns(new ValidationResult());
        _invoiceServiceMock.Setup(s => s.GeneratePDF(request.Id, request.LanguageCode, request.DocumentType))
            .ReturnsAsync((memoryStream, fileName));

        // Act
        var result = await _invoiceController.GeneratePDF(request);

        // Assert
        result.Should().BeOfType<FileStreamResult>()
            .Which.ContentType.Should().Be("application/pdf");

        _validatorGenerateMock.Verify(v => v.Validate(request), Times.Once());
        _validatorGenerateMock.VerifyNoOtherCalls();
        _invoiceServiceMock.Verify(s => s.GeneratePDF(request.Id, request.LanguageCode, request.DocumentType), Times.Once());
        _invoiceServiceMock.VerifyNoOtherCalls();
    }
}
