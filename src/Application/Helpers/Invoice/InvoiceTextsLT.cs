namespace Application.Helpers.Invoice;

public readonly struct InvoiceTextsLT : IInvoiceTexts
{
    public string SellerTitle() => "Prekių / paslaugų pardavėjas";
    public string BuyerTitle() => "Prekių / paslaugų pirkėjas";
    public string CompanyNumber() => "Juridinio asmens kodas";
    public string IndividualNumber() => $"Ind. veiklos pažymos {NumberShort()}";
    public string Adress() => "Adresas";
    public string Phone() => "Tel.";
    public string Email() => "El. paštas";
    public string BankAccount() => "A.s";


    public string Invoice() => "SĄSKAITA FAKTŪRA";
    public string JobDoneAct() => "ATLIKTŲ DARBŲ AKTAS";
    public string InvoiceSeries() => "Serija";
    public string NumberShort() => "Nr.";
    public string CreationDate() => "Išrašymo data";
    public string DueDate() => "Apmokėjimo data";
    public string ItemName() => "Prekės, turto ar paslaugos pavadinimas";
    public string ItemPrice() => $"vnt. kaina, {Currency()}";
    public string Amount() => "Kiekis";
    public string Sum() => $"Suma, {Currency()}";
    public string SumTotal() => "Bendra suma";
    public string SumInWords() => "Suma žodžiais";
    public string Currency() => "€";
    public string Comment() => "Komentaras";
    public string InvoicedBy() => "Sąskaitą išrašė";
    public string Prepared() => "Parengė";
    public string ServiceRecieved() => "Paslaugą gavau";
    public string Contractor() => "Rangovas";
    public string Coordinated() => "Suderinta";
    public string ResponsibleWorker() => "Atsakingas darbuotojas";
    public string Position() => "Pareigos";
    public string Name() => "Vardas";
    public string Surname() => "Pavardė";
    public string Signature() => "Parašas";
    public string Date() => "Data";
}
