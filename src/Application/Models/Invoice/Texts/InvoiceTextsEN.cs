namespace Application.Models.Invoice.Texts;

public readonly struct InvoiceTextsEN : IInvoiceTexts
{
    public string SellerTitle() => "Goods / service seller";
    public string BuyerTitle() => "Goods / service buyer";
    public string CompanyNumber() => "Company registration number";
    public string IndividualNumber() => $"Individual activity certificate {NumberShort()}";
    public string Adress() => "Address";
    public string Phone() => "Phone";
    public string Email() => "Email";
    public string BankAccount() => "Bank account";


    public string Invoice() => "INVOICE";
    public string JobDoneAct() => "WORK COMPLETION CERTIFICATE";
    public string InvoiceSeries() => "Series";
    public string NumberShort() => "No.";
    public string CreationDate() => "Issue date";
    public string DueDate() => "Payement due date";
    public string ItemName() => "Goods, property or service description";
    public string ItemPrice() => $"Unit price, {Currency()}";
    public string Amount() => "Quantity";
    public string Sum() => $"Amount, {Currency()}";
    public string SumTotal() => "Total amount";
    public string SumInWords() => "Amount in words";
    public string Currency() => "€";
    public string Comment() => "Comment";
    public string InvoicedBy() => "Invoiced by";
    public string Prepared() => "Prepared by";
    public string ServiceRecieved() => "Service received";
    public string Contractor() => "Contractor";
    public string Coordinated() => "Coordinated by";
    public string ResponsibleWorker() => "Responsible employee";
    public string Position() => "Position";
    public string Name() => "First name";
    public string Surname() => "Last name";
    public string Signature() => "Signature";
    public string Date() => "Date";
}
