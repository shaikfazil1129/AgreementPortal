namespace AgreementPortal.Models
{
    public class Agreement
    {
        public int Id { get; set; }
        public string Agreement_Num { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Assigned_To { get; set; }
        public DateTime Created_Date { get; set; }
        public DateTime? End_Date { get; set; }
        public string Comments { get; set; } = string.Empty;
    }
}
