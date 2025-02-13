namespace Phone_book.Models.Dto
{
    public class UpsInfo
    {
        public int Id { get; set; }
        public string UpsName { get; set; }
        public int DepId { get; set; }
        public string DepName { get; set; }
        public List<Ops> Opses { get; set; }
        public List<Person> Employees { get; set; }
        public int? SelTabInd { get; set; }
    }
}
