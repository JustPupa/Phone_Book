namespace Phone_book.Models.Dto
{
    public class DSPidsNamesOnly
    {
        public List<IdName> Deps { get; set; }
        public List<IdName> SubDeps { get; set; }
        public List<IdName> Ops { get; set; }
        public List<IdName> Phones { get; set; }
        public List<IdName> Jobs { get; set; }
        public List<IdName> Indexes { get; set; }
    }
}