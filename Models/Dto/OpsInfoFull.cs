namespace Phone_book.Models.Dto
{
    public class OpsInfoFull
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Dep { get; set; }
        public string Subdep { get; set; }
        public int? Index { get; set; }
        public string Address { get; set; }
        public OpsInfoFull(int id, string n, int? d, int? sd, int? ind, string addr)
        {
            Id = id;
            Name = n;
            if (d != null)
            {
                Dep = Repo.GetSingle<Departments>("departments", Repo.GetEqCondition("id", d.ToString())).name;
            }
            if (sd != null)
            {
                var sdep = Repo.GetSingle<Subdepartments>("subdepartments", Repo.GetEqCondition("id", sd.ToString()));
                Subdep = sdep.name;
                Dep = Repo.GetSingle<Departments>("departments", Repo.GetEqCondition("id", sdep.depId.ToString())).name;
            }
            Index = ind;
            Address = addr;
        }
        public static List<OpsInfoFull> ConvertToFull(IQueryable<Ops> opses)
        {
            return opses.Select(o => new OpsInfoFull(o.id, o.name, o.dep, o.subdep, o.index, o.address)).ToList();
        }
    }
}
