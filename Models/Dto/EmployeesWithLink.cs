namespace Phone_book.Models.Dto
{
    public class EmployeesWithLink
    {
        public List<Person> Employees { get; set; }
        public string DepAlias { get; set; }
        public (int?,string) Ops { get; set; }
        public (int?,string) Sdep { get; set; }
        public (int?,string) Dep { get; set; }
        public int? SelTabInd { get; set; }
    }
}
