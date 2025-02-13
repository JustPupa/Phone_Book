namespace Phone_book.Models.Dto
{
    public class IdName
    {
        public int Id { get; set; }
        public string name { get; set; }
        public IdName(int i, string n)
        {
            Id = i;
            name = n;
        }
    }
}
