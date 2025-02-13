namespace Phone_book.Models.Dto
{
    public class RupsWithUps
    {
        public string RupsName { get; set; }
        public string AliasName { get; set; }
        public List<(string, List<Subdepartments>)> Upss { get; set; }
        public List<(string, List<Person>)> EmployersWRups { get; set; }
        public List<Ops> Opss { get; set; }
        public int? SelTabInd { get; set; }
    }
}