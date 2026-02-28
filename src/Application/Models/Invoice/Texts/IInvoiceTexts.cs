namespace Application.Models.Invoice.Texts;

public interface IInvoiceTexts
{
    public string SellerTitle();
    public string BuyerTitle();
    public string CompanyNumber();
    public string IndividualNumber();
    public string Adress();
    public string Phone();
    public string Email();
    public string BankAccount();


    public string Invoice();
    public string JobDoneAct();
    public string InvoiceSeries();
    public string NumberShort();
    public string CreationDate();
    public string DueDate();
    public string ItemName();
    public string ItemPrice();
    public string Amount();
    public string Sum();
    public string SumTotal();
    public string SumInWords();
    public string Currency();
    public string Comment();
    public string InvoicedBy();
    public string Prepared();
    public string ServiceRecieved();
    public string Contractor();
    public string Coordinated();
    public string ResponsibleWorker();
    public string Position();
    public string Name();
    public string Surname();
    public string Signature();
    public string Date();
}
