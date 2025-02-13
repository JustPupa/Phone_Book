namespace Phone_book.Models.Dto
{
    public class SdepPhonesGroup
    {
        public string Sdep { get; set; }
        public List<Person> PhonesOfSdep { get; set; }
        public SdepPhonesGroup(string sd, List<Person> phs)
        {
            this.Sdep = sd;
            this.PhonesOfSdep = phs;
        }
    }
}
